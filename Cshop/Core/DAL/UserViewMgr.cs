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
using NHibernate.Criterion;

namespace Claw.Modules.Eshop.DAL
{
    public class UserViewMgr : BaseMgr<UserViewModel, UserView>
    {
        static UserViewMgr() { Init(typeof(UserViewMgr)); }

        #region GetAll

        public static List<UserViewModel> GetAll(IDB db, int minutes = 10)
        {
            var ret = db.GetSession().CreateCriteria(typeof (UserView))
                .AddOrder(Order.Desc("Timestamp"))
                .SetMaxResults(10000)
                .SetCacheable(true)
                .List<UserView>().ToList();

            return Mapper.Map<List<UserView>, List<UserViewModel>>(ret);
        }

        #endregion

        #region GetAlsoViewItems

        public static List<System.Tuple<int, int>> GetAlsoViewItems(IDB db, int productId, int days = 30, int count = 10)
        {
            var hSql = string.Format(@"
            select top {3} B.RefId, count(*)
            from (
                select UserId 
                from eshop.UserViews
                where RefId = {0}
                and Timestamp > '{1:yyyy-MM-dd}'
                and Type = {2}
                group by UserId
            ) A join eshop.UserViews B on A.UserId = B.UserId
            where B.Timestamp > '{1:yyyy-MM-dd}' 
            and B.RefId <> {0} 
            and B.Type = {2}
            group by B.RefId
            order by count(*) desc ", productId, DateTime.Now.AddDays(-1 * days), UserViewType.Product.Val(), count);

            return db.GetSession().CreateSQLQuery(hSql).List<object[]>().Select(m => new System.Tuple<int, int>(m[0].SafeChangeType<int>(), m[1].SafeChangeType<int>())).ToList();
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, List<UserViewModel> models)
        {
            if (models.SafeCount() == 0) return false;

            var session = db.GetSession();
            var trans = session.BeginTransaction();
            try
            {
                session.SetBatchSize(100);

                const int n = 100;
                var domDic = new Dictionary<Guid, UserView>();
                var ids = models.Where(x => x.Id != Guid.Empty).Select(x => x.Id).ToList();

                for (var i = 0; i < ids.Count; i += n)
                {
                    var tmpLst = new List<object>();
                    for (var k = 0; k < n && i + k < ids.Count; k++) tmpLst.Add(ids[i + k]);
                    if (tmpLst.Count != 0)
                    {
                        var domLst = session.CreateCriteria(typeof (UserView))
                            .Add(Restrictions.In("Id", tmpLst.ToArray()))
                            .SetCacheable(true)
                            .List<UserView>()
                            .Action(x => domDic[x.Id] = x);
                    }
                }

                foreach (var model in models)
                {
                    var obj = domDic.SafeGet(model.Id) ?? new UserView() {Id = Guid.NewGuid()};

                    obj.UserId = model.UserId;
                    obj.RefId = model.RefId;
                    obj.Type = model.Type;
                    obj.Count = model.Count;
                    obj.Timestamp = model.Timestamp;

                    session.SaveOrUpdate(obj);

                    model.Id = obj.Id;

                    //new UserViewMgr().OnUpdate(obj);
                }

                trans.Commit();
                return true;
            }
            catch (Exception err)
            {
                trans.Rollback();
                return false;
            }
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(UserView obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(UserView obj)
        {
        }
        #endregion
    }
}