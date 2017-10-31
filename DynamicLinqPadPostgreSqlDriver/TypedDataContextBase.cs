using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using LinqToDB.DataProvider;

namespace DynamicLinqPadPostgreSqlDriver
{
   public class TypedDataContextBase : DataConnection
   {
      private static Type _typedDataContextType;
      private static IDataProvider _dataProvider;
      private static string _connectionString;

      public TypedDataContextBase(IDataProvider dataProvider, string connectionString) : base(dataProvider, connectionString)
      {
         _typedDataContextType = GetType();
         _dataProvider = dataProvider;
         _connectionString = connectionString;
      }

      public static TypedDataContextBase CreateNewInstance()
      {
         if (_typedDataContextType == null)
            return null;

         return (TypedDataContextBase) Activator.CreateInstance(_typedDataContextType, _dataProvider, _connectionString);
      }

      protected Func<IDataReader, TTarget> GetMapper<TTarget>()
      {
         if (typeof(TTarget).IsPrimitive || typeof(TTarget) == typeof(string) || typeof(TTarget) == typeof(decimal))
         {
            return rdr => (TTarget)rdr.GetValue(0);
         }
         if (typeof(TTarget) == typeof(object)
             || typeof(TTarget) == typeof(ExpandoObject))
         {
            return rdr =>
            {
               var dict = (IDictionary<string, object>)new ExpandoObject();

               for (int i = 0; i < rdr.FieldCount; i++)
               {
                  var name = rdr.GetName(i);
                  var val = rdr.GetValue(i);
                  dict[name] = val;
               }

               return (TTarget)dict;
            };
         }

         throw new NotSupportedException($"The type {typeof(TTarget).Name} is not supported.");
      }
   }
}
