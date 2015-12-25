using System.Windows;

namespace ExCastle.Wpf
{
   /// <summary>
   /// Extension to be able to bind to a <see cref="Window"/>'s DialogResult property.
   /// 
   /// http://blog.excastle.com/2010/07/25/mvvm-and-dialogresult-with-no-code-behind/
   /// </summary>
   public static class DialogCloser
   {
      public static readonly DependencyProperty DialogResultProperty = DependencyProperty.RegisterAttached("DialogResult", typeof(bool?),
               typeof(DialogCloser), new PropertyMetadata(DialogResultChanged));

      private static void DialogResultChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
      {
         var window = d as Window;
         if (window != null)
            window.DialogResult = e.NewValue as bool?;
      }
      public static void SetDialogResult(Window target, bool? value)
      {
         target.SetValue(DialogResultProperty, value);
      }
   }
}