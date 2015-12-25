using System;

namespace DynamicLinqPadPostgreSqlDriver.Extensions
{
   internal static class TypeExtensions
   {
      public static string GetTypeName(this Type type)
      {
         if (type == null)
            return "unknown";

         var nullableType = Nullable.GetUnderlyingType(type);
         if (nullableType != null)
            return $"{nullableType.Name}?";

         return type.Name;
      }
   }
}
