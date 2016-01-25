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

      /// <summary>
      /// Checks whether the given <see cref="Type"/> is nullable.
      /// 
      /// Adopted from http://stackoverflow.com/a/374663
      /// </summary>
      /// <param name="type">The type which shall be checked.</param>
      /// <returns><c>true</c> if the type is nullable, otherwise <c>false</c></returns>
      public static bool IsNullable(this Type type)
      {
         if (!type.IsValueType)
            return true; // ref-type

         if (Nullable.GetUnderlyingType(type) != null)
            return true; // Nullable<T>

         return false; // value-type
      }
   }
}
