using System;

namespace DynamicLinqPadPostgreSqlDriver
{
   internal class FunctionArgumentInfo
   {
      public int Index { get; set; }
      public string Name { get; set; }
      public Type Type { get; set; }
   }
}