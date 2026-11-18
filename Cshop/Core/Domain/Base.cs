using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Text;
using System.Web.Script.Serialization;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.Models;
using Newtonsoft.Json;
using NHibernate.UserTypes;
using System.Data;
using NHibernate;
using NHibernate.Util;
using NHibernate.SqlTypes;
using Claw.Modules.Eshop.Helper;

namespace Claw.Modules.Eshop.Domain
{
    #region IDom

    public interface IDom
    {
        int Id { get; set; }
    }

    #endregion

    #region IStatus

    public interface IStatus
    {
        int Id { get; set; }
        bool Active { get; set; }
    }

    #endregion

    #region IXmlCustType

    public interface IXmlCustType
    {
        void SetXmlData(string xmlData);
        string GetXmlData();
        object Clone();
    }

    #endregion

    #region IDisplayOrder

    public interface IDisplayOrder
    {
        int DisplayOrder { get; set; }
    }

    #endregion

    #region Tuple

    public class Tuple
    {
        static bool Debug = false;
        public static volatile bool Initialized = false;
        private ISession _session;
        private PropertyInfo _idProp;
        private Type _type;

        public Tuple()
        {
            if (Initialized) _session = Session;
        }

        private static ISession Session
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    const string key = "Claw-Cshop-nhibernate-session";
                    return (ISession)(HttpContext.Current.Items[key] ?? (HttpContext.Current.Items[key] = ((Claw.Core.DAL.DataService)Claw.Core.ClawContext.Current.DataService).SafeProp(m => m.Session)));
                }

                var ds = Claw.Core.ClawContext.Current.DataService;
                return ds == null ? null : ((Claw.Core.DAL.DataService)ds).Session;
            }
        }

        protected virtual List<ICustGroup> CustGroups
        {
            get
            {
                if (HttpContext.Current == null) return null;

                const string key = "Claw-Cshop-CustGroups";

                if (HttpContext.Current.Items.Contains(key)) return (List<ICustGroup>) HttpContext.Current.Items[key];

                bool showPrice;
                decimal? discountRate;
                List<ICustGroup> groups;
                Dictionary<int, decimal> cateDiscountRates;
                if (Singleton<ISessionHelper>.Default.GetUserId(out showPrice, out discountRate, out cateDiscountRates,
                        out groups) > 0)
                {
                    HttpContext.Current.Items[key] = groups;
                    return groups;
                }

                return null;
            }
        }

        [JsonIgnore]
        public virtual IShoppingCart ShoppingCart
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    const string key = "Claw-Cshop-ShoppingCart";
                    return (IShoppingCart)((Debug ? null : HttpContext.Current.Items[key]) ?? (HttpContext.Current.Items[key] = Singleton<IShoppingCartMgr>.Default.Get()));
                }
                return Singleton<IShoppingCartMgr>.Default.Get();
            }
        }

        protected virtual SettingsBiz Settings
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    const string key = "Claw-Cshop-Settings";
                    return (SettingsBiz)(HttpContext.Current.Items[key] ?? (HttpContext.Current.Items[key] = SettingsBiz.GetSettings()));
                }
                return new SettingsBiz();
            }
        }

        public virtual bool IsCurrentSession()
        {
            return _session == Session;
        }

        protected IList<T> Get<T>(ref IList<T> value)
            where T : class
        {
            if (value == null) value = Activator.CreateInstance<List<T>>();
            else
            {
                return value;

                var list = value as NHibernate.Collection.AbstractPersistentCollection;
                var nbag = list as NHibernate.Collection.Generic.PersistentGenericBag<T>;

                lock (nbag != null ? nbag.SyncRoot : value)
                {
                    while (list != null && !list.WasInitialized)
                    {                                                                         
                        var session = Session;

                        if (session == null || _session == null || session == _session|| list.Owner == null) break;

                        _type = _type ?? list.Owner.GetType();
                        _idProp = _idProp ?? _type.GetProperty("Id");

                        if (_idProp == null) break;

                            var idx = list.Role.LastIndexOf('.');
                            var obj = session.Get(_type, _idProp.GetValue(list.Owner));
                            var role = idx != -1 ? list.Role.Substring(idx + 1) : list.Role.Substring(_type.FullName.Length + 1);
                            var ret = (IList<T>) _type.GetProperty(role).GetValue(obj);

                            return obj != null ? ret : value;
                    }
                }
            }

            return value;
        }
    }

    public class Tuple<T> : Tuple
    {
        protected IList<T> _list0;
    }

    public class Tuple<T0, T1> : Tuple<T0>
    {
        protected IList<T1> _list1;
    }

    public class Tuple<T0, T1, T2> : Tuple<T0, T1>
    {
        protected IList<T2> _list2;
    }

    public class Tuple<T0, T1, T2, T3> : Tuple<T0, T1, T2>
    {
        protected IList<T3> _list3;
    }

    public class Tuple<T0, T1, T2, T3, T4> : Tuple<T0, T1, T2, T3>
    {
        protected IList<T4> _list4;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5> : Tuple<T0, T1, T2, T3, T4>
    {
        protected IList<T5> _list5;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6> : Tuple<T0, T1, T2, T3, T4, T5>
    {
        protected IList<T6> _list6;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7> : Tuple<T0, T1, T2, T3, T4, T5, T6>
    {
        protected IList<T7> _list7;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7>
    {
        protected IList<T8> _list8;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8>
    {
        protected IList<T9> _list9;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        protected IList<T10> _list10;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        protected IList<T11> _list11;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
    {
        protected IList<T12> _list12;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
    {
        protected IList<T13> _list13;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
    {
        protected IList<T14> _list14;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
    {
        protected IList<T15> _list15;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
    {
        protected IList<T16> _list16;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
    {
        protected IList<T17> _list17;
    }

    public class Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> : Tuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>
    {
        protected IList<T18> _list18;
    }
    #endregion

    #region XmlList
    
    public class XmlList<T> : IXmlCustType, IEnumerable<T>
    {
        private List<T> _items;
        private string _xmlData;

        protected XmlList(string xmlData)
        {
            _xmlData = xmlData;
        }

        public XmlList()
        {
        }

        public int Count
        {
            get { return Init().Count; }
        }

        private List<T> Init()
        {
            if (_items == null) _items = (List<T>)_xmlData.ToObject<List<T>>() ?? new List<T>();

            return _items;
        }

        public void Add(T item)
        {
            Init().Add(item);
        }

        public bool Remove(T item)
        {
            return Init().Remove(item);
        }

        public T this[int index]
        {
            get { return Init()[index]; }
            set { Init()[index] = value; }
        }

        public void Clear()
        {
            Init().Clear();
        }

        public string GetXmlData()
        {
            return _items == null ? _xmlData : _items.ToXml();
        }

        public void SetXmlData(string xmlData)
        {
            _items = null;
            _xmlData = xmlData;
        }

        public object Clone()
        {
            var ret = Activator.CreateInstance(GetType());
            ((XmlList<T>)ret).SetXmlData(GetXmlData());
            return ret;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Init().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Init().GetEnumerator();
        }
    }
    
    #endregion

    #region XmlType
    
    public class XmlType<T> : IUserType
        where T : class, IXmlCustType 
    {
        object IUserType.NullSafeGet(IDataReader rs, string[] names, object owner)
        {
            var data = NHibernateUtil.String.NullSafeGet(rs, names[0]) as string;
            var obj = Activator.CreateInstance<T>();
            obj.SetXmlData(data);
            return obj;
        }

        void IUserType.NullSafeSet(IDbCommand cmd, object value, int index)
        {
            var data = value == null ? null : ((T) value).GetXmlData();
            NHibernateUtil.String.NullSafeSet(cmd, data, index);
        }

        public object Assemble(object cached, object owner)
        {
            return DeepCopy(cached);
        }

        public object DeepCopy(object value)
        {
            return value == null ? null : ((T) value).Clone();
        }

        public object Disassemble(object value)
        {
            return DeepCopy(value);
        }

        public new bool Equals(object x, object y)
        {
            return EqualsHelper.Equals(x, y);
        }

        public int GetHashCode(object x)
        {
            return x == null ? 0 : x.GetHashCode();
        }

        public bool IsMutable
        {
            get { return true; }
        }

        public object Replace(object original, object target, object owner)
        {
            return original;
        }

        public Type ReturnedType
        {
            get { return typeof(T); }
        }

        public SqlType[] SqlTypes
        {
            get { return new[] { new SqlType(DbType.Xml) }; }
        }
    }    
    #endregion
}