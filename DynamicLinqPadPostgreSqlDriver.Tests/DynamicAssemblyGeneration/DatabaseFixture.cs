using System;
using System.Data;
using Dapper;
using Npgsql;

namespace DynamicLinqPadPostgreSqlDriver.Tests.DynamicAssemblyGeneration
{
   public class DatabaseFixture : IDisposable
   {
      public const string ProviderName = "PostgreSql";
      public const string ConnectionString = "Server=localhost;Port=5433;Database=TestDb_DynamicAssemblyGeneration;User Id=postgres;Password=postgres;";

      public IDbConnection DBConnection { get; }

      public DatabaseFixture()
      {
         CreateDatabaseIfNotExists();

         DBConnection = new NpgsqlConnection(ConnectionString);
         DBConnection.Open();
      }

      private static void CreateDatabaseIfNotExists()
      {
         var cxBuilder = new NpgsqlConnectionStringBuilder(ConnectionString);
         var database = cxBuilder.Database;
         cxBuilder.Database = null;

         var db = new NpgsqlConnection(cxBuilder.ToString());
         db.Execute($"CREATE DATABASE '{database}' IF NOT EXISTS");
      }

      private static void DropDatabase()
      {
         var cxBuilder = new NpgsqlConnectionStringBuilder(ConnectionString);
         var database = cxBuilder.Database;
         cxBuilder.Database = null;

         var db = new NpgsqlConnection(cxBuilder.ToString());
         db.Execute($"DROP DATABASE '{database}'");
      }

      public void Dispose()
      {
         DBConnection.Close();
         DropDatabase();
      }
   }
}
