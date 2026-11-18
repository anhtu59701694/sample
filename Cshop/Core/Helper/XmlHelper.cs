using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using YAXLib;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Helper
{
    public static class XmlHelper
    {
        /// <summary>Convert xml format string to object</summary>
        public static object ToObject<T>(this string xmlData)
        {
            return xmlData.ToObject(typeof(T));
        }

        public static object ToObject(this string xmlData, Type type)
        {
            if (xmlData.IsNullOrEmpty()) return null;

            Object obj = null;

            var serializer = new YAXSerializer(type, YAXLib.YAXExceptionHandlingPolicies.DoNotThrow, YAXLib.YAXExceptionTypes.Error, YAXLib.YAXSerializationOptions.SerializeNullObjects);
            obj = serializer.Deserialize(xmlData);
            serializer = null;

            return obj;
        }

        /// <summary>Convert an object to xml format</summary>
        public static string ToXml<T>(this T obj)
            where T : class
        {
            if (obj == null) return "";

            var serializer = new YAXSerializer(typeof (T), YAXExceptionHandlingPolicies.DoNotThrow,
                YAXExceptionTypes.Error, YAXSerializationOptions.DontSerializeNullObjects);
            var xmlData = serializer.Serialize(obj);

            return xmlData;
        }
    }
}