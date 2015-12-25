using DynamicLinqPadPostgreSqlDriver.UI.ViewModels;
using LINQPad.Extensibility.DataContext;
using System;
using System.Windows;

namespace DynamicLinqPadPostgreSqlDriver.UI
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   internal partial class ConfigurationWindow : Window
   {
      public ConfigurationWindow()
      {
         // empty constructor for designer support
      }

      public ConfigurationWindow(IConnectionInfo cxInfo)
      {
         if (cxInfo == null)
            throw new ArgumentNullException(nameof(cxInfo));

         InitializeComponent();

         var viewModel = new ConnectionInfoViewModel(cxInfo, () => passwordBox.Password, s => passwordBox.Password = s);
         DataContext = viewModel;
      }
   }
}
