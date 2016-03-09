using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;

namespace DynamicLinqPadPostgreSqlDriver
{
   public class TypedDataContextBase : DataConnection
   {
      private static Type _typedDataContextType;
      private static string _providerName;
      private static string _connectionString;

      public TypedDataContextBase(string providerName, string connectionString) : base(providerName, connectionString)
      {
         _typedDataContextType = GetType();
         _providerName = providerName;
         _connectionString = connectionString;
      }

      public static TypedDataContextBase CreateNewInstance()
      {
         if (_typedDataContextType == null)
            return null;

         return (TypedDataContextBase) Activator.CreateInstance(_typedDataContextType, _providerName, _connectionString);
      }

      protected Func<IDataReader, TTarget> GetMapper<TTarget>()
      {
         if (typeof(TTarget).IsPrimitive)
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
