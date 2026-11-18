using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Status : Tuple<ProductBase, StatusLang, VariantBase>
    {
        public const string StatusIDEqual = "10";

        #region Stat
        public class Stat
        {
            public int Id { get; set; }
            public int Count { get; set; }
            public int CountBundle { get; set; }
        } 
        #endregion

        public virtual int Id { get; set; }
        public virtual string Code { get; set; }
        public virtual bool ContactLink { get; set; }
        public virtual bool EnableBuy { get; set; }
        public virtual bool ShowIcon { get; set; }
        public virtual string Icon { get; set; }
        public virtual bool DescInstock { get; set; }
        public virtual bool ShowPrice { get; set; }
        public virtual bool ShowFrontEnd { get; set; }

        public virtual IList<StatusLang> Languages
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }

        public virtual IList<ProductBase> Products
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<VariantBase> Variants
        {
            get { return Get(ref _list2); }
            set { _list2 = value; }
        }
    }

    public class StatusStatList : XmlList<Status.Stat> { }
    public class StatusStatListType : XmlType<StatusStatList> { }

}