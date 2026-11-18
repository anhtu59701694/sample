using System.Web.Mvc;
using System.Linq;
using Claw.Controls.Attributes;
using Claw.Core.Enumerations;
using Claw.Core.Interfaces;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Web.UI.WebControls;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm]
    public class VatController : BaseController<VatModel, Vat>
    {
        private const string IndexView = "Admin/VatsIndex.cshtml";
        private const string EditView = "Admin/VatsEdit.cshtml";

        public VatController(IDataService ds) : base(ds) { }

        #region Index
        [ValidateInput(false)]
        public ActionResult Index()
        {
            ViewBag.Settings = GetSettings();

            ViewBag.EagerData = new GridEagerLoadDataHandler(List);

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
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

            var items = VatMgr.GetAll(this, null, page, pageSize, out totalRows, sortDir, sortField)
                .Action(m => { m.InlineEdit = true; m.AnyDepend = m.GetRaw().AnyDepend(); });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region Input.Get
        [ValidateInput(false)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = VatMgr.Get(this, id);

            //ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();
           

            if (obj == null)
                obj = new VatModel();

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request,
            VatModel obj, string lang, string mode)
        {
            if (obj == null) return JsonStatus("", false);

            if (obj.InlineEdit && obj.Id != 0)
            {
                var dom = VatMgr.GetDom(this, obj.Id);
                obj.AnyDepend = dom.AnyDepend();
            }

            if (ModelState.ModLocalize().IsValid)
            {
                if (VatMgr.SaveorUpdate(this, obj))
                {
                    if (!obj.InlineEdit) SetViewMsgInput(true);
                }
                else goto LB_ERR;

                return obj.InlineEdit
                    ? JsonInline(request, obj, true)
                    : RedirectToAction("Index", new { lang = lang, mode = mode });
            }

        LB_ERR:

            if (obj.InlineEdit)
                ModelState.TransformModelError<VatModel>((field, dispName, modelErr) =>
                    string.Format(obj.QuickAdd ? "{1}" : "[{0}] {1}", dispName, modelErr.ErrorMessage));
            else SetViewMsgInput(false);

            return obj.InlineEdit
                ? JsonInline(request, obj, false)
                : ClawView("Admin/VatsEdit.cshtml", obj);
        }

        #endregion

        #region Delete

        [HttpPost]
        public override ActionResult Delete(GridId[] ids)
        {
            var retCode = (ids != null && ids.Length > 0 && BaseMgr<VatModel, Vat>.Delete(this,
                (sess, obj) => !obj.AnyDepend(), null, ids.Select(x => x.Id).ToArray()));

            if (!IndexAction) SetViewMsg(retCode, StringTable.Deleted);

            return JsonStatus(StringTable.Deleted, retCode);
        }

        #endregion

        #region SetDefault

        [HttpPost]
        public virtual ActionResult SetDefault(int id, bool value)
        {
            var ret = VatMgr.Update(this, id, value);

            return JsonStatus(StringTable.UpdateDefault, ret);
        }

        #endregion

        #region SetTakeAway

        [HttpPost]
        public virtual ActionResult SetTakeAway(int id, bool value)
        {
            var ret = VatMgr.Update(this, id, null, value);

            return JsonStatus(StringTable.UpdateTakeAway, ret);
        }

        #endregion

        #region ValidateCode

        public virtual ActionResult ValidateCode(string code, int? id)
        {
            return ValidateField("Code", code, id);
        }

        #endregion
    }

}