using System.ComponentModel.DataAnnotations;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(LocationModel), "Validate")]
    public class LocationModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        #region Validate

        public static ValidationResult Validate(LocationModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.Location>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            return null;
        }

        #endregion
    }
}