using System;

namespace DynamicLinqPadPostgreSqlDriver
{
   internal class FunctionReturnTypeInfo
   {
      public bool ExistsAsTable { get; set; }
      public Type ElementType { get; set; }
      public Type CollectionType { get; set; }
   }
}