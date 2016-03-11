using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DynamicLinqPadPostgreSqlDriver.DDL
{
   /// <summary>
   /// DDL statements in LINQPad are baked in unfortunately. With the help
   /// of this interceptor we can work around this limitation by rewriting the 
   /// statements to be compatible with PostgreSql
   /// </summary>
   public class CommandTextInterceptor
   {
      public string GetCommandText(string commandText)
      {
         if (commandText.StartsWith("DROP FUNCTION"))
         {
            // DROP FUNCTION [dbo].[xyz] => DROP FUNCTION public.xyz;
            commandText = commandText.Replace("[dbo]", "public").Replace("[", "").Replace("]", "") + ";";
         }
         // sp_helptext is SqlServer specific and needs to be replaced by its PostgreSql equivalent
         if (commandText.StartsWith("sp_helptext"))
         {
            var funcName = Regex.Match(commandText, "(?<=(dbo\\.)).*(?=\\()");
            commandText = $"select pg_get_functiondef(oid) from pg_proc where proname = '{funcName}';";
         }
         return commandText;
      }
   }
}