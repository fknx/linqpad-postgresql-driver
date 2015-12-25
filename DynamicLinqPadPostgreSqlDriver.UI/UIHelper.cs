using LINQPad.Extensibility.DataContext;

namespace DynamicLinqPadPostgreSqlDriver.UI
{
   public static class UIHelper
   {
      public static bool ShowConfigurationWindow(IConnectionInfo cxInfo)
      {
         var mainWindow = new ConfigurationWindow(cxInfo);
         return mainWindow.ShowDialog() ?? false;
      }
   }
}
