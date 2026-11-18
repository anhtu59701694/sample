using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Modules.Eshop.Helper;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.DAL
{
    public class CompanyMgr : BaseMgr<CompanyModel, Company>
    {
        static CompanyMgr() { Init(typeof(CompanyMgr)); } 

        #region GetAll

        public static List<CompanyModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", 
            string keyword = null, bool bindStats = false)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select comp from {0} as comp ", typeof(Company).FullName);

            if (!keyword.IsNullOrEmpty())
            {
                hSql += " and (comp.Name like :keyword or comp.OrgNumber like :keyword ";
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            hSql += string.Format(" order by {0} {1}", "comp." + sortField, GetDir(sortDirection));

            var ret = GetList<Company>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            if (bindStats) CustomerMgr.BindStats(db, ret);

            return Mapper.Map<List<Company>, List<CompanyModel>>(ret);
        }

        #endregion

        #region GetDom
        public static Company GetDom(IDB db, string orgNum)
        {
            if (orgNum.IsNullOrEmpty()) return null;

            return db.DataService.GetObject<Company>(GetSiteId(db), "OrgNumber", orgNum.Trim());
        } 
        #endregion

        #region SaveorUpdate

        public static Company SaveorUpdate(IDB db, CompanyModel model, ISession session = null)
        {
            if (model == null) return null;

            var obj = GetDom(db, model.Id) ?? (GetDom(db, model.OrgNumber) ?? new Company()
            {
                OrgNumber = model.OrgNumber,
                CreatedDate = DateTime.Now,
            });

            obj.Name = model.Name;
            obj.Data = model.Data;
            obj.Owner = CustomerMgr.GetDom(db, model.OwnerId ?? 0);

            if (SaveOrUpdate(obj, session ?? db.GetSession(), session == null))
            {
                model.Id = obj.Id;
                new CompanyMgr().OnUpdate(obj);

                return obj;
            }
            else return null;
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(Company obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Company obj)
        {
        }
        #endregion
    }
}