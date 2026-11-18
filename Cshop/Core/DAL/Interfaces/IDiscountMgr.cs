using System.Collections.Generic;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using NHibernate;
using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.DAL.Interfaces
{
    public interface IDiscountMgr : IDalMgr
    {
        IDiscount GetDom(IDB db, int id, ISession session = null);

        void BindStats(IDB db, List<ICustGroup> list);

        void OnProductRemoved(IDB db, List<IProduct> list);

        int? SaveOrUpdate(IDB db, int paymentMethodId, RecurringType type, decimal value);
        
        IDiscount SaveOrUpdate(IDB db, List<int> paymentMethodIds, RecurringType type, decimal value, decimal? oldVal, int productId);

        List<IDiscount> ListAppliable(IDB db, decimal orderTotal, int itemTotal,
            int? id = null, List<int> custGroupIds = null);

        List<IDiscount> ListAppliable(IDB db, int productid, int? variantid, int quantity,
            int? id = null, List<int> custGroupIds = null);

        List<IDiscount> GetAll(IDB db, int type, int? paymentMethodId = null);
    }
}