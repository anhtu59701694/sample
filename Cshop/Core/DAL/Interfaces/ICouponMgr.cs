using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Domain.Interfaces;
using NHibernate;

namespace Claw.Modules.Eshop.DAL.Interfaces
{
    public interface ICouponMgr : IDalMgr
    {
        ICoupon GetDom(IDB db, int id, ISession session = null);
        ICoupon Get(string code);
    }
}