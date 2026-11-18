using System.Collections.Generic;
using Claw.Modules.Eshop.Domain;
using NHibernate;
using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.DAL.Interfaces
{
    public interface ICustGroupMgr : IDalMgr
    {
        ICustGroup GetDom(IDB db, int id, ISession session = null);
        List<ICustGroup> GetAll(IDB db);
    }
}