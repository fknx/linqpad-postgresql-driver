using DynamicLinqPadPostgreSqlDriver.Extensions;
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
         if (queryData == null)
            return Enumerable.Empty<T>();

         using (queryData.DataContext)
         {
            return queryData.Table.Where(queryData.Query).ToArray();
         }
      }

      protected T ResolveManyToOne<T>(string propertyName) where T : class
      {
         var queryData = PrepareQuery<T>(propertyName);
         if (queryData == null)
            return default(T);

         using (queryData.DataContext)
         {
            return queryData.Table.SingleOrDefault(queryData.Query);
         }
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

         var dataContext = TypedDataContextBase.CreateNewInstance();
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

         var key = thisKeyField.GetValue(this);
         if (key == null)
            return null;

         // build query expression
         var parameter = Expression.Parameter(typeof(T));
         var fieldExpression = (Expression) Expression.Field(parameter, otherKeyField);

         Expression fieldNotNullExpression = null;
         if (otherKeyField.FieldType.IsNullable())
         {
            // check whether the field is null before a cast or comparioson
            fieldNotNullExpression = Expression.NotEqual(fieldExpression, Expression.Constant(null, otherKeyField.FieldType));
         }

         if (thisKeyField.FieldType != otherKeyField.FieldType)
         {
            // the types are not matching, so try to convert the expression
            fieldExpression = Expression.Convert(fieldExpression, thisKeyField.FieldType);
         }

         var keyExpression = Expression.Constant(key, thisKeyField.FieldType);
         var equalExpression = Expression.Equal(fieldExpression, keyExpression);

         var notNullAndEqualExpression = fieldNotNullExpression != null
            ? Expression.AndAlso(fieldNotNullExpression, equalExpression)
            : equalExpression;

         // create & compile query
         return new QueryData<T>(dataContext, table, Expression.Lambda<Func<T, bool>>(notNullAndEqualExpression, parameter).Compile());
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
         public IDataContext DataContext { get; }

         public ITable<T> Table { get; }

         public Func<T, bool> Query { get; }

         public QueryData(IDataContext dataContext, ITable<T> table, Func<T, bool> query)
         {
            DataContext = dataContext;
            Table = table;
            Query = query;
         }
      }
   }
}
