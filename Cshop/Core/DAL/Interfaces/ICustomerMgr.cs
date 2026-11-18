using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Domain.Interfaces;
using NHibernate;

namespace Claw.Modules.Eshop.DAL.Interfaces
{
    public interface ICustomerMgr : IDalMgr
    {
        ICustomer GetDom(IDB db, int id, ISession session = null);
        ICustomer Get(IDB db, string email);
    }
}