using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace Claw.Modules.Eshop.Helper
{
    public static class ClawEnumHelper
    {
        public static T ConvertValueToEnum<T>(this string value) where T : struct
        {
            T t;
            var isResult = Enum.TryParse(value, true, out t);
            return isResult ? t : default(T);
        }

        public static List<SelectListItem> ConvertEnumToDropdownList<T>() where T : struct
        {
            var listDropdownList = new List<SelectListItem>();
            var enumValues = Enum.GetValues(typeof(T));
            foreach (var item in enumValues)
            {
                var enumItem = (T)item as Enum;
                if (enumItem == null) continue;
                var description = GetAttributeValue<DescriptionAttribute, string>(enumItem, x => x.Description);
                var text = !string.IsNullOrEmpty(description) ? description : enumItem.ToString();

                var test = Enum.Parse(typeof(T), enumItem.ToString());
                var value = (int)test;
                var dropdownlist = new SelectListItem
                {
                    Text = text,
                    Value = value.ToString(CultureInfo.InvariantCulture)
                };
                listDropdownList.Add(dropdownlist);
            }
            return listDropdownList;
        }

        public static List<SelectListItem> ConvertEnumToDropdownListValString<T>() where T : struct
        {
            var listDropdownList = new List<SelectListItem>();
            var enumValues = Enum.GetValues(typeof(T));
            foreach (var item in enumValues)
            {
                var enumItem = (T)item as Enum;
                if (enumItem == null) continue;
                var description = GetAttributeValue<DescriptionAttribute, string>(enumItem, x => x.Description);
                var text = !string.IsNullOrEmpty(description) ? description : enumItem.ToString();

                var test = Enum.Parse(typeof(T), enumItem.ToString());
                var value = test.ToString();
                var dropdownlist = new SelectListItem
                {
                    Text = text,
                    Value = value
                };
                listDropdownList.Add(dropdownlist);
            }
            return listDropdownList;
        }

        public static TV GetAttributeValue<T, TV>(this Enum enumeration, Func<T, TV> expression) where T : Attribute
        {
            var firstOrDefault = enumeration
                .GetType()
                .GetMember(enumeration.ToString()).FirstOrDefault(member => member.MemberType == MemberTypes.Field);
            if (firstOrDefault == null)
                return default(TV);
            T attribute = firstOrDefault
                    .GetCustomAttributes(typeof(T), false)
                    .Cast<T>()
                    .SingleOrDefault();

            if (attribute == null)
                return default(TV);

            return expression(attribute);
        }
    }
}