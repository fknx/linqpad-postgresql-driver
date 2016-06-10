namespace DynamicLinqPadPostgreSqlDriver.Tests.DynamicAssemblyGeneration
{
   /// <summary>
   /// Test data for testing different signatures on pgsql functions
   /// </summary>
   public class InputParameterTestData
   {
      public string Name { get; }
      public string PgSqlType { get; }
      public object Default { get; }

      public InputParameterTestData(string name, string pgSqlType, object defaultValue = null)
      {
         Name = name;
         PgSqlType = pgSqlType;
         Default = defaultValue;
      }
   }
}
