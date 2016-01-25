using System.Reflection.Emit;
using LINQPad.Extensibility.DataContext;
using System.Collections.Generic;

namespace DynamicLinqPadPostgreSqlDriver
{
   internal class TableData
   {
      public string Name { get; }

      public ExplorerItem ExplorerItem { get; }

      public TypeBuilder TypeBuilder { get; }

      public ISet<string> PropertyAndFieldNames { get; }

      public TableData(string name, ExplorerItem explorerItem, TypeBuilder typeBuilder, ISet<string> propertyAndFieldNames)
      {
         Name = name;
         ExplorerItem = explorerItem;
         TypeBuilder = typeBuilder;

         PropertyAndFieldNames = propertyAndFieldNames;
      }
   }
}
