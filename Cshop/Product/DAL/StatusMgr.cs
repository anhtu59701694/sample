using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.Helper;

namespace Claw.Modules.Eshop.DAL
{
    public class StatusMgr : BaseMgr<StatusModel, Status>
    {
        static StatusMgr() { Init(typeof(StatusMgr)); }

        #region GetAll

        public static List<StatusModel> GetAll(IDB db, string langCode,
            int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select status from {0} as status join status.Languages as lang
            where lang.LangCode = '{1}' ", typeof(Status).FullName, langCode);

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += string.Format(@" and lang.Name like :keyword ");
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "status.") + sortField, GetDir(sortDirection));

            var ret = GetList<Status>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            return Mapper.Map<List<Status>, List<StatusModel>>(ret);
        }

        public static List<StatusModel> GetAll(IDB db, int? id)
        {
            int total;
            return GetAll(db, LanguageMgr.OptLangCode(id ?? 0), 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name");
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, StatusModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            #region Status

            if (obj != null)
            {
                obj.Code = model.Code;

                obj.ShowIcon = model.ShowIcon;
                obj.ContactLink = model.ContactLink;
                obj.DescInstock = model.DescInstock;

                obj.Icon = model.Icon;
                obj.EnableBuy = model.EnableBuy;
            }
            else obj = new Status()
            {
                Code = model.Code,

                ShowIcon = model.ShowIcon,
                ContactLink = model.ContactLink,
                DescInstock = model.DescInstock,
                Icon = model.Icon,
                EnableBuy = model.EnableBuy
            };

            obj.ShowPrice = model.ShowPrice;
            obj.ShowFrontEnd = model.ShowFrontEnd;


            //obj.ZeroQtyStatus = StatusMgr.GetDom(db, model.ZeroQtyStatusId ?? 0);

            #endregion

            #region Languages
            if (model.Languages != null)
            {
                foreach (var item in model.Languages)
                {
                    var lid = item.Id;
                    var ditm = (lid == 0 ? null : obj.Languages.FirstOrDefault(x => x.Id == lid));
                    if (ditm == null)
                    {
                        obj.Languages.Add(new StatusLang()
                        {
                            Status = obj,
                            Name = item.Name,
                            LangCode = item.LangCode,
                        });
                    }
                    else ditm.Name = item.Name;
                }

                obj.Languages.RemoveAll(x => model.Languages.All(w => w.Id != x.Id));
            }
            #endregion

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new StatusMgr().OnUpdate(obj); return true;
            }
            else return false;
        }
        #endregion

        #region Get
        public static Status GetDom(IDB db, string code)
        {
            return db.DataService.GetObject<Status>(0, "Code", code);
        }

        public static StatusModel Get(IDB db, string code)
        {
            return Mapper.Map<Status, StatusModel>(GetDom(db, code));
        }
        #endregion

        #region Update
        public static bool Update(IDB db, int id, bool? defVal = null, bool? enableBuy = null, bool? showPrice = null, bool? showFrontEnd = null)
        {
            if (defVal == null && enableBuy == null && showPrice == null && showFrontEnd == null) return false;

            var obj = GetDom(db, id);

            if (obj == null) return false;

            if (enableBuy != null) obj.EnableBuy = enableBuy.Value;

            if (showPrice != null) obj.ShowPrice = showPrice.Value;

            if (showFrontEnd != null) obj.ShowFrontEnd = showFrontEnd.Value;

            if (obj.SaveorUpdate(db.DataService))
            {
                return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(Status obj)
        {
            CombineScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Status obj)
        {
            CombineScope.Invalidate();
        }
        #endregion
    }
}