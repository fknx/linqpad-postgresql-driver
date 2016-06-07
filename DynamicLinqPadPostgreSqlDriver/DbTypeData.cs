using System;

namespace DynamicLinqPadPostgreSqlDriver
{
   internal class DbTypeData
   {
      public string DbType { get; private set; }
      public string UdtName { get; private set; }

      /// <summary>
      /// Convert the <see cref="typeName"/> to <see cref="DbTypeData"/> that is in the
      /// correct form to be understood by <see cref="SqlHelper.MapDbTypeToType"/>
      /// (has to be done for arrays in user defined types)
      /// </summary>
      public static DbTypeData FromString(string typeName)
      {
         var arrayIdentifierPos = typeName?.IndexOf("[]", StringComparison.Ordinal)??-1;
         if (arrayIdentifierPos >= 0)
         {
            return new DbTypeData
            {
               DbType = "array",
               UdtName = typeName?.Remove(arrayIdentifierPos)
            };
         }

         return new DbTypeData { DbType = typeName };
      }
   }
}