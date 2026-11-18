using Claw.Modules.Eshop.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(PermissionModel), "Validate")]
    public class PermissionModel 
    {
        public int Id { get; set; }

        public bool InlineEdit { get; set; }

        public bool QuickAdd { get; set; }

        #region Raw
        protected Permission _raw;

        public Permission Raw { get { return null; } set { _raw = value; } }

        public Permission GetRaw() { return _raw; }
        #endregion

        public int RoleId { get; set; }

        [Display(Name = "Role name")]
        public string RoleName { get; set; }

        [Display(Name = "Role description")]
        public string RoleDescription { get; set; }

        public long Bitmask { get; set; }

        #region ValueText
        [Display(Name = "Value")]
        public string ValueText
        {
            get
            {
                return new Dictionary<string, byte>()
                {
                    {this.GetDisplayName(m => m.CatalogBits), CatalogBits},
                    {this.GetDisplayName(m => m.PromotionBits), PromotionBits},
                    {this.GetDisplayName(m => m.OrderBits), OrderBits},
                }.Where(x => x.Value != 0).Select(x => string.Format("{0} [{1}]", x.Key,
                    typeof(PermissionMask).LocalizeEnumValues<byte>(w => x.Value.BitmaskCmp(w))
                        .Select(w => w.Text).ToString(" | "))).ToString(", ");
            }
        } 
        #endregion

        #region CatalogBits
        /// <summary>bit 0-3</summary>
        [Display(Name = "Catalog functions")]
        public byte CatalogBits
        {
            get { return (byte)(Bitmask & 0xF); }
            set { Bitmask = (Bitmask & ~0xF) | value; }
        } 
        #endregion

        #region CatalogView
        public bool CatalogView
        {
            get { return CatalogBits.BitmaskCmp((byte)PermissionMask.View); }
            set
            {
                const byte mask = (byte)PermissionMask.View;
                CatalogBits = value ? (byte)(CatalogBits | mask) : (byte)(CatalogBits & ~mask);
            }
        } 
        #endregion

        #region CatalogEdit
        public bool CatalogEdit
        {
            get { return CatalogBits.BitmaskCmp((byte)PermissionMask.Edit); }
            set
            {
                const byte mask = (byte)PermissionMask.Edit;
                CatalogBits = value ? (byte)(CatalogBits | mask | (byte)PermissionMask.View) : (byte)(CatalogBits & ~mask);
            }
        } 
        #endregion

        #region PromotionBits
        /// <summary>bit 4-7, coupon/discount</summary>
        [Display(Name = "Promotion functions")]
        public byte PromotionBits
        {
            get { return (byte)((Bitmask & 0xF0) >> 4); }
            set { Bitmask = (Bitmask & ~0xF0) | ((long)value << 4); }
        } 
        #endregion

        #region PromotionView
        public bool PromotionView
        {
            get { return PromotionBits.BitmaskCmp((byte)PermissionMask.View); }
            set
            {
                const byte mask = (byte)PermissionMask.View;
                PromotionBits = value ? (byte)(PromotionBits | mask) : (byte)(PromotionBits & ~mask);
            }
        } 
        #endregion

        #region PromotionEdit
        public bool PromotionEdit
        {
            get { return PromotionBits.BitmaskCmp((byte)PermissionMask.Edit); }
            set
            {
                const byte mask = (byte)PermissionMask.Edit;
                PromotionBits = value ? (byte)(PromotionBits | mask | (byte)PermissionMask.View) : (byte)(PromotionBits & ~mask);
            }
        } 
        #endregion

        #region OrderBits
        /// <summary>bit 8-11</summary>
        [Display(Name = "Order functions")]
        public byte OrderBits
        {
            get { return (byte)((Bitmask & 0xF00) >> 8); }
            set { Bitmask = (Bitmask & ~0xF00) | ((long)value << 8); }
        } 
        #endregion

        #region OrderView
        public bool OrderView
        {
            get { return OrderBits.BitmaskCmp((byte)PermissionMask.View); }
            set
            {
                const byte mask = (byte)PermissionMask.View;
                OrderBits = value ? (byte)(OrderBits | mask) : (byte)(OrderBits & ~mask);
            }
        } 
        #endregion

        #region OrderEdit
        public bool OrderEdit
        {
            get { return OrderBits.BitmaskCmp((byte)PermissionMask.Edit); }
            set
            {
                const byte mask = (byte)PermissionMask.Edit;
                OrderBits = value ? (byte)(OrderBits | mask | (byte)PermissionMask.View) : (byte)(OrderBits & ~mask);
            }
        } 
        #endregion

        public int? Users { get; set; }

        [Display(Name = "Modified date")]
        public ODateTime ModifiedDate { get; set; }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }

        #region Validate
        public static ValidationResult Validate(PermissionModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            return null;
        }
        #endregion
    }
}