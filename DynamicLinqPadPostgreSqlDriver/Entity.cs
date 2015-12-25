using LinqToDB;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DynamicLinqPadPostgreSqlDriver
{
   public class Entity
   {
      protected IEnumerable<T> ResolveOneToMany<T>(string propertyName) where T : class
      {
         var queryData = PrepareQuery<T>(propertyName);
         return queryData != null ? queryData.Table.Where(queryData.Query) : Enumerable.Empty<T>();
      }

      protected T ResolveManyToOne<T>(string propertyName) where T : class
      {
         var queryData = PrepareQuery<T>(propertyName);
         return queryData != null ? queryData.Table.SingleOrDefault(queryData.Query) : default(T);
      }

      private QueryData<T> PrepareQuery<T>(string propertyName) where T : class
      {
         if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("The argument may not be null or empty.", nameof(propertyName));

         var property = GetType().GetProperty(propertyName);
         if (property == null)
            throw new Exception($"The type '{GetType().Name}' does not have a property named '{propertyName}'.");

         var association = property.GetCustomAttribute<AssociationAttribute>();
         if (association == null)
            throw new Exception($"The property '{propertyName}' does not represent an association.");

         var dataContext = TypedDataContextBase.Instance;
         if (dataContext == null)
            throw new Exception("No data context available.");

         var typedTableType = typeof(ITable<>).MakeGenericType(typeof(T));
         var tableProperty = dataContext.GetType().GetProperties().FirstOrDefault(p => p.PropertyType == typedTableType);
         if (tableProperty == null)
            throw new Exception($"The data context does not contain a property of type '{typedTableType.Name}'.");

         var table = (ITable<T>)tableProperty.GetValue(dataContext);
         if (table == null)
            throw new NullReferenceException($"The property '{tableProperty.Name}' is null.");

         var thisKeyField = GetFieldByColumnName(GetType(), association.ThisKey);
         if (thisKeyField == null)
            throw new Exception($"The key field (this) with column name '{association.ThisKey}' does not exist.");

         var otherKeyField = GetFieldByColumnName(typeof(T), association.OtherKey);
         if (otherKeyField == null)
            throw new Exception($"The key field (other) with column name '{association.OtherKey}' does not exist.");

         // build query expression
         var parameter = Expression.Parameter(typeof(T));
         var fieldExpression = (Expression) Expression.Field(parameter, otherKeyField);

         if (thisKeyField.FieldType != otherKeyField.FieldType)
         {
            // the types are not matching, so try to convert the expression
            fieldExpression = Expression.Convert(fieldExpression, thisKeyField.FieldType);
         }

         var key = thisKeyField.GetValue(this);
         if (key == null)
            return null;

         var keyExpression = Expression.Constant(key);
         var equalsExpression = Expression.Equal(fieldExpression, keyExpression);

         // create & compile query
         return new QueryData<T>(table, Expression.Lambda<Func<T, bool>>(equalsExpression, parameter).Compile());
      }

      private FieldInfo GetFieldByColumnName(Type type, string columnName)
      {
         return type.GetFields().FirstOrDefault(f =>
         {
            var attribute = f.GetCustomAttribute<ColumnAttribute>();
            return attribute != null && attribute.Name == columnName;
         });
      }

      private class QueryData<T>
      {
         public ITable<T> Table { get; }

         public Func<T, bool> Query { get; }

         public QueryData(ITable<T> table, Func<T, bool> query)
         {
            Table = table;
            Query = query;
         }
      }
   }
}
