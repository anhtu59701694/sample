using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using NHibernate;
using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.DAL.Interfaces
{
    public interface IPaymentMethodMgr : IDalMgr
    {
        IPaymentMethod GetDom(IDB db, int id, ISession session = null);
        List<IPaymentMethod> GetAll(IDB db, int? id, bool? active = null);
        List<IPaymentMethod> GetAll(IDB db, RecurringOrderMode recurringMode, bool? active = null);
    }
}