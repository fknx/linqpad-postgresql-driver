using LINQPad.Extensibility.DataContext;
using System;
using System.Text;

namespace DynamicLinqPadPostgreSqlDriver.Shared.Extensions
{
   public static class ConnectionInfoExtensions
   {
      public static string GetPostgreSqlConnectionString(this IConnectionInfo cxInfo)
      {
         if (cxInfo == null)
            throw new ArgumentNullException(nameof(cxInfo));

         if (!string.IsNullOrWhiteSpace(cxInfo.DatabaseInfo.CustomCxString))
            return cxInfo.DatabaseInfo.CustomCxString;

         return BuildConnectionString(cxInfo.DatabaseInfo.Server, cxInfo.DatabaseInfo.Database, cxInfo.DatabaseInfo.UserName, cxInfo.DatabaseInfo.Password);
      }

      internal static string BuildConnectionString(string serverWithPort, string database, string userName, string password)
      {
         if (string.IsNullOrWhiteSpace(serverWithPort))
            throw new ArgumentException("The argument may not be null or empty.", nameof(serverWithPort));

         if (string.IsNullOrWhiteSpace(database))
            throw new ArgumentException("The argument may not be null or empty.", nameof(database));

         var server = "";
         var port = "";

         if (serverWithPort.Contains(":"))
         {
            var parts = serverWithPort.Split(':');
            server = parts[0];
            port = parts[1];
         }
         else
         {
            server = serverWithPort;
         }

         var sb = new StringBuilder();

         sb.AppendFormat("Server={0};", server);

         if (!string.IsNullOrWhiteSpace(port))
         {
            sb.AppendFormat("Port={0};", port);
         }

         sb.AppendFormat("Database={0};", database);

         if (!string.IsNullOrWhiteSpace(userName))
         {
            sb.AppendFormat("User Id={0};", userName);

             if (!string.IsNullOrWhiteSpace(password))
             {
                 sb.AppendFormat("Password={0};", password);
             }
         }
         else
         {
            sb.Append("Integrated Security=true;");
         }

         return sb.ToString();
      }
   }
}
