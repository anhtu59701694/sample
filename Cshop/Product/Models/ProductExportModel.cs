using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Claw.Modules.Eshop.DAL;
using Claw.Core.DAL;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(ProductExportModel), "Validate")]
    public class ProductExportModel
    {
        #region PropertyModel

        public class PropertyModel
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Value { get; set; }

            public override string ToString()
            {
                return string.Format("{0} : {1}", Name, Value);
            }
        }

        #endregion

        #region VariantModel

        public class VariantModel
        {
            private Models.VariantModel _raw;

            public VariantModel(Models.VariantModel variant)
            {
                _raw = variant;
            }

            public int Id { get { return _raw.Id; } set { _raw.Id = value; } }

            public string Image { get { return _raw.ImageExpList; } set { _raw.ImageExpList = value; } }

            [Display(Name = "Price (exc VAT)")]
            public OMoney Price { get { return _raw.Price; } set { _raw.Price = value; } }

            [Display(Name = "List price (incl. VAT)")]
            public OMoney PriceInc { get { return _raw.PriceInc; } set { _raw.PriceInc = value; } }

            [Display(Name = "Sale price (excl. VAT)")]
            public OMoney SalePrice { get { return _raw.SalePrice; } set { _raw.SalePrice = value; } }

            [Display(Name = "Sale price (incl. VAT)")]
            public OMoney SalePriceInc { get { return _raw.SalePriceInc; } set { _raw.SalePriceInc = value; } }

            [Display(Name = "Buying price (excl. VAT)")]
            public OMoney CostPrice { get { return _raw.CostPrice; } set { _raw.CostPrice = value; } }

            [Display(Name = "Buying price(incl. VAT)")]
            public OMoney CostPriceInc { get { return _raw.CostPriceInc; } set { _raw.CostPriceInc = value; } }

            [Display(Name = "Variant status")]
            public string ProductStatusName { get { return _raw.ProductStatusName; } set { _raw.ProductStatusName = value; } }

            internal int? ProductStatusId { get { return _raw.ProductStatusId; } set { _raw.ProductStatusId = value; } }

            [Display(Name = "Quantity")]
            public int Quantity { get { return _raw.Quantity; } set { _raw.Quantity = value; } }

            [Display(Name = "BackOrder qty")]
            public int BackOrderQty { get { return _raw.BackOrderQty; } set { _raw.BackOrderQty = value; } }

            public string SKU { get { return _raw.SKU; } set { _raw.SKU = value; } }

            [Display(Name = "Barcode")]
            public string BarCode { get { return _raw.BarCode; } set { _raw.BarCode = value; } }

            public OFloat Weight { get { return _raw.Weight; } set { _raw.Weight = value; } }
        }

        #endregion

        private bool _reuseModel;
        private string _categoryPath;
        private string _statusName;
        private readonly ProductModel _raw;
        private readonly VariantModel _variant;
        private List<CategoryModel> _categories;
        private List<StatusModel> _statuses;
        private List<ManufacturerModel> _manus;

        public ProductExportModel(ProductModel product, VariantModel variant, List<CategoryModel> categories = null,
            List<StatusModel> statuses = null, List<ManufacturerModel> manus = null, bool reuseModel = false)
        {
            _reuseModel = reuseModel;
            _raw = product;
            _variant = variant;
            _categories = categories;
            _statuses = statuses;
            _manus = manus;
        }

        public int Id { get { return _raw.Id; } set { _raw.Id = value; } }

        private bool CanSetProdMember { get { return !_reuseModel || _variant == null; } }

        [Display(Name = "Url name")]
        public string Alias { get { return _raw.Alias; } set { if (CanSetProdMember) _raw.SetAlias(value); } }

        public string Name { get { return _raw.Name; } set { if (CanSetProdMember) _raw.SetName(value); } }

        [Display(Name = "Tag")]
        public string Tags { get { return _raw.ActiveLang.Tags; } set { if (CanSetProdMember) _raw.ActiveLang.Tags = value; } }

        [Display(Name = "Description")]
        public string Description { get { return _raw.ActiveLang.Description; } set { if (CanSetProdMember) _raw.ActiveLang.Description = value; } }

        [Display(Name = "Summary info")]
        public string ExtraInfo { get { return _raw.ActiveLang.ExtraInfo; } set { if (CanSetProdMember) _raw.ActiveLang.ExtraInfo = value; } }

        [Display(Name = "Page title")]
        public string PageTitle { get { return _raw.ActiveLang.PageTitle; } set { if (CanSetProdMember) _raw.ActiveLang.PageTitle = value; } }

        [Display(Name = "Meta description")]
        public string MetaDesc { get { return _raw.ActiveLang.MetaDesc; } set { if (CanSetProdMember) _raw.ActiveLang.MetaDesc = value; } }

        [Display(Name = "Manufacturer name")]
        public string ManufacturerName
        {
            get { return _raw.ManufacturerName; }
            set
            {
                if (CanSetProdMember)
                {
                    _raw.ManufacturerName = value;
                    if (_manus != null && !value.IsNullOrEmpty())
                    {
                        var manu = _manus.FirstOrDefault(x => x.Languages.Any(z => z.Name.EqualsIgCase(value)));
                        _raw.ManufacturerId = manu != null ? manu.Id : (int?)null;
                    }
                }
            }
        }

        [Display(Name = "Image")]
        public string Image
        {
            get { return _variant == null ? _raw.ImageExpList : _variant.Image; }
            set { if (_variant != null) _variant.Image = value; else _raw.ImageExpList = value; }
        }

        [Display(Name = "Product sku")]
        public string SKU { get { return _raw.SKU; } set { _raw.SKU = value; } }

        [Display(Name = "Variant sku")]
        public string VariantSKU { get { return _variant == null ? null : _variant.SKU; } set { if (_variant != null) _variant.SKU = value; } }

        [Display(Name = "Tax")]
        public int? Vat { get { return _raw.Vat == null ? null : (int?)(_raw.Vat.Value * 100); } set { if (CanSetProdMember) _raw.Vat = value == null ? null : value / 100d; } }

        [Display(Name = "Price (exc VAT)")]
        public OMoney Price
        {
            get { return _variant == null ? _raw.Price : _variant.Price; }
            set { if (_variant != null) _variant.Price = value; else _raw.Price = value; }
        }

        [Display(Name = "List price (incl. VAT)")]
        public OMoney PriceInc
        {
            get { return _variant == null ? _raw.PriceInc : _variant.PriceInc; }
            set { if (_variant != null) _variant.PriceInc = value; else _raw.PriceInc = value; ; }
        }

        [Display(Name = "Sale price (excl. VAT)")]
        public OMoney SalePrice
        {
            get { return _variant == null ? _raw.SalePrice : _variant.SalePrice; }
            set { if (_variant != null) _variant.SalePrice = value; else _raw.SalePrice = value; ; }
        }

        [Display(Name = "Sale price (incl. VAT)")]
        public OMoney SalePriceInc
        {
            get { return _variant == null ? _raw.SalePriceInc : _variant.SalePriceInc; }
            set { if (_variant != null) _variant.SalePriceInc = value; else _raw.SalePriceInc = value; }
        }

        [Display(Name = "Buying price (excl. VAT)")]
        public OMoney CostPrice
        {
            get { return _variant == null ? _raw.CostPrice : _variant.CostPrice; }
            set { if (_variant != null) _variant.CostPrice = value; else _raw.CostPrice = value; }
        }

        [Display(Name = "Buying price(incl. VAT)")]
        public OMoney CostPriceInc
        {
            get { return _variant == null ? _raw.CostPriceInc : _variant.CostPriceInc; }
            set { if (_variant != null) _variant.CostPriceInc = value; else _raw.CostPriceInc = value; }
        }

        [Display(Name = "Status")]
        public string ProductStatusName
        {
            get { return _statusName; }
            set
            {
                _statusName = value;
                if (_statuses != null && !value.IsNullOrEmpty())
                {
                    var statusId = _statuses.FirstOrDefault(x => x.Languages.Any(z => z.Name.EqualsIgCase(value))).SafeProp(x => x.Id).NullIf(x => x <= 0);
                    if (_variant != null)
                    {
                        _variant.ProductStatusId = statusId;
                        _raw.ProductStatusId = _raw.ProductStatusId > 0 ? _raw.ProductStatusId : statusId;
                    }
                    else _raw.ProductStatusId = statusId;
                }
            }
        }

        [Display(Name = "Quantity")]
        public int Quantity
        {
            get { return _variant == null ? _raw.Quantity : _variant.Quantity; }
            set { if (_variant != null) _variant.Quantity = value; else _raw.Quantity = value; }
        }

        [Display(Name = "BackOrder qty")]
        public int BackOrderQty
        {
            get { return -1 * (_variant == null ? _raw.BackOrderQty : _variant.BackOrderQty); }
            set { if (_variant != null) _variant.BackOrderQty = value; else _raw.BackOrderQty = value; }
        }

        [Display(Name = "Barcode")]
        public string BarCode
        {
            get { return _variant == null ? _raw.BarCode : _variant.BarCode; }
            set { if (_variant != null) _variant.BarCode = value; else _raw.BarCode = value; }
        }

        public OFloat Weight
        {
            get { return _variant == null ? _raw.Weight : _variant.Weight; }
            set { if (_variant != null) _variant.Weight = value; else _raw.Weight = value; }
        }

        [Display(Name = "Weight unit")]
        public string WUnit
        {
            get { return _raw.WUnit == null ? null : Enum.GetName(typeof(WeightUnit), _raw.WUnit);  }
            set { _raw.WUnit = value.IsNullOrEmpty() ? (byte?)null : (byte)Enum.Parse(typeof(WeightUnit), value, true); }
        }

        public string Location
        {
            get { return _raw.Location; }
            set { _raw.Location = value; }
        }

        [Display(Name = "Visible on website: yes/no")]
        public bool VisibleWeb { get { return _raw.VisibleWeb; } set { if (CanSetProdMember) _raw.VisibleWeb = value; } }

        [Display(Name = "Visible on POS: yes/no")]
        public bool VisiblePos { get { return _raw.VisiblePos; } set { if (CanSetProdMember) _raw.VisiblePos = value; } }

        #region CategoryPath
        [Display(Name = "Category")]
        public string CategoryPath
        {
            get { return _categoryPath; }
            set
            {
                if (!CanSetProdMember) return;

                _categoryPath = value;
                if (_categories != null && !value.IsNullOrEmpty())
                {
                    Categories = new List<CategoryModel>();
                    var arr = value.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var arr2 = arr.Select((x, i) => new Tuple<string, string>(x, arr.GetRange(0, i + 1).ToString(">"))).ToList();

                    var lst = new List<CategoryModel>();

                    for (var i = 0; i < arr2.Count; i++)
                    {
                        var cate = _categories.FirstOrDefault(x => x.Languages.Any(w => w.Alias.EqualsIgCase(arr2[i].Item2)));
                        if (cate == null)
                            _categories.Add(cate = new CategoryModel()
                        {
                            Visibility = 0,
                            Languages = new List<CategoryLangModel>()
                            {
                                    new CategoryLangModel()
                                    {
                                        LangCode = LanguageMgr.FocusLangCode,
                                        Name = arr2[i].Item1,
                                        Alias = arr2[i].Item2
                                    }
                            },
                        });

                        lst.Add(cate);
                    }

                    Categories.AddRange(lst);
                }
            }
        }

        #endregion

        internal List<CategoryModel> Categories { get; set; }

        public List<PropertyModel> Properties { get; set; }

        #region Validate

        public static ValidationResult Validate(ProductExportModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (obj.Alias.IsNullOrEmpty())
            {
                var iseq = 1;
                obj.Alias = obj.Name.GenerateAlias();
                while (BaseMgr.Exists<Domain.ProductLang>(x => x.Id, obj.Id, null,
                    new KeyValuePair() {Key = "Alias", Value = obj.Alias}))
                {
                    obj.Alias = obj.Name.GenerateAlias() + '-' + (++iseq);
                }
                //if (BaseMgr.Exists<Domain.ProductLang>(x => x.Id, obj.Id, null, new KeyValuePair() { Key = "Alias", Value = obj.Alias }))
                //    obj.Alias = BaseMgr.Unique(new DefaultDb(), "Alias", new Domain.Product() { Id = obj.Id, Alias = obj.Alias });
            }

            return null;
        }

        #endregion
     
    }
}