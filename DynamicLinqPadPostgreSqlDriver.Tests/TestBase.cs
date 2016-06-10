using Dapper;
using Npgsql;
using System;
using System.Data;
using System.Linq;
using Xunit;

namespace DynamicLinqPadPostgreSqlDriver.Tests
{
   public class TestBase : IDisposable
   {
      private const string ConnectionString = "Server=localhost;Port=5432;Database=TestDb;User Id=postgres;Password=Password12!;";

      protected const string CreateTableStatement = "CREATE TABLE TestTable (TestColumn {0});";
      protected const string GetColumnsStatement = "SELECT column_name AS \"ColumnName\", is_nullable AS \"Nullable\", data_type AS \"DataType\", udt_name AS \"UdtName\" FROM information_schema.columns WHERE table_catalog = 'TestDb' AND table_name ilike 'TestTable' ORDER BY ordinal_position;";

      protected const string InsertStatement = "INSERT INTO TestTable VALUES (@Value);";
      protected const string SelectStatement = "SELECT TestColumn AS \"Value\" FROM TestTable;";

      protected IDbConnection DBConnection { get; }

      protected TestBase()
      {
         DBConnection = new NpgsqlConnection(ConnectionString);
         DBConnection.Open();
      }

      public void Dispose()
      {
         DBConnection.Execute("DROP TABLE TestTable;");
         DBConnection.Dispose();
      }

      protected Type GetColumnType()
      {
         var column = DBConnection.Query(GetColumnsStatement).Single();
         return SqlHelper.MapDbTypeToType(column.DataType, column.UdtName, "YES".Equals(column.Nullable, StringComparison.InvariantCultureIgnoreCase), true);
      }

      protected void TestNonNullableType<T>(string dataType, T testValue)
      {
         DBConnection.Execute(string.Format(CreateTableStatement, $"{dataType} NOT NULL"));

         var type = GetColumnType();
         Assert.Equal(typeof(T), type);

         DBConnection.Execute(InsertStatement, new { Value = testValue });

         var value = DBConnection.Query(SelectStatement).Single().Value;
         Assert.Equal(testValue, value);
      }

      protected void TestNullableType<T>(string dataType, T testValue)
      {
         DBConnection.Execute(string.Format(CreateTableStatement, $"{dataType} NULL"));

         var type = GetColumnType();
         Assert.Equal(typeof(T), type);

         DBConnection.Execute(InsertStatement, new { Value = testValue });

         var value = DBConnection.Query(SelectStatement).Single().Value;
         Assert.Equal(testValue, value);

         DBConnection.Execute(InsertStatement, new { Value = (object)null });

         value = DBConnection.Query(SelectStatement).Last().Value;
         Assert.Equal(null, value);
      }
   }
}
