namespace DynamicLinqPadPostgreSqlDriver
{
   internal class FunctionData
   {
      public string Name { get; set; }
      public string ReturnType { get; set; }
      public int ArgumentCount { get; set; }
      public string[] ArgumentNames { get; set; }
      public int[] ArgumentTypeOids { get; set; }
      public string ArgumentDefaults { get; set; }
      public bool IsMultiValueReturn { get; set; }
   }
}