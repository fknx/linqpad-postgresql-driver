using LinqToDB.Mapping;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicLinqPadPostgreSqlDriver.Extensions
{
   internal static class FieldBuilderExtensions
   {
      private static readonly ConstructorInfo ColumnAttributeConstructor = typeof(ColumnAttribute).GetConstructor(new[] { typeof(string) });

      public static void AddColumnAttribute(this FieldBuilder fieldBuilder, string columnName)
      {
         var attributeBuilder = new CustomAttributeBuilder(ColumnAttributeConstructor, new object[] { columnName });
         fieldBuilder.SetCustomAttribute(attributeBuilder);
      }

      private static readonly ConstructorInfo PrimaryKeyAttributeConstructor = typeof(PrimaryKeyAttribute).GetConstructor(Type.EmptyTypes);

      public static void AddPrimaryKeyAttribute(this FieldBuilder fieldBuilder)
      {
         var attributeBuilder = new CustomAttributeBuilder(PrimaryKeyAttributeConstructor, new object[0]);
         fieldBuilder.SetCustomAttribute(attributeBuilder);
      }

      private static readonly ConstructorInfo IdentityAttributeConstructor = typeof(IdentityAttribute).GetConstructor(Type.EmptyTypes);

      public static void AddIdentityAttribute(this FieldBuilder fieldBuilder)
      {
         var attributeBuilder = new CustomAttributeBuilder(IdentityAttributeConstructor, new object[0]);
         fieldBuilder.SetCustomAttribute(attributeBuilder);
      }
   }
}
