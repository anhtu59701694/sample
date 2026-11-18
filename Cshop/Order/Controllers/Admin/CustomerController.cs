using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AutoMapper;
using Claw.Controls.Attributes;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.Enumerations;
using Claw.Core.Extensions;
using Claw.Core.Interfaces;
using Claw.Core.Logs;
using Claw.Core.Providers;
using Claw.Core.Utility;
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using System.Linq;
using System.Web;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Web.Mvc;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.Helper;
using System.Text.RegularExpressions;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("OrderBits", byte.MaxValue)]
    public class CustomerController : BaseController<CustomerModel, Customer>
    {
        private const string IndexView = "Admin/CustomersIndex.cshtml";
        private const string EditView = "Admin/CustomersEdit.cshtml";

        #region CustomerController
        public CustomerController(IDataService ds)
            : base(ds)
        {
        } 
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Index()
        {
            ViewBag.Settings = GetSettings();
            ViewBag.Groups = CustGroupMgr.GetAll(this);

            ViewBag.EagerData = new GridEagerLoadDataHandler(List);

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult List([DataSourceRequest] DataSourceRequest request)
        {
            TrimReq(request);

            string sortField = "Id";
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            return Json(List(request.Page, request.PageSize, sortField, sortDir));
        }

        private DataSourceResult List(int page, int pageSize, string sortField, SortDirection sortDir)
        {
            int totalRows;
            var groupid = GetParam<int?>("groupid");
            var status = GetParam<int?>("status");
            var companyId = GetParam<int?>("companyId");
            var keyword = GetKeyword();

            var items = CustomerMgr.GetAllEx(this, page,
                pageSize, out totalRows, sortDir, sortField, groupid, keyword, true, status: status, companyId: companyId)
                .Action(x =>
                {
                    x.InlineEdit = true;
                    x.InitCustGroups(this);
                });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListCate

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult ListCate([DataSourceRequest] DataSourceRequest request)
        {
            TrimReq(request);

            string sortField = null;
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            var categoryid = GetParam<int>("categoryid");
            var cate = CategoryMgr.GetDom(this, categoryid);
            var lst = (cate == null ? new List<CategoryCustomer>() : cate.CategoryCustomers.Cast<CategoryCustomer>()).Select(m => new CategoryCustomerModel(m.Customer)
            {
                Id = m.Id, DiscountRate = m.DiscountRate * 100, Notes = m.Notes
            }).ToList();

            if(!sortField.IsNullOrEmpty())
                switch (sortField.ToLower())
                {
                    case "CustomerEmail":
                        lst = sortDir == SortDirection.Ascending
                            ? lst.OrderBy(m => m.CustomerEmail).ToList()
                            : lst.OrderByDescending(m => m.CustomerEmail).ToList();
                        break;
                    case "CustomerName":
                        lst = sortDir == SortDirection.Ascending
                            ? lst.OrderBy(m => m.CustomerName).ToList()
                            : lst.OrderByDescending(m => m.CustomerName).ToList();
                        break;
                    case "DiscountRate":
                        lst = sortDir == SortDirection.Ascending
                            ? lst.OrderBy(m => m.DiscountRate).ToList()
                            : lst.OrderByDescending(m => m.DiscountRate).ToList();
                        break;
                }

            var items = lst.GetPage(request.Page, request.PageSize);

            return Json(new DataSourceResult() { Data = items, Total = lst.Count });
        }

        #endregion

        #region ListOrder
        [ValidateInput(false)]
        public ActionResult ListOrder(int id, string lang, string mode)
        {
            return RedirectToAction("Index", "Order", new { CustomerId = id, lang, mode });
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Input(int id, string categories, string lang, string optlang, string mode)
        {
            var obj = CustomerMgr.Get(this, id) ?? new CustomerModel() { Raw = new Domain.Customer() };

            if ((obj.Status ?? 0).InList(CustStatus.Deleted))
            {
                SetViewMsgInput(false, StringTable.Deleted);
                return RedirectToAction("Index", new {lang, mode});
            }

            ViewBag.Settings = GetSettings();
            ViewBag.Countries = CountryMgr.GetAll(this);

            if(obj.Id != 0 && !obj.Email.IsNullOrEmpty()) 
                OrderMgr.BindStats(this, new[] { obj.GetRaw() }.Where(x => x != null).ToList());

            obj.InitCustGroups(this);
            obj.InitCustCategories(this, categories);
            obj.InitCustAddresses();

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, CustomerModel obj,
            string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new CustomerModel(), false)
                    : RedirectToAction("Index", new { lang, mode });

            var oId = obj.Id;
            var passwdTxt = "";
            if (obj.Id == 0 && (obj.Status ?? 0).InList(CustStatus.Active))
            {
                obj.PasswordSalt = EncryptProvider.GenerateSalt();
                passwdTxt = obj.Password.IfNull(CustomerMgr.GeneratePassword());
                obj.Password = EncryptProvider.EncryptPassword(passwdTxt, obj.PasswordSalt);
            }

            #region CustAddresses

            if (obj.CustAddresses != null && !obj.InlineEdit)
                foreach (var itm in obj.CustAddresses
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.CustAddresses.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<CustomerModel, object>(m => m.CustAddresses)),
                        itm.Index, obj.CustAddresses.Count);
                }
            else obj.CustAddresses = new List<AddressModel>();

            #endregion

            #region CustGroups

            if (obj.CustGroups != null && !obj.InlineEdit)
                foreach (var itm in obj.CustGroups
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.CustGroups.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<CustomerModel, object>(m => m.CustGroups)),
                        itm.Index, obj.CustGroups.Count);
                }
            else obj.CustGroups = new List<CustomerCGroupModel>();

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                if (CustomerMgr.SaveorUpdate(this, obj))
                {
                    if (!obj.InlineEdit) SetViewMsgInput(true);

                    if (oId == 0 && (obj.Status ?? 0).InList(CustStatus.Active))
                        Singleton<ISendEmailMgr>.Default.EmailCustomer(this, EmailTemplateType.UserAccActivated, obj.Id, passwdTxt);
                }
                else goto LB_ERR;

                return obj.InlineEdit
                    ? JsonInline(request, obj, true)
                    : RedirectToAction("Index", new { lang, mode });
            }

        LB_ERR:

            if (obj.InlineEdit)
                ModelState.TransformModelError<CustomerModel>((field, dispName, modelErr) =>
                    string.Format("[{0}] {1}", dispName, modelErr.ErrorMessage));
            else
            {
                SetViewMsgInput(false);
                ViewBag.Settings = GetSettings();
                ViewBag.Countries = CountryMgr.GetAll(this);

                obj.InitCustGroups(this, true);
                obj.InitCustCategories(this, null, true);
            }

            return obj.InlineEdit
                ? JsonInline(request, obj, false)
                : ClawView(EditView, obj);
        }
        #endregion

        #region Delete

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            string errMsg = null;
            var retCode = ids.SafeCount() > 0 && CustomerMgr.Action(this, true, (sess, obj) =>
            {
                if (obj.Dependants.Any()) {
                    errMsg = $"Dependant count = {obj.Dependants.Count}"; return false; 
                }

                obj.GetType().GetProperties()
                        .Where(m => m.CanWrite && m.PropertyType == typeof (string))
                        .Action(m =>
                    {
                        if (m.GetValue(obj) != null)
                            m.SetValue(obj, "");
                    });
                    obj.Email = string.Format("{0}@deleted.xxx", obj.Id);
                    obj.CustomerCGroups.Action(m => { m.Group.CustomerNum = null; sess.Update(m); }).Clear();
                    obj.CustomerProducts.Clear();
                    obj.CategoryCustomers.Clear();
                    obj.Status = (int)CustStatus.Deleted;

                    if (obj.Company != null) 
                    { 
                        obj.Company.UserNum = null; sess.Update(obj.Company); 
                    }
                    RecurOrderMgr.GetAll(this, null, null, 1, int.MaxValue, out _, customerId: obj.Id)
                        .Select(m => m.GetRaw()).Action(m => 
                        {
                            m.Status = (byte)RecurOrderStatus.Deleted;
                            sess.Update(m);
                        });

                    return true;
                }, ids.Select(x => x.Id).ToArray());

            if (!IndexAction && Request != null) SetViewMsg(retCode, StringTable.Delete, errMsg);

            return JsonStatus(StringTable.Delete, retCode, errMsg);
        }

        #endregion

        #region Export

        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Export()
        {
            var groupid = QParam.GetField<int?>("groupid", false);
            var keyword = GetKeyword();

            int total;
            var fileName = "Customers_" + DateTime.Now.ToString("ddMMyyyy") + ".xls";
            var list = CustomerMgr.GetAll(this, 1, int.MaxValue, out total, SortDirection.Descending, "Id", groupid, keyword)
                .Action(m => m.RecvOfferInfo = m.RecvOfferInfo ?? false);

            var ret = ToExcel(list, new[]
            {
                "ContactName", "OrgNumber", "CompanyName", "InvoiceEmail", "Email",  "Telephone", "Mobile", 
                "DeliverName", "DeliverAddress", "DeliverPostnr", "DeliverPosted", "DeliverCountryName", "DeliverCountryCode", "DeliverMobile", "DeliverPhone",        
                "BillingName", "BillingAddress", "BillingPostnr", "BillingPosted", "BillingCountryName", "BillingCountryCode", "BillingMobile", "BillingPhone",
                "RecvOfferInfo", "CreatedDate",
            }, fDispName: x => x.GetExportDisplayNameEx());

            return File(ret, "application/vnd.ms-excel", fileName);
        }

        #endregion

        #region UpdateCompanyOwner

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdateCompanyOwner(int id,  bool value)
        {
            var obj = CustomerMgr.GetDom(this, id);

            if (!GetSettings().Hidden.CustCompany || obj == null || obj.CompanyName.IsNullOrEmpty() || obj.OrgNumber.IsNullOrEmpty()) 
                return JsonStatus(StringTable.UpdateCompanyOwner, false);

            var company = obj.Company ?? (CompanyMgr.GetDom(this, obj.OrgNumber) ?? CompanyMgr.SaveorUpdate(this, new CompanyModel()
            {
                Name = obj.CompanyName, OrgNumber = obj.OrgNumber,
            }));

            var retCode = BaseMgr.SaveOrUpdate(this, true, (session, comp) =>
            {
                comp.Owner = value ? obj : null;
                return true;
            }, company);

            if (retCode && value)
            {
                var lst = BaseMgr.GetList<Customer>(this, string.Format(
                    "from {0} where OrgNumber = '{1}' and Company is null and 0 = (Flags & {2}) ", 
                    typeof(Customer).FullName, obj.OrgNumber, CustomerFlagType.CustCompanyInited.Val())).Action(m =>
                    {
                        m.Company = company;
                        m.Flags |= (int) CustomerFlagType.CustCompanyInited;
                        m.Orders.Where(z => z.Company == null).Action(z => z.Company = m.Company);
                        m.CustomerProducts.Where(z => z.Company == null).Action(z => z.Company = m.Company);
                    });

                if (lst.Count != 0) retCode = BaseMgr.Update(this, lst);
            }

            return JsonStatus(StringTable.UpdateCompanyOwner, retCode);
        }

        #endregion

        #region UpdateGroup

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdateGroup(int id, int groupid, bool selected)
        {
            var obj = CustomerMgr.Get(this, id);
            var retCode = obj != null;

            if (retCode)
            {
                obj.InitCustGroups(this);
                obj.CustGroups.Where(x => x.GroupId == groupid).Action(x => x.Selected = selected);
                obj.CustGroups.RemoveAll(x => !x.Selected);
                retCode = CustomerMgr.SaveorUpdate(this, obj);
            }

            return JsonStatus(StringTable.Update, retCode);
        }

        #endregion

        #region UpdateStatus

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdateStatus(int id, int status)
        {      
            var dom = CustomerMgr.GetDom(this, id);
            var oldStatus = dom == null ? 0 : dom.Status;
            var retCode = CustomerMgr.Update(this, id, m => m.Status, status);

            if (retCode && oldStatus.InList(CustStatus.Pending, CustStatus.Inactive) && status.InList(CustStatus.Active))
            {
                var obj = CustomerMgr.GetDom(this, id);
                var passwd = "";
                if (obj.Password.IsNullOrEmpty())
                {
                    passwd = CustomerMgr.GeneratePassword();
                    obj.PasswordSalt = EncryptProvider.GenerateSalt();
                    obj.Password = EncryptProvider.EncryptPassword(passwd, obj.PasswordSalt);
                    obj.Update(DataService);
                }
                Singleton<ISendEmailMgr>.Default.EmailCustomer(this, EmailTemplateType.UserAccActivated, id, passwd);
            }

            return JsonStatus(StringTable.UpdateStatus, retCode);
        }

        #endregion

        #region ResetPassword

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult ResetPassword(int id, string newPassword)
        {
            var obj = CustomerMgr.GetDom(this, id);
            var retCode = obj != null;

            if (retCode)
            {
                obj.PasswordSalt = EncryptProvider.GenerateSalt();
                obj.Password = EncryptProvider.EncryptPassword(newPassword, obj.PasswordSalt);
                retCode = CustomerMgr.Update(this, new[] { obj }.ToList());
            }

            return JsonStatus(StringTable.ResetPassword, retCode);
        }

        #endregion

        #region ValidateEmail

        public virtual ActionResult ValidateEmail()
        {
            var email = GetParam<string>("Email");
            var id = GetParam<int?>("Id");

            return ValidateField("Email", email, id ?? 0);
        }

        #endregion
    }
}