using System.Reflection;
using LINQPad.Extensibility.DataContext;

namespace DynamicLinqPadPostgreSqlDriver.Extensions
{
   public static class ExplorerItemExtensions
   {
      public static void SupportsDDLEditing(this ExplorerItem explorerItem, bool value)
      {
         var ddlEditingField = typeof(ExplorerItem).GetField("SupportsDDLEditing", BindingFlags.NonPublic | BindingFlags.Instance);
         ddlEditingField?.SetValue(explorerItem, value);
      }
   }
}
