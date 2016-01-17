using LinqToDB.Data;
using System;
using System.Reflection;

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
   }
}
