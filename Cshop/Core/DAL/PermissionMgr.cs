using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.Enumerations;
using Claw.Modules.Eshop.Helper;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using CoreDom=Claw.Core.Domain;

namespace Claw.Modules.Eshop.DAL
{
    public class PermissionMgr : BaseMgr<PermissionModel, Permission>
    {
        static PermissionMgr() { Init(typeof(PermissionMgr)); }

        #region GetAll

        public static List<PermissionModel> GetAll(IDB db, int? userid, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSqlPerm = string.Format(@"
            select perm from {0} as perm where 1 = 1 ", typeof (Permission).FullName);

            if (userid != null)
            {
                var uroles = db.DataService.GetObject<CoreDom.User>(userid ?? 0).SafeProp(x => x.Usercms_UserRoleses);
                var roleids = uroles == null || uroles.Count == 0 ? "0" : uroles.Cast<CoreDom.UserRole>().Select(x => x.Role.Id).ToString(",");
                hSqlPerm += string.Format(" and perm.Role.Id in ({0}) ", roleids);
            }

            var hSqlRole = string.Format(@"
            select role from {0} as role
            where (role.Site.Id is null or role.Site.Id = {1})
            and role.Active = 1 
            and role.RoleName is not null
            and (role.MaskPermission&{2}) = {2}
            and role.RoleName not in ('Administration','Anonymous') ", typeof(CoreDom.Role).FullName, GetSiteId(db), AdminPermissionType.Eshop.Val());

            hSqlRole += string.Format(" order by {0} {1}", "role." + sortField, GetDir(sortDirection));

            var retRole = GetList<CoreDom.Role>(db, hSqlRole, pageIndex, pageSize, out totalRows);

            if (retRole.Count != 0 && retRole.Count < 50)
                hSqlPerm += string.Format(" and perm.Role.Id in ({0}) ", retRole.Select(x => x.Id).ToString(","));

            var retPerm = GetList<Permission>(db, hSqlPerm)
                .Where(x => x.Role != null).Distinct(x => x.Role.Id).ToDictionary(x => x.Role.Id, x => x);

            var ret = retRole.Select(x =>
            {
                var p = retPerm.SafeGet(x.Id);
                return p ?? new Permission() { Id = -1*x.Id, Role = x, CreatedDate = DateTime.Now, ModifiedDate = DateTime.Now };
            }).ToList();

            return Mapper.Map<List<Permission>, List<PermissionModel>>(ret);
        }

        public static List<PermissionModel> GetAll(IDB db, int? userid)
        {
            int totalRows;
            return GetAll(db, userid, 1, int.MaxValue, out totalRows);
        }

        #endregion

        #region Get
        public static new PermissionModel Get(IDB db, int id)
        {
            if (id == 0) return null;

            var obj = GetDom(db, id) ?? (id < 0
                ? new Permission() { Role = db.DataService.GetObject<CoreDom.Role>(-1*id), CreatedDate = DateTime.Now, ModifiedDate = DateTime.Now }
                : null);

            return obj == null ? null : Mapper.Map<Permission, PermissionModel>(obj);
        }

        public static new Permission GetDom(IDB db, int id, ISession session = null)
        {
            if (id == 0) return null;

            if (id < 0)
                return GetList<Permission>(session == null ? db : new DefaultDb(session),
                    string.Format("from {0} where Role.Id = {1}", typeof (Permission).FullName, -1*id)).FirstOrDefault(); 

            if (session != null) return session.Get<Permission>(id);
            else return db.DataService.GetObject<Permission>(id);
        }

        #endregion

        #region CurrentPerm

        public static PermissionModel CurrentPerm
        {
            get
            {
                const string key = "eshop-current-user-permission";
                var ret = (PermissionModel) HttpContext.Current.Items[key];
                if (ret == null)
                {
                    var lst = GetAll(new DefaultDb(), CurrentUser.Id);
                    ret = new PermissionModel()
                    {
                        Id = -1,
                        RoleId = -1,
                        Bitmask = CurrentUser.IsAdmin() ? -1 : 0,
                        RoleName = "Combined role",
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                    };
                    lst.Action(x => ret.Bitmask |= x.Bitmask);
                    HttpContext.Current.Items[key] = ret;
                }
                return ret;
            }
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, PermissionModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? GetDom(db,-1*model.RoleId) ?? new Permission(){CreatedDate = DateTime.Now};

            obj.Bitmask = model.Bitmask;
            obj.ModifiedDate = DateTime.Now;

            obj.Role = db.DataService.GetObject<CoreDom.Role>(model.RoleId);

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new PermissionMgr().OnUpdate(obj);

                return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(Permission obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Permission obj)
        {
        }
        #endregion
    }

    #region UserMgr
    public class UserMgr : BaseMgr<UserModel, CoreDom.User>
    {
        #region GetAll

        public static List<UserModel> GetAll(IDB db, int? roleId, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select distinct usr from {0} as usr join usr.Usercms_UserRoleses as urole
            where (usr.Site.Id is null or usr.Site.Id = {1})
            and (urole.Role.MaskPermission&{2}) = {2}
            and urole.Role.RoleName not in ('Administration','Anonymous') ",
                typeof(CoreDom.User).FullName, GetSiteId(db), AdminPermissionType.Eshop.Val());

            if (roleId != null) hSql += " and urole.Role.Id = " + roleId;

            hSql += string.Format(" order by {0} {1}", "usr." + sortField, GetDir(sortDirection));

            var ret = GetList<CoreDom.User>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<CoreDom.User>, List<UserModel>>(ret);
        }

        #endregion
    } 
    #endregion
}