using AutoMapper;
using Claw.Core.Interfaces;
using Claw.Modules.Eshop;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using NHibernate;
using NHibernate;
using NHibernate.Event;
using NHibernate.Type;
using NHibernate.Type;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace Claw.Modules.Eshop
{
    public class AutoMapper
    {
        public static void Configuration()
        {
            Domain.Tuple.Initialized = true;
            new DAL.DelayQueueMgr();

            System.Threading.Tasks.Task.Run(() =>
            {
                System.Threading.Tasks.Task.Delay(5000).Wait();
                //Claw.Core.IoC.Kernel.Load(new NHibernateModuleCshop());
            });

            Mapper.CreateMap<EmailTempl, EmailTemplModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m));
            Mapper.CreateMap<EmailTemplLang, EmailTemplLangModel>()
                .ForMember(m => m.EmailTemplId, conf => conf.MapFrom(m => m.EmailTempl.Id));

            Mapper.CreateMap<Country, CountryModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.RegionName, conf => conf.MapFrom(m => m.Region.GetName(null)));
            Mapper.CreateMap<CountryLang, CountryLangModel>()
                .ForMember(m => m.CountryId, conf => conf.MapFrom(m => m.Country.Id));

            Mapper.CreateMap<City, CityModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.CountryName, conf => conf.MapFrom(m => m.Country.GetName(null)));
            Mapper.CreateMap<CityLang, CityLangModel>()
                .ForMember(m => m.CityId, conf => conf.MapFrom(m => m.City.Id));

            Mapper.CreateMap<Region, RegionModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m));
            Mapper.CreateMap<RegionLang, RegionLangModel>()
                .ForMember(m => m.RegionId, conf => conf.MapFrom(m => m.Region.Id));

            Mapper.CreateMap<District, DistrictModel>()
                .ForMember(m => m.CityId, conf => conf.MapFrom(m => m.City.Id))
                .ForMember(m => m.CityName, conf => conf.MapFrom(m => m.City.GetName(null)));

            Mapper.CreateMap<Permission, PermissionModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Id, conf => conf.MapFrom(m => Math.Max(0, m.Id)))
                .ForMember(m => m.RoleId, conf => conf.MapFrom(m => m.Role.GetId(null)))
                .ForMember(m => m.RoleName, conf => conf.MapFrom(m => m.Role.GetName()))
                .ForMember(m => m.RoleDescription, conf => conf.MapFrom(m => m.Role.SafeProp(x => x.Description)));
            Mapper.CreateMap<Claw.Core.Domain.User, UserModel>()
                .ForMember(m => m.Roles, conf => conf.MapFrom(m => m.GetUserRoles()));

            Mapper.CreateMap<ActionLog, ActionLogModel>();
            Mapper.CreateMap<Trace, TraceModel>();
            Mapper.CreateMap<Currency, CurrencyModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Rate, conf => conf.MapFrom(m => new OFloat(m.Rate) { DecimalPlaces = 5 }))
                .ForMember(m => m.ThousandSep, conf => conf.MapFrom(m => m.ThousandSep.EncodeBase64()))
                .ForMember(m => m.Default, conf => conf.MapFrom(m => m.IsDefault()));
            Mapper.CreateMap<Task, TaskModel>()
                .ForMember(m => m.LastRunTime, conf => conf.MapFrom(m => m.LastRunTime.ToODateTime()))
                .ForMember(m => m.LastDoneTime, conf => conf.MapFrom(m => m.LastDoneTime.ToODateTime()));
            Mapper.CreateMap<TaskLog, TaskLogModel>();

            Mapper.CreateMap<UserView, UserViewModel>();
        }
    }

    public class NHibernateModuleCshop : Claw.Core.Configurations.NHibernateModule
    {
        public override void Load()
        {
            Claw.Core.IoC.Kernel.Get<Claw.Core.Configurations.INHibernateConfiguration>()
                .GetConfig().SetInterceptor(new CshopInterceptor());

            Unbind<ISessionFactory>();

            Bind<ISessionFactory>().ToMethod(context => context.Kernel.Get<Claw.Core.Configurations.INHibernateConfiguration>()
                .GetConfig().BuildSessionFactory()).InSingletonScope();
        }
    }

    public class CshopInterceptor : EmptyInterceptor
    {
        private int updates;
        private int creates;
        private int loads;

        public override NHibernate.SqlCommand.SqlString OnPrepareStatement(NHibernate.SqlCommand.SqlString sql)
        {
            if (sql?.Count > 0 && !sql.StartsWithCaseInsensitive("select") && HttpContext.Current != null)
            {
                Log(sql.ToString());
            }
            return sql;
        }

        public override void OnDelete(object entity, object id, object[] state, string[] propertyNames, IType[] types)
        {
        }

        public override bool OnFlushDirty(object entity, object id, object[] currentState, object[] previousState, string[] propertyNames, IType[] types)
        {
            if (entity != null && HttpContext.Current != null) Log($"OnFlushDirty [{entity.GetType().Name}]: id={id}");
            updates++;
            return false;
        }

        public override bool OnLoad(object entity, object id, object[] state, string[] propertyNames, IType[] types)
        {
            loads++;
            return false;
        }

        public override bool OnSave(object entity, object id, object[] state, string[] propertyNames, IType[] types)
        {
            if (entity != null && HttpContext.Current != null) Log($"OnSave [{entity.GetType().Name}]: id={id}");
            creates++;
            return false;
        }

        public override void AfterTransactionCompletion(ITransaction tx)
        {
            Log($"AfterTransactionCompletion");

            if (tx != null && tx.WasCommitted)
                System.Console.WriteLine("Creations: " + creates +     ", Updates: " + updates +  ", Loads: " + loads);
            
            updates = 0;
            creates = 0;
            loads = 0;
        }

        public override void AfterTransactionBegin(ITransaction tx)
        {
            Log($"AfterTransactionBegin");
        }

        public override void BeforeTransactionCompletion(ITransaction tx)
        {
            Log($"BeforeTransactionCompletion");
        }

        #region Log
        public static void Log(string msg, params object[] args)
        {
            try
            {
                var folder = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('/', '\\') + @"\logs";

                if (!System.IO.Directory.Exists(folder)) return;

                var cxt = HttpContext.Current;
                var path = folder + string.Format(@"\sql.{0}.log", DateTime.Now.ToString("yyyy.MM.dd"));
                var contents = string.Format("[{0}]['{3}']{1}{2}{1}{1}", DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss fff"), Environment.NewLine, args == null || args.Length == 0 ? msg : string.Format(msg, args), cxt == null ? null : cxt?.Request?.RawUrl);

                System.IO.File.AppendAllText(path, contents);
            }
            catch
            {
            }
        }
        #endregion

    }
}