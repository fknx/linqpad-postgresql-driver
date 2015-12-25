using System;
using System.Linq;
using System.Xml.Linq;

namespace DynamicLinqPadPostgreSqlDriver.Shared.Extensions
{
   public static class XElementExtensions
   {
      public static T GetDescendantValue<T>(this XElement parent, DriverOption driverOption, Func<string, T> converter)
      {
         return parent.GetDescendantValue(driverOption.ToString(), converter, default(T));
      }

      public static T GetDescendantValue<T>(this XElement parent, string name, Func<string, T> converter)
      {
         return parent.GetDescendantValue(name, converter, default(T));
      }

      public static T GetDescendantValue<T>(this XElement parent, DriverOption driverOption, Func<string, T> converter, T defaultValue)
      {
         return parent.GetDescendantValue(driverOption.ToString(), converter, defaultValue);
      }

      public static T GetDescendantValue<T>(this XElement parent, string name, Func<string, T> converter, T defaultValue)
      {
         if (name == null)
            throw new ArgumentNullException(nameof(name));

         if (converter == null)
            throw new ArgumentNullException(nameof(converter));

         if (parent == null)
            return defaultValue;

         var element = parent.Descendants(name).FirstOrDefault();
         if (element == null)
            return defaultValue;

         try
         {
            return converter(element.Value);
         }
         catch { }

         return defaultValue;
      }
   }
}
