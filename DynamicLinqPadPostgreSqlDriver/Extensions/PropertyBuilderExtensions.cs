using LinqToDB.Mapping;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicLinqPadPostgreSqlDriver.Extensions
{
   internal static class PropertyBuilderExtensions
   {
      private static readonly ConstructorInfo AssociationAttributeConstructor = typeof(AssociationAttribute).GetConstructor(Type.EmptyTypes);

      public static void AddAssociationAttribute(this PropertyBuilder propertyBuilder, string primaryKey, string foreignKey, string foreignTable, bool backReference = false)
      {
         var type = typeof(AssociationAttribute);

         var thisKeyProperty = type.GetProperty("ThisKey");
         var otherKeyProperty = type.GetProperty("OtherKey");
         var storageProperty = type.GetProperty("Storage");
         var isBackReferenceProperty = type.GetProperty("IsBackReference");

         var associationAttributeBuilder = new CustomAttributeBuilder(AssociationAttributeConstructor, new object[0],
            new[] { thisKeyProperty, otherKeyProperty, storageProperty, isBackReferenceProperty },
            new object[] { primaryKey, foreignKey, foreignTable, backReference });

         propertyBuilder.SetCustomAttribute(associationAttributeBuilder);
      }

   }
}
