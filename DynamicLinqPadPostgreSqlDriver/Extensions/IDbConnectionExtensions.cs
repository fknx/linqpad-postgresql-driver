using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DynamicLinqPadPostgreSqlDriver.Extensions
{
   internal static class IDbConnectionExtensions
   {
      public static int GetOid(this IDbConnection dbConnection, string tableName)
      {
         var query = SqlHelper.LoadSql("QueryOid.sql");
         var oid = dbConnection.Query(query, new { TableName = $"\"{tableName}\"" }).First().Oid;

         return Convert.ToInt32(oid);
      }

      public static ISet<string> GetPrimaryKeyColumns(this IDbConnection dbConnection, string tableName)
      {
         var oid = dbConnection.GetOid(tableName);
         var query = SqlHelper.LoadSql("QueryPrimaryKeyColumns.sql");

         var columns = new HashSet<string>(dbConnection.Query(query, new { Oid = oid }).Select(r => r.Name).Cast<string>());
         return columns;
      }
   }
}
