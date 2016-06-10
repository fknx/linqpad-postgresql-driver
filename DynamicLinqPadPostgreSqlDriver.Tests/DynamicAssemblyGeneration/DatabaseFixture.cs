using System;
using System.Data;
using Dapper;
using Npgsql;

namespace DynamicLinqPadPostgreSqlDriver.Tests.DynamicAssemblyGeneration
{
   public class DatabaseFixture : IDisposable
   {
      public const string ProviderName = "PostgreSQL";
      public const string ConnectionString = "Server=localhost;Port=5432;Database=TestDb_DynamicAssemblyGeneration;User Id=postgres;Password=Password12!;";

      public IDbConnection DBConnection { get; }

      public DatabaseFixture()
      {
         CreateDatabase();

         DBConnection = new NpgsqlConnection(ConnectionString);
         DBConnection.Open();
      }

      private static void CreateDatabase()
      {
         var cxBuilder = new NpgsqlConnectionStringBuilder(ConnectionString);
         var database = cxBuilder.Database;
         cxBuilder.Database = null;

         var db = new NpgsqlConnection(cxBuilder.ToString());
         db.Execute($"DROP DATABASE IF EXISTS \"{database}\"");
         db.Execute($"CREATE DATABASE \"{database}\"");
      }

      public void Dispose()
      {
         DBConnection.Close();
      }
   }
}
