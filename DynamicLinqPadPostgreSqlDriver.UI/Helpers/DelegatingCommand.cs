using System;
using System.Windows.Input;

namespace DynamicLinqPadPostgreSqlDriver.UI.Helpers
{
   class DelegatingCommand : ICommand
   {
      private readonly Action _action;

      public DelegatingCommand(Action action)
      {
         if (action == null)
            throw new ArgumentNullException(nameof(action));

         _action = action;
      }

#pragma warning disable CS0067
      public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

      public bool CanExecute(object parameter)
      {
         return true;
      }

      public void Execute(object parameter)
      {
         _action();
      }
   }
}
