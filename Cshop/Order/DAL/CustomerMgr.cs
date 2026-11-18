using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.Providers;
using Claw.Modules.Eshop.Helper;
using Newtonsoft.Json;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Domain.Interfaces;
using NHibernate.Linq;
using System.Text;

namespace Claw.Modules.Eshop.DAL
{
    public class CustomerMgr : BaseMgr<CustomerModel, Customer>, ICustomerMgr
    {
        private static object _synclock = new object();
        private static Dictionary<int, object> _companySyncLocks = new Dictionary<int, object>();
        public static Func<IDB, string, Customer> FuncGetEmail = null;

        static CustomerMgr() { Init(typeof(CustomerMgr)); }

        #region GetAll
        /// <summary>Cpos</summary>
        public static List<CustomerModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows, 
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", int? groupid = null, 
            string keyword = null, bool bindStat = false, string email = null, string name = null, 
            string phone = null, int? status = null)

        {
            return GetAllEx(db, pageIndex, pageSize, out totalRows, sortDirection, sortField, groupid, keyword, bindStat, email, name, phone, status, null);
        }

        public static List<CustomerModel> GetAllEx(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id",
            int? groupid = null, string keyword = null, bool bindStat = false, string email = null,
            string name = null, string phone = null, int? status = null, int? companyId = null,
            bool? hasOrders = null, DateTime? lastModified = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select cust from {0} as cust ", typeof(Customer).FullName);

            if (groupid != null) hSql += " left join cust.CustomerCGroups as ccgroup";

            hSql += string.Format(" where cust.Status <> {0} ", CustStatus.Deleted.Val());

            if (groupid != null) hSql += " and ccgroup.Group.Id = " + groupid;
         
            if (status != null) hSql += " and cust.Status = " + status;

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += string.Format(@" and (cust.DeliverName like :keyword or cust.ContactName like :keyword or cust.Email like :keyword or cust.DeliverPhone like :keyword or cust.DeliverMobile like :keyword)");
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            if (!string.IsNullOrEmpty(email))
            {
                hSql += string.Format(@" and cust.Email like :email ");
                args.Add(new System.Tuple<string, string>("email", "%" + email + "%"));
            }

            if (!string.IsNullOrEmpty(name))
            {
                hSql += string.Format(@" and cust.DeliverName like :name ");
                args.Add(new System.Tuple<string, string>("name", "%" + name + "%"));
            }

            if (!string.IsNullOrEmpty(phone))
            {
                hSql += string.Format(@" and (cust.DeliverPhone like :phone or cust.DeliverMobile like :phone) ");
                args.Add(new System.Tuple<string, string>("phone", "%" + phone + "%"));
            }

            if (companyId != null)
            {
                var comp = CompanyMgr.GetDom(db, companyId ?? 0);
                hSql += string.Format(@" and cust.OrgNumber like :orgNumber and cust.Company.Id = {0} ", companyId);
                args.Add(new System.Tuple<string, string>("orgNumber", comp.SafeProp(m => m.OrgNumber)));
            }

            if (hasOrders == true)
                hSql += string.Format(@" and (cust.OrderNum > 0 or cust.OrderNum is null)");

            if (lastModified != null)
                hSql += string.Format(@" and cust.ModifiedDate >= '{0:yyyy-MM-dd HH:mm}' ", lastModified.Value);

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate.Text":
                    sortField = "CreatedDate";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", "cust." + sortField, GetDir(sortDirection));

            #endregion

        LB_LOOP:
            var ret = hasOrders == true
                ? GetList<Customer>(db, hSql, pageIndex, pageSize, out totalRows, query => query.SetMaxResults(pageSize*2), null, args.ToArray()) 
                : GetList<Customer>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            if (bindStat) OrderMgr.BindStats(db, ret);

            if (bindStat && hasOrders == true && totalRows == (pageSize * 2) 
                && ret.Count(m => m.OrderNum > 0) < pageSize) goto LB_LOOP;

            if (hasOrders == true)
            {
                totalRows = ret.Count(m => m.OrderNum > 0);
                ret = ret.Take(pageSize, m => m.OrderNum > 0);
            }

            return Mapper.Map<List<Customer>, List<CustomerModel>>(ret);
        }

        #endregion

        #region BindStats

        public static void BindStats(IDB db, List<Company> list)
        {
            if (list.SafeCount() == 0) return;

            list = list.Where(x => x.UserNum == null).ToList();

            if (list.SafeCount() == 0) return;

            var hSql = string.Format(@"
            select comp.Id, count(*)
            from {0} as cust join cust.Company as comp 
            where comp.Id in ({1}) and comp.OrgNumber = cust.OrgNumber and cust.Status not in ({2})
            group by comp.Id ", typeof(Customer).FullName, list.Select(x => x.Id).ToString(","), CustStatus.Deleted.Val());

            var ret = GetList<object[]>(db, hSql)
                .Select(arr => new
                {
                    CompanyId = arr[0].SafeChangeType<int>(),
                    Count = arr[1].SafeChangeType<int>()
                }).ToList();

            list.Action(x => x.UserNum = 0);

            foreach (var itm in list.Join(ret,
                o => o.Id, m => m.CompanyId, (o, m) => new { O = o, M = m }))
            {
                itm.O.UserNum = itm.M.Count;
            }

            Update(db, list);
        }

        public static void BindStats(IDB db, List<CustGroup> list)
        {
            if (list.SafeCount() == 0) return;

            list = list.Where(x => x.CustomerNum == null).ToList();

            if (list.SafeCount() == 0) return;

            var hSql = string.Format(@"
            select cust.Group.Id, count(*)
            from {0} as cust join cust.Customer as cust2 
            where cust.Group.Id in ({1}) and cust2.Status not in ({2})
            group by cust.Group.Id ", typeof(CustomerCGroup).FullName, list.Select(x => x.Id).ToString(","), CustStatus.Deleted.Val());

            var ret = GetList<object[]>(db, hSql)
                .Select(arr => new
                {
                    GroupId = arr[0].SafeChangeType<int>(),
                    Count = arr[1].SafeChangeType<int>()
                }).ToList();

            list.Action(x => x.CustomerNum = 0);

            foreach (var itm in list.Join(ret,
                o => o.Id, m => m.GroupId, (o, m) => new { O = o, M = m }))
            {
                itm.O.CustomerNum = itm.M.Count;
            }

            Update(db, list);
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, CustomerModel model, ISession session = null, bool ignoreStatus = false)
        {
            if (model == null) return false;

            var settings = Helper.SettingsBiz.GetSettings();
            var oldDom = GetDom(db, model.Id) ?? Get(db, model.Email);
            var oldStatus = oldDom.SafeProp(m => m.Status, (int?)null);
            var oldKey = oldDom == null ? "" : string.Format("DR={0},CatR={1},DGroupId={2}", (double?)oldDom.DiscountRate, 
                oldDom.CategoryCustomers.OrderBy(m => m.Category.Id).Select(m => string.Format("{0}:{1}", m.Category.Id, (double)m.DiscountRate)).ToString(";"),
                oldDom.CustomerCGroups.Where(m => m.Group.Discount).Select(m => m.Group.Id).ToString(";", m => m));
            var obj = ToDom(db, model, session, true);

            obj.Address = BaseMgr<AddressModel, Address>.GetDom(db, model.AddressId ?? 0, session);
            obj.Status = ignoreStatus && oldStatus != null ? oldStatus.Value : (model.Status ?? obj.Status);
            obj.Comments = model.Comments;
            obj.DiscountRate = model.DiscountRate.IsNull() ? (decimal?)null : model.DiscountRate / 100m;

            var newKey = oldDom == null ? "" : string.Format("DR={0},CatR={1},DGroupId={2}", (double?)oldDom.DiscountRate, 
                oldDom.CategoryCustomers.OrderBy(m => m.Category.Id).Select(m => string.Format("{0}:{1}", m.Category.Id, (double)m.DiscountRate)).ToString(";"),
                oldDom.CustomerCGroups.Where(m => m.Group.Discount).Select(m => m.Group.Id).ToString(";", m => m));           
            if (SaveOrUpdate(session != null ? new DefaultDb(session) : db, session == null, (sess, cust) =>
            {
                cust.Address = cust.Addresses.FirstOrDefault(m => m is AddressEx && ((AddressEx)m).IsDefault == true) ?? cust.Address;
                sess.SaveOrUpdate(cust);
                return true;
            }, new[]{ obj }.ToArray()))
            {
                model.Id = obj.Id;

                if (oldStatus != null && oldStatus.Value.InList(CustStatus.Pending, CustStatus.Inactive) &&
                    obj.Status.InList(CustStatus.Active))
                {
                    var passwd = "";
                    if (obj.Password.IsNullOrEmpty() && !settings.Hidden.CustomerDefActiveStatusRequireEmailActivate)
                    {
                        passwd = GeneratePassword();
                        obj.PasswordSalt = EncryptProvider.GenerateSalt();
                        obj.Password = EncryptProvider.EncryptPassword(passwd, obj.PasswordSalt);
                        obj.Update(db.DataService);
                    }
                    Singleton<ISendEmailMgr>.Default.EmailCustomer(db, EmailTemplateType.UserAccActivated, model.Id, passwd);
                }
                else if (false && oldStatus == null && obj.Status.InList(CustStatus.Pending) && settings.Hidden.CustomerDefActiveStatusRequireEmailActivate)
                {
                    var defStatus = obj.CompanyName.IsNullOrEmpty() ? settings.Hidden.CustomerDefaultStatus : settings.Hidden.CompanyCustomerDefaultStatus;
                    if(defStatus.InList(CustStatus.Active)) 
                        Singleton<ISendEmailMgr>.Default.EmailCustomer(db, EmailTemplateType.UserAccWelcome, model.Id, "");
                }

                if (!newKey.EqualsIgCase(oldKey)) Helper.CustomerScope.Invalidate();

                new CustomerMgr().OnUpdate(db, obj, session); return true;
            }
            else return false;
        }
        #endregion

        #region GetData

        public static TVal GetData<TVal>(IDB db, int id, string key, TVal anonymousType = null, bool local = false) where TVal : class
        {
            var obj = GetDom(db, id);

            if (obj == null || key.IsNullOrEmpty()) return null;

            var data = !local && obj.Company != null && obj.Company.Data != null ? obj.Company.Data : obj.Data;
            var dic = data.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(data);

            //if (!local && dic != null && obj.Company != null && obj.Company.Data == null && dic.Any(m => !m.Value.IsNullOrEmpty()))
            //{
            //    obj.Company.Data = obj.Data;
            //    Update(db, new[] {obj.Company}.ToList());
            //}

            if (dic == null || !dic.ContainsKey(key) || dic[key].IsNullOrEmpty()) return null;

            if (anonymousType != null && !dic[key].IsNullOrEmpty()) return JsonConvert.DeserializeAnonymousType(dic[key], anonymousType);

            return dic[key].SafeChangeType<TVal>();
        }

        public static TVal GetData<TVal>(IDB db, int id, string key, Func<string, TVal> fDeserialize, bool local = false) where TVal : class
        {
            var obj = GetDom(db, id);

            if (obj == null || key.IsNullOrEmpty() || fDeserialize == null) return null;

            var data = !local && obj.Company != null && obj.Company.Data != null ? obj.Company.Data : obj.Data;
            var dic = data.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(data);

            //if (!local && dic != null && obj.Company != null && obj.Company.Data == null && dic.Any(m => !m.Value.IsNullOrEmpty()))
            //{
            //    obj.Company.Data = obj.Data;
            //    Update(db, new[] { obj.Company }.ToList());
            //}

            if (dic == null || !dic.ContainsKey(key) || dic[key].IsNullOrEmpty()) return null;
            
            return fDeserialize(dic[key]);
        }

        #endregion

        #region UpdateData
        public static bool UpdateData(IDB db, int id, Dictionary<string, string> dataDic)
        {
            if (dataDic == null || dataDic.Count == 0) return true;

            var obj = GetDom(db, id);

            if (obj == null || dataDic.Any(m => m.Key.IsNullOrEmpty())) return false;

            var data = obj.Data;
            var dic = data.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(data);

            dic = dic ?? new Dictionary<string, string>();

            dataDic.ForEach(m => dic[m.Key] = m.Value);

            obj.Data = dic.All(m => m.Value.IsNullOrEmpty()) ? null : JsonConvert.SerializeObject(dic);

            if (obj.Update(db.DataService))
            {
                //new CustomerMgr().OnUpdate(obj); 
                return true;
            }
            else return false;
        }

        public static bool UpdateDataCompany(IDB db, int id, Dictionary<string, string> dataDic, Func<string,string, string, string> fDataFilter)
        {
            if (dataDic == null || dataDic.Count == 0) return true;

            object syncObj;

            lock (_synclock)
            {
                syncObj = _companySyncLocks.ContainsKey(id) ? _companySyncLocks[id] : new object();
            }

            lock (syncObj)
            {
                var obj = CompanyMgr.GetDom(db, id);

                if (obj == null || fDataFilter == null || dataDic.Any(m => m.Key.IsNullOrEmpty())) return false;

                var dic = obj.Data.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(obj.Data);

                dic = dic ?? new Dictionary<string, string>();

                dataDic.ForEach(m => dic[m.Key] = fDataFilter(m.Key, dic.SafeGet(m.Key), m.Value));

                obj.Data = dic.All(m => m.Value.IsNullOrEmpty()) ? null : JsonConvert.SerializeObject(dic);

                if (obj.Update(db.DataService))
                {
                    //new CustomerMgr().OnUpdate(obj); 
                    return true;
                }
                else return false;
            }
        }
        #endregion

        #region Get

        public static Customer Get(IDB db, string email)
        {
            if (email.IsNullOrEmpty()) return null;
            if (FuncGetEmail != null) return FuncGetEmail(db, email);
            return db.DataService.GetList<Customer>(0, "Email", email.Trim()).FirstOrDefault();
        }
        public static CustomerModel GetDom(IDB db, string email)
        {
            if (email.IsNullOrEmpty()) return null;

            var ret = FuncGetEmail != null
                ? FuncGetEmail(db, email)
                : db.DataService.GetList<Customer>(0, "Email", email.Trim()).FirstOrDefault();

            return Mapper.Map<Customer, CustomerModel>(ret);
        }
        public static Customer Get2(IDB db, string mobile)
        {
            if (mobile.IsNullOrEmpty()) return null;

            return GetList<Customer>(db, string.Format(@"
            from {0} 
            where Mobile like :mobile 
            or DeliverMobile like :mobile 
            or BillingMobile like :mobile ", typeof(Customer).FullName), new[] { new System.Tuple<string,string>("mobile", mobile.Trim()) }).FirstOrDefault();
            
        }

        #endregion

        #region GetProducts

        public static List<CustomerProduct> GetProducts(IDB db, int id, int? companyId, string tags, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null, string[] datafields = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select custP 
            from {0} as custP {1}
            where custP.Active = 1 ", typeof(CustomerProduct).FullName, 
                keyword.IsNullOrEmpty() ? "" : " join custP.Product as prod join prod.Languages as lang ");

            if (companyId == null) hSql += " and custP.Company is null and custP.Customer.Id = " + id;
            else hSql += " and custP.Company.Id = " + companyId;

            if (!tags.IsNullOrEmpty())
            {
                hSql += " and custP.Tags like :tags ";
                args.Add(new System.Tuple<string, string>("tags", tags));
            }

            if (!keyword.IsNullOrEmpty())
            {
                hSql += string.Format(@"  
                and lang.Visible = 1 and lang.LangCode = '{0}' 
                and (lang.Name like :keyword ", LanguageMgr.FocusLangCode);

                if (datafields != null && datafields.Length != 0)
                    hSql += " or " + datafields.Select((f, i) =>
                    {
                        if (f.IsNullOrEmpty()) return null;
                        args.Add(new System.Tuple<string, string>("keyword" + i, "%\"" + f + "\":\"%" + keyword + (i == datafields.Length - 1 ? "%\"%" : "%\",\"" + datafields[i+1] + "\"%")));
                        return " custP.Data like :keyword" + i;
                    }).Where(m => m != null).ToString(" or ");

                hSql += ")";

                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            hSql += string.Format(" order by custP.{0} {1}", sortField, GetDir(sortDirection));

            var ret = GetList<CustomerProduct>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            return ret;
        }

        #endregion

        #region UpdateProducts
        public static bool UpdateProducts(IDB db, int id, int? companyid, string tags, List<System.Tuple<int, bool?, Func<string, string>>> items = null, Func<string, string> transformFunc = null)
        {
            if (items.SafeCount() == 0 && transformFunc == null) return false;

            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            from {0} as custP
            where ", typeof(CustomerProduct).FullName);

            if (companyid != null) hSql += " custP.Company.Id = " + companyid;
            else hSql += " custP.Customer.Id = "+id;

            if (!tags.IsNullOrEmpty())
            {
                hSql += " and custP.Tags like :tags ";
                args.Add(new System.Tuple<string, string>("tags", tags));
            }

            if (items.SafeCount() != 0) 
                hSql += string.Format(" and custP.Id in ({0}) ", items.Select(m => m.Item1).ToString(","));
            else 
                if (transformFunc != null) hSql += " and custP.Active = 1 ";

            var lst = GetList<CustomerProduct>(db, hSql, args.ToArray());
            
            if (items.SafeCount() != 0)
                lst.Join(items, o => o.Id, i => i.Item1, (o, i) => new {o, i})
                    .Action(m =>
                    {
                        m.o.Active = m.i.Item2 ?? m.o.Active;
                        if (m.i.Item3 != null) 
                            m.o.Data = m.i.Item3(m.o.Data);
                    });

            if (transformFunc != null)
                lst.Action(m => m.Data = transformFunc(m.Data));

            if (lst.Update(db.DataService))
            {
                //new CustomerMgr().OnUpdate(obj); 
                Helper.OrderScope.Invalidate();

                return true;
            }
            else return false;
        }
        #endregion

        #region GetTagStats

        public static List<System.Tuple<string, int>> GetTagStats(IDB db, int id, int? companyId, string[] tags)
        {
            return Helper.CacheHelper<Helper.OrderScope, List<System.Tuple<string, int>>>.Get(
                string.Format("CustomerMgr.GetTagStats::{0}::{1}::{2}", id, companyId, tags == null ? "" : tags.ToString(";")),
                () => GetTagStatsRaw(db, id, companyId, tags), 15 * 60, false, false);
        }

        public static List<System.Tuple<string, int>> GetTagStatsRaw(IDB db, int id, int? companyId, string[] tags)
        {         
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select custP.Tags, count(*)
            from {0} as custP
            where custP.Active = 1 ", typeof(CustomerProduct).FullName);

            if (companyId == null) hSql += " and custP.Customer.Id = " + id;
            else hSql += " and custP.Company.Id = " + companyId;

            if (tags.SafeCount() != 0)
            {
                hSql += string.Format(" and ({0}) ", tags.Select((t, i) => "custP.Tags = :tag" + i).ToString(" or "));
                tags.Select((t, i) => new {Key = ":tag" + i, Value = t})
                    .ForEach(m => args.Add(new System.Tuple<string, string>(m.Key, m.Value)));
            }

            hSql += string.Format(" group by custP.Tags ");

            var ret = GetList<object[]>(db, hSql, args.ToArray());

            return ret.Select(m => new System.Tuple<string, int>((string)m[0], m[1].SafeChangeType<int>())).ToList();
        }

        #endregion

        #region ToDom
        public static Customer ToDom(IDB db, CustomerModel model, ISession session = null,
            bool saveChange = false, bool overrideable = true)
        {
            model.Email = (model.Email ?? "").Trim();

            var settings = Helper.SettingsBiz.GetSettings();
            var obj = (GetDom(db, model.Id) ?? Get(db, model.Email))
                      ?? new Customer() { CreatedDate = DateTime.Now, Password = "", PasswordSalt = "", Flags = model.Flags };

            if (obj.Id != 0 && obj.Status.InList(CustStatus.Pending)) obj.Flags = model.Flags;

            #region CustGroups

            // update request #6 from CSHOP-Update ngày 20Sept2017.docx
            if (obj.Id == 0 && settings.Hidden.CustomerDefaultGroupId != 0 && model.CustGroups.SafeCount() == 0)
            {
                var custGroup = CustGroupMgr.GetDom(db, settings.Hidden.CustomerDefaultGroupId); 
                if (custGroup != null)
                {
                    obj.CustomerCGroups.Add(new CustomerCGroup() { Customer = obj, Group = custGroup });

                    custGroup.CustomerNum = null;

                    custGroup.Update(db.DataService);
                }
            }

            if (model.CustGroups != null)
            {
                foreach (var item in model.CustGroups)
                {
                    var lid = item.GroupId;
                    var ditm = (lid == 0 ? null : obj.CustomerCGroups.FirstOrDefault(x => x.Group.Id == lid));
                    if (ditm == null)
                        obj.CustomerCGroups.Add(ditm = new CustomerCGroup() { Customer = obj });

                    ditm.Group = CustGroupMgr.GetDom(db, item.GroupId ?? 0);

                    if (ditm.Id == 0) ditm.Group.CustomerNum = null;
                }

                foreach (var ccg in obj.CustomerCGroups.Where(x => model.CustGroups.All(w => w.GroupId != x.Group.Id)).ToList())
                {
                    ccg.Group.CustomerNum = null;
                    obj.CustomerCGroups.Remove(ccg);
                    if (saveChange)
                        if (session != null) session.Update(ccg.Group);
                        else ccg.Group.Update(db.DataService);
                }
            }

            #endregion

            #region CustCategories

            if (model.CustCategories != null)
            {
                foreach (var item in model.CustCategories.Where(m => !m.IsDataModelEmpty))
                {
                    var lid = item.CategoryId;
                    var ditm = (lid == 0 ? null : obj.CategoryCustomers.FirstOrDefault(x => x.Category.Id == lid));
                    if (ditm == null)
                        obj.CategoryCustomers.Add(ditm = new CategoryCustomer() { Customer = obj });

                    ditm.DiscountRate = item.DiscountRate / 100m;
                    ditm.Notes = item.Notes;

                    ditm.Category = CategoryMgr.GetDom(db, item.CategoryId ?? 0);
                }

                foreach (var cc in obj.CategoryCustomers.Where(x => model.CustCategories.All(w => w.IsDataModelEmpty || w.CategoryId != x.Category.Id)).ToList())
                {
                    obj.CategoryCustomers.Remove(cc);
                }
            }

            #endregion

            #region CustAddresses

            if (model.CustAddresses != null)
            {
                foreach (var item in model.CustAddresses.Where(m => !m.IsDataModelEmpty))
                {
                    var lid = item.Id;
                    var ditm = (lid == 0 ? null : obj.Addresses.FirstOrDefault(x => x.Id == lid));
                    if (ditm == null)
                        obj.Addresses.Add(ditm = new AddressEx() { Customer = obj, CreatedDate = DateTime.Now, IsDefault = item.IsDefault });

                    ditm.Name = item.Name;
                    ditm.Phone = item.Phone;
                    ditm.Mobile = item.Mobile;
                    ditm.Street = item.Street;
                    ditm.Postnr = item.Postnr;
                    ditm.Posted = item.Posted;
                    ditm.City = item.City;

                    ditm.Country = CountryMgr.GetDom(db, item.CountryId ?? 0);
                }

                foreach (var itm in obj.Addresses.Where(x => x.Id != 0 && model.CustAddresses.All(m => m.Id == 0 || m.Id != x.Id)).ToList())
                {
                    obj.Addresses.Remove(itm);
                }
            }

            #endregion

            if (overrideable || obj.Id == 0 || (obj.CompanyName == null && obj.OrgNumber == null && obj.InvoiceEmail == null))
            {
                obj.CompanyName = model.Individual == true ? null : model.CompanyName;
                obj.OrgNumber = model.Individual == true ? null : model.OrgNumber;
                obj.InvoiceEmail = model.Individual == true ? null : model.InvoiceEmail;
                obj.ContactName = (model.Individual == true ? null : model.ContactName) ?? "";
            }

            if (model.Individual == false && !model.ContactName.IsNullOrEmpty())
                obj.ContactName = model.ContactName;

            if (overrideable || obj.Id == 0 || (obj.Telephone == null && obj.Mobile == null && obj.RecvOfferInfo == null))
            {
                obj.Telephone = model.Telephone;
                obj.Mobile = model.Mobile;
                obj.RecvOfferInfo = model.RecvOfferInfo;
            }

            if (overrideable || obj.Id == 0 || obj.All(x => x.StartsWith("Deliver"), x => x == null) || obj.DeliverPostnr.IsNullOrEmpty() || obj.DeliverAddress.IsNullOrEmpty() || obj.DeliverMobile.IsNullOrEmpty())
            {
                obj.DeliverName = model.DeliverName;
                obj.DeliverPhone = model.DeliverPhone;
                obj.DeliverMobile = model.DeliverMobile;
                obj.DeliverAddress = model.DeliverAddress;
                obj.DeliverPostnr = model.DeliverPostnr;
                obj.DeliverPosted = model.DeliverPosted;
                obj.DeliverCity = model.DeliverCity;
                obj.DeliverCountry = CountryMgr.GetDom(db, model.DeliverCountryId ?? 0);
            }

            if (overrideable || obj.Id == 0 || obj.All(x => x.StartsWith("Billing"), x => x == null) || obj.BillingPostnr.IsNullOrEmpty() || obj.BillingAddress.IsNullOrEmpty() || obj.BillingMobile.IsNullOrEmpty())
            {
                obj.BillingName = model.BillingName;
                obj.BillingPhone = model.BillingPhone;
                obj.BillingMobile = model.BillingMobile;
                obj.BillingAddress = model.BillingAddress;
                obj.BillingPostnr = model.BillingPostnr;
                obj.BillingPosted = model.BillingPosted;
                obj.BillingCity = model.BillingCity;
                obj.BillingCountry = CountryMgr.GetDom(db, model.BillingCountryId ?? 0);
            }

            if (overrideable || obj.Id == 0)
            {
                obj.Email = model.Email;
                obj.Password = string.IsNullOrEmpty(model.Password) ? obj.Password : model.Password;
                obj.PasswordSalt = string.IsNullOrEmpty(model.PasswordSalt) ? obj.PasswordSalt : model.PasswordSalt;
                
                if (obj.Id == 0)
                {
                    var defStatus = obj.CompanyName.IsNullOrEmpty() ? settings.Hidden.CustomerDefaultStatus : settings.Hidden.CompanyCustomerDefaultStatus;
                    var emailActivate = defStatus.InList(CustStatus.Active) && settings.Hidden.CustomerDefActiveStatusRequireEmailActivate;

                    obj.Status = emailActivate ? (int)CustStatus.Pending : defStatus;

                    if (emailActivate)
                    {
                        obj.TokenId = (obj.TokenExpired > DateTime.Now && !obj.TokenId.IsNullOrEmpty()) ? obj.TokenId : Globals.GenerateAlias(Guid.NewGuid().ToString().Replace("-", ""), true);
                        obj.TokenExpired = DateTime.Now.AddMinutes(settings.Hidden.CustomerActivationExpiryMinutes);
                    }
                }
                else
                {
                    obj.Status = model.Status ?? obj.Status;
                    obj.TokenId = model.TokenId;
                    obj.TokenExpired = model.TokenExpired;
                }

                obj.ModifiedDate = DateTime.Now;
            }

            return obj;
        }
        #endregion

        #region GeneratePassword

        public static string GeneratePassword(int length = 7)
        {
            var random = new Random();
            var passTxt = new StringBuilder();

            for (var i = 0; i < length; i++) passTxt.Append(random.Next(0, 35).ToBase36());

            return passTxt.ToString();
        }

        #endregion

        #region OnUpdate

        protected override void OnUpdate(Customer obj)
        {
            OnUpdate(null, obj);
        }

        protected void OnUpdate(IDB db, Customer obj, ISession session = null)
        {
            var db2 = session == null ? (db ?? new DefaultDb()) : new DefaultDb(session);

            if (SettingsBiz.GetSettings().Hidden.CustCompany && obj != null && obj.Company == null && obj.Status.InList(CustStatus.Active) &&
                !obj.CompanyName.IsNullOrEmpty() && !obj.OrgNumber.IsNullOrEmpty())
            {
                obj.Flags |= (int) CustomerFlagType.CustCompanyInited;
                obj.Company = CompanyMgr.GetDom(db2, obj.OrgNumber) ?? CompanyMgr.SaveorUpdate(db2, new CompanyModel()
                {
                    Name = obj.CompanyName, OrgNumber = obj.OrgNumber
                }, session);

                if (obj.Company.Id != 0)
                {
                    obj.Company.UserNum = null;
                    Update(db2, new[] { obj.Company }.ToList(), session == null);
                }

                Update(db2, new[] {obj}.ToList(), session == null);
            }

            try
            {
                Singleton<IMembershipMgr>.Default.FireCustomerUpdated(obj, db2);
            }
            catch (Exception err) { LogErr(err); }

            CustomerScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Customer obj)
        {
        }
        #endregion

        #region ICustomerMgr

        ICustomer ICustomerMgr.GetDom(IDB db, int id, ISession session)
        {
            return CustomerMgr.GetDom(db, id, session);
        }

        ICustomer ICustomerMgr.Get(IDB db, string email)
        {
            return CustomerMgr.Get(db, email);
        }

        #endregion
    }
}