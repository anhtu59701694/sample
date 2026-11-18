using System;
using System.Collections.Generic;
using System.Web;
using Claw.Modules.Eshop.Domain;
using NHibernate;
using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.DAL.Interfaces
{
    public interface IGiftCardMgr : IDalMgr
    {
        IGiftCard Get(string code, IDB db = null);

        bool Apply(IDB db, int id, decimal value, int orderId, ISession session, HttpRequestBase request, bool autoSave = true);

        bool Refund(IDB db, int id, decimal value, int orderId, ISession session, HttpRequestBase request, bool autoSave = true);

        IDom Create(IDB db, decimal amount, string name, string email, string phone, DateTime expiredDate, bool? creditNote, IDom orderDetail, ISession session = null);
    }
}