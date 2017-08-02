using LINQPad.Extensibility.DataContext;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicLinqPadPostgreSqlDriver.Extensions
{
   internal static class TableDataExtensions
   {
      public static string CreateOneToManyAssociation(this TableData table, TableData foreignTable, string columnNames, string foreignColumnNames, string constraintName)
      {
         // create an IEnumerable<> with the type of the (main) table's type builder
         var typedEnumerableType = typeof(IEnumerable<>).MakeGenericType(table.TypeBuilder);

         // use the table's name as property name (plus constraint name)
         var propertyName = $"{table.Name} ({constraintName})";
         foreignTable.PropertyAndFieldNames.Add(propertyName);

         // create a property in the foreign key's target table
         var property = foreignTable.TypeBuilder.DefineProperty(propertyName, typedEnumerableType);

         // create a getter for the property
         var propertyGetter = foreignTable.TypeBuilder.DefineGetter(property);

         // obtain ResolveOneToMany method
         var resolveOneToManyMethod = typeof(Entity).GetMethod("ResolveOneToMany", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(table.TypeBuilder);

         // "implement" the method to obtain and return the value by calling 'ResolveOneToMany'
         var ilGenerator = propertyGetter.GetILGenerator();

         // call the method and return the value
         ilGenerator.Emit(OpCodes.Ldarg_0);
         ilGenerator.Emit(OpCodes.Ldstr, property.Name);
         ilGenerator.Emit(OpCodes.Call, resolveOneToManyMethod);
         ilGenerator.Emit(OpCodes.Ret);

         property.SetGetMethod(propertyGetter);

         // add the 'AssociationAttribute' to the property
         property.AddAssociationAttribute(foreignColumnNames, columnNames, table.Name);

         // create the explorer item
         var explorerItem = new ExplorerItem(propertyName, ExplorerItemKind.CollectionLink, ExplorerIcon.OneToMany) { HyperlinkTarget = table.ExplorerItem };
         foreignTable.ExplorerItem.Children.Add(explorerItem);

         // create 'backward' association
         table.CreateManyToOneAssociation(foreignTable, foreignColumnNames, columnNames, constraintName, true);

         return propertyName;
      }

      public static string CreateManyToOneAssociation(this TableData table, TableData foreignTable, string columnNames, string foreignColumnNames, string constraintName, bool backwardReference = false)
      {
         // use the foreign table's name as property name (plus constraint name)
         var propertyName = $"{foreignTable.Name} ({constraintName})";
         table.PropertyAndFieldNames.Add(propertyName);

         // create a property of the foreign table's type in the table entity
         var property = table.TypeBuilder.DefineProperty(propertyName, foreignTable.TypeBuilder);

         // create a getter for the property
         var propertyGetter = table.TypeBuilder.DefineGetter(property);

         // obtain ResolveManyToOne method
         var resolveManyToOneMethod = typeof(Entity).GetMethod("ResolveManyToOne", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(property.PropertyType);

         // "implement" the method to obtain and return the value by calling 'ResolveManyToOne'
         var ilGenerator = propertyGetter.GetILGenerator();

         // call the method and return the value
         ilGenerator.Emit(OpCodes.Ldarg_0);
         ilGenerator.Emit(OpCodes.Ldstr, property.Name);
         ilGenerator.Emit(OpCodes.Call, resolveManyToOneMethod);
         ilGenerator.Emit(OpCodes.Ret);

         property.SetGetMethod(propertyGetter);

         // add the 'AssociationAttribute' to the property
         property.AddAssociationAttribute(foreignColumnNames, columnNames, foreignTable.Name, true);

         // create the explorer item
         var explorerItem = new ExplorerItem(propertyName, ExplorerItemKind.ReferenceLink, ExplorerIcon.ManyToOne) { HyperlinkTarget = foreignTable.ExplorerItem };
         table.ExplorerItem.Children.Add(explorerItem);

         return property.Name;
      }

      /*
      public static string FindFreePropertyName(this TableData tableData, string propertyName)
      {
         var originalPropertyName = propertyName;

         // check for a naming collision
         var i = 1;
         while (tableData.PropertyAndFieldNames.Contains(propertyName))
         {
            propertyName = $"{originalPropertyName}{i++}";
         }

         return propertyName;
      }
      */
   }
}
