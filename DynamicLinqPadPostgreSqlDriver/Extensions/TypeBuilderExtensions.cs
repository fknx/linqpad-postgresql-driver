using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicLinqPadPostgreSqlDriver.Extensions
{
   internal static class TypeBuilderExtensions
   {
      private static readonly ConstructorInfo TableAttributeConstructor = typeof(TableAttribute).GetConstructor(new[] { typeof(string) });

      public static void AddTableAttribute(this TypeBuilder typeBuilder, string tableName)
      {
         var tableAttributeBuilder = new CustomAttributeBuilder(TableAttributeConstructor, new object[] { tableName });
         typeBuilder.SetCustomAttribute(tableAttributeBuilder);
      }

      private const MethodAttributes GetMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

      public static MethodBuilder DefineGetter(this TypeBuilder typeBuilder, PropertyBuilder property)
      {
         return typeBuilder.DefineMethod($"get_{property.Name}", GetMethodAttributes, property.PropertyType, Type.EmptyTypes);
      }

      public static PropertyBuilder DefineProperty(this TypeBuilder typeBuilder, string name, Type type)
      {
         return typeBuilder.DefineProperty(name, PropertyAttributes.None, type, Type.EmptyTypes);
      }

      public static void CreateAndAddType(this TypeBuilder dataContextTypeBuilder, TableData tableData)
      {
         var typeBuilder = tableData.TypeBuilder;
         var explorerItem = tableData.ExplorerItem;

         var type = typeBuilder.CreateType();

         // create a Table<> field in the data context type
         var genericTableType = typeof(ITable<>).MakeGenericType(type);

         // create the property itself
         var property = dataContextTypeBuilder.DefineProperty(explorerItem.Text, PropertyAttributes.None, genericTableType, Type.EmptyTypes);

         // create a getter for the property
         var propertyGetter = dataContextTypeBuilder.DefineMethod($"get_{property.Name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, genericTableType, Type.EmptyTypes);

         // obtain GetTable method
         var getTableMethod = typeof(DataConnection).GetMethod("GetTable", Type.EmptyTypes).MakeGenericMethod(type);

         // "implement" the method to obtain and return the value by calling GetTable<T>()
         var ilGenerator = propertyGetter.GetILGenerator();

         // call the method and return the value
         ilGenerator.Emit(OpCodes.Ldarg_0);
         ilGenerator.Emit(OpCodes.Call, getTableMethod);
         ilGenerator.Emit(OpCodes.Ret);

         property.SetGetMethod(propertyGetter);
      }
   }
}
