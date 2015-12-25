using DynamicLinqPadPostgreSqlDriver.Shared.Extensions;
using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DynamicLinqPadPostgreSqlDriver.Shared.Helpers
{
   public class ConnectionHelper
   {
      public static async Task<bool> CheckConnection(string connectionString)
      {
         if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("The argument may not be null or empty", nameof(connectionString));

         using (var connection = new NpgsqlConnection(connectionString))
         {
            await connection.OpenAsync();
            return connection.State == ConnectionState.Open;
         }
      }

      public static async Task<bool> CheckConnection(string server, string database, string userName, string password)
      {
         return await CheckConnection(ConnectionInfoExtensions.BuildConnectionString(server, database, userName, password));
      }
   }
}
