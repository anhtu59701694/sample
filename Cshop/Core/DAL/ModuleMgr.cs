using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Interfaces;
using Claw.Core.Domain;

namespace Claw.Modules.Eshop.DAL
{
    public class ModuleMgr
    {
        public static readonly string Name;
        public static readonly string FullName;

        #region ModuleMgr.static
        static ModuleMgr()
        {
            Name = typeof(ModuleMgr).Module.Name;

            int iL = Name.LastIndexOf('.');
            int iM = Name.LastIndexOf('.', iL - 1);

            if (iL != -1) FullName = Name.Substring(0, iL);
            if (iL != -1 && iM != -1) Name = "Eshop";//Name.Substring(iM + 1, iL - iM - 1);
        } 
        #endregion

        #region Get
        public static Module Get(IDataService db, string name = null)
        {
            return Helper.DataCache<Helper.DefaultScope, Module>.Get(string.Format("ModuleMgr.Get:{0}", name ?? Name), () =>
            {
                var site = db.GetCurrentSite();
                if (site == null) return null;
                else return db.GetModuleByName(name ?? Name, site.Id);
            }, 15);
        } 
        #endregion

        public static bool IsEmbedMode { get { return Type.GetType("Claw.Web.MvcApplication,Claw.Web") == null; } }

        public static bool IsMembershipActive { get { return Get(new DefaultDb().DataService, "Membership").SafeProp(m => m.Active); } }
    }
}
