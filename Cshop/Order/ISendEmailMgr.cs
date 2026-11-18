using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Models;
using System.Web.Mvc;

namespace Claw.Modules.Eshop
{
    public interface ISendEmailMgr
    {
        bool Send(OrderModel model, byte status, string comment, bool payOnline = true);

        bool Send(Controllers.Admin.OrderController controller, int id, byte status, string comment, bool statusChange, string prodList = null, bool newOrder = false, bool? recurring = null);

        bool Send(Controller controler, OrderModel model, byte status, string comment, bool payOnline = true);

        bool Send(Controller controler, OrderModel model, byte status, string comment, EmailTemplModel template, bool payOnline = true); 

        bool EmailCustomer(IDB db, EmailTemplateType templType, int customerid, string password = null);
    }
}