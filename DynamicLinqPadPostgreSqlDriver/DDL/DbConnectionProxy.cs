using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace DynamicLinqPadPostgreSqlDriver.DDL
{
   [Serializable]
   public class DbConnectionProxy : DbConnection
   {
      internal readonly DbConnection Original;

      public DbConnectionProxy(DbConnection original)
      {
         if (original == null)
         {
            throw new ArgumentNullException(nameof(original));
         }
         Original = original;
      }

      public new void Dispose()
      {
         Original.Dispose();
      }

      protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
      {
         return Original.BeginTransaction(isolationLevel);
      }

      public new IDbTransaction BeginTransaction()
      {
         return Original.BeginTransaction();
      }

      public new IDbTransaction BeginTransaction(IsolationLevel il)
      {
         return Original.BeginTransaction(il);
      }

      public override void Close()
      {
         Original.Close();
      }

      public override void ChangeDatabase(string databaseName)
      {
         Original.ChangeDatabase(databaseName);
      }

      public new IDbCommand CreateCommand()
      {
         return new DbCommandProxy(Original.CreateCommand());
      }

      protected override DbCommand CreateDbCommand()
      {
         return new DbCommandProxy(Original.CreateCommand());
      }

      public override void Open()
      {
         Original.Open();
      }

      public override string ConnectionString {
         get
         {
            return Original.ConnectionString;
         }
         set
         {
            Original.ConnectionString = value;
         }
      }
      public override int ConnectionTimeout => Original.ConnectionTimeout;
      public override string Database => Original.Database;
      public override string DataSource => Original.DataSource;
      public override string ServerVersion => Original.ServerVersion;
      public override ConnectionState State => Original.State;

      /// <summary>
      /// This is accessed in the LINQPad code via reflection
      /// </summary>
      protected override DbProviderFactory DbProviderFactory
      {
         get
         {
            var providerFactoryMethod = Original.GetType().GetProperty("DbProviderFactory", BindingFlags.Instance | BindingFlags.NonPublic);
            var originalFactory = (DbProviderFactory)providerFactoryMethod?.GetValue(Original) ?? base.DbProviderFactory;
            return new DbProviderFactoryProxy(originalFactory);
         }
      }
   }
}
