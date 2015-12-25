using System.Reflection.Emit;
using LINQPad.Extensibility.DataContext;

namespace DynamicLinqPadPostgreSqlDriver
{
   internal class TableData
   {
      public string Name { get; }

      public ExplorerItem ExplorerItem { get; }

      public TypeBuilder TypeBuilder { get; }

      public TableData(string name, ExplorerItem explorerItem, TypeBuilder typeBuilder)
      {
         Name = name;
         ExplorerItem = explorerItem;
         TypeBuilder = typeBuilder;
      }
   }
}
