using System;
using System.Data.Common;

namespace DynamicLinqPadPostgreSqlDriver.DDL
{
   /// <summary>
   /// The ProviderFactory is retrieved by LINQPad via reflection, so it needs to
   /// be replaced to return a <see cref="DbCommandProxy"/> instead of the underlying command.
   /// </summary>
   public class DbProviderFactoryProxy : DbProviderFactory
   {
      private readonly DbProviderFactory original;

      public DbProviderFactoryProxy(DbProviderFactory original)
      {
         if (original == null)
         {
            throw new ArgumentNullException(nameof(original));
         }
         this.original = original;
      }

      public override DbCommand CreateCommand()
      {
         return new DbCommandProxy(original.CreateCommand());
      }

      public override DbDataAdapter CreateDataAdapter()
      {
         return original.CreateDataAdapter();
      }
   }
}