using System;
using System.Data;
using System.Data.Common;
using Npgsql;

namespace DynamicLinqPadPostgreSqlDriver.DDL
{
   [Serializable]
   public class DbCommandProxy : DbCommand
   {
      private readonly CommandTextInterceptor interceptor = new CommandTextInterceptor();
      private readonly DbCommand original;

      public DbCommandProxy(DbCommand original)
      {
         if (original == null)
         {
            throw new ArgumentNullException(nameof(original));
         }
         this.original = original;
      }

      private T HandleEx<T>(Func<T> func)
      {
         try
         {
            return func();
         }
         // NpgsqlException does not implement MarshalByRefObject and therefore causes a SerializationException
         // when thrown, obfuscating the real error message.
         catch (NpgsqlException ex)
         {
            throw new DataException($"{ex.Message} --- {CommandText}");
         }
      }

      public new void Dispose()
      {
         original.Dispose();
      }

      public override void Prepare()
      {
         original.Prepare();
      }

      public override void Cancel()
      {
         original.Cancel();
      }

      public new IDbDataParameter CreateParameter()
      {
         return original.CreateParameter();
      }

      protected override DbParameter CreateDbParameter()
      {
         return original.CreateParameter();
      }

      protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
      {
         return HandleEx(() => original.ExecuteReader(behavior));
      }

      public override int ExecuteNonQuery()
      {
         return HandleEx(() => original.ExecuteNonQuery());
      }

      public new IDataReader ExecuteReader()
      {
         return HandleEx(() => original.ExecuteReader());
      }

      public new IDataReader ExecuteReader(CommandBehavior behavior)
      {
         return HandleEx(() => original.ExecuteReader(behavior));
      }

      public override object ExecuteScalar()
      {
         return HandleEx(() => original.ExecuteScalar());
      }

      public new DbConnection Connection
      {
         get
         {
            return DbConnection;
         }
         set
         {
            DbConnection = value;
         }
      }

      protected override DbConnection DbConnection
      {
         get
         {
            return original.Connection;
         }
         set
         {
            if (value is DbConnectionProxy)
            {
               original.Connection = ((DbConnectionProxy)value).Original;
            }
            else
            {
               original.Connection = value;
            }
         }
      }

      protected override DbParameterCollection DbParameterCollection => original.Parameters;

      protected override DbTransaction DbTransaction
      {
         get
         {
            return original.Transaction;
         }
         set
         {
            original.Transaction = value;
         }
      }

      public override bool DesignTimeVisible
      {
         get
         {
            return original.DesignTimeVisible;
         }
         set
         {
            original.DesignTimeVisible = value;
         }
      }

      public new DbTransaction Transaction
      {
         get
         {
            return original.Transaction;
         }
         set
         {
            original.Transaction = value;
         }
      }

      public override string CommandText
      {
         get
         {
            return original.CommandText;
         }
         set
         {
            original.CommandText = interceptor.GetCommandText(value);
         }
      }

      public override int CommandTimeout
      {
         get
         {
            return original.CommandTimeout;
         }
         set
         {
            original.CommandTimeout = value;
         }
      }

      public override CommandType CommandType
      {
         get
         {
            return original.CommandType;
         }
         set
         {
            original.CommandType = value;
         }
      }

      public new IDataParameterCollection Parameters => original.Parameters;

      public override UpdateRowSource UpdatedRowSource
      {
         get
         {
            return original.UpdatedRowSource;
         }
         set
         {
            original.UpdatedRowSource = value;
         }
      }
   }
}