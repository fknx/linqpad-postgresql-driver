using LinqToDB.Data;

namespace DynamicLinqPadPostgreSqlDriver
{
   public class TypedDataContextBase : DataConnection
   {
      public static TypedDataContextBase Instance { get; private set; }

      public TypedDataContextBase(string providerName, string connectionString) : base(providerName, connectionString)
      {
         Instance = this;
      }
   }
}
