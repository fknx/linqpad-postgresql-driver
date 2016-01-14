using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Npgsql;
using Dapper;
using System.Threading;
using System.Reflection.Emit;
using System.IO;
using LinqToDB.Data;
using System.Data;
using LinqToDB.Mapping;
using DynamicLinqPadPostgreSqlDriver.Extensions;
using DynamicLinqPadPostgreSqlDriver.UI;
using DynamicLinqPadPostgreSqlDriver.Shared.Extensions;

namespace DynamicLinqPadPostgreSqlDriver
{
   public class DynamicPostgreSqlDriver : DynamicDataContextDriver
   {
      public override string Author => "Frederik Knust";

      public override string Name => "PostgreSQL (LINQ to DB)";

      public override string GetConnectionDescription(IConnectionInfo cxInfo)
      {
         if (!string.IsNullOrWhiteSpace(cxInfo.DisplayName))
            return cxInfo.DisplayName;

         if (string.IsNullOrWhiteSpace(cxInfo.DatabaseInfo.CustomCxString))
            return $"{cxInfo.DatabaseInfo.Server} - {cxInfo.DatabaseInfo.Database}";

         return "PostgreSql";
      }

      public override IDbConnection GetIDbConnection(IConnectionInfo cxInfo)
      {
         var connectionString = cxInfo.GetPostgreSqlConnectionString();

         var connection = new NpgsqlConnection(connectionString);
         return connection;
      }

      public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
      {
         yield return "linq2db.dll";
         yield return "Npgsql.dll";
      }

      public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
      {
         yield return "System.Net"; // for IPAddress type
         yield return "System.Net.NetworkInformation"; // for PhysicalAddress type
         yield return "LinqToDB";
         yield return "NpgsqlTypes";
      }

      public override void ClearConnectionPools(IConnectionInfo cxInfo)
      {
         NpgsqlConnection.ClearAllPools();
      }

      public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
      {
         var providerNameParameter = new ParameterDescriptor("providerName", "System.String");
         var connectionStringParameter = new ParameterDescriptor("connectionString", "System.String");

         return new[] { providerNameParameter, connectionStringParameter };
      }

      public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
      {
         return new object[] { "PostgreSQL", cxInfo.GetPostgreSqlConnectionString() };
      }

      public override List<ExplorerItem> GetSchemaAndBuildAssembly(IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
      {
         var fileName = Path.GetFileName(assemblyToBuild.CodeBase);
         var directory = Path.GetDirectoryName(assemblyToBuild.CodeBase);

         var assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyToBuild, AssemblyBuilderAccess.RunAndSave, directory);
         var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyToBuild.Name, fileName);

         var dataContextTypeBuilder = moduleBuilder.DefineType(string.Format("{0}.{1}", nameSpace, typeName), TypeAttributes.Public, typeof(TypedDataContextBase));

         var explorerItems = new List<ExplorerItem>();

         using (var connection = GetIDbConnection(cxInfo))
         {
            connection.Open();

            var query = SqlHelper.LoadSql("QueryTables.sql");
            var tables = connection.Query(query);

            foreach (var group in tables.GroupBy(t => t.TableCatalog))
            {
               var databaseName = group.Key;

               var preparedTables = new List<TableData>();
               
               foreach(var table in group.OrderBy(t => t.TableName))
               {
                  var unmodifiedTableName = (string)table.TableName;
                  var tableName = cxInfo.GetTableName(unmodifiedTableName);

                  var explorerItem = new ExplorerItem(tableName, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                  {
                     IsEnumerable = true,
                     Children = new List<ExplorerItem>(),
                     DragText = tableName,
                     SqlName = $"\"{unmodifiedTableName}\""
                  };

                  var tableData = PrepareTableEntity(cxInfo, moduleBuilder, connection, nameSpace, databaseName, unmodifiedTableName, explorerItem);
                  preparedTables.Add(tableData);
               }

               // build the associations before the types are created
               BuildAssociations(connection, preparedTables);

               foreach (var tableData in preparedTables)
               {
                  dataContextTypeBuilder.CreateAndAddType(tableData);
                  explorerItems.Add(tableData.ExplorerItem);
               }
            }
         }

         // fetch the base constructor which shall be called by the new constructor
         var baseConstructor = typeof(TypedDataContextBase).GetConstructor(new[] { typeof(string), typeof(string) });

         // create the typed data context's constructor
         var dataContextConstructor = dataContextTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(string), typeof(string) });
         var ilGenerator = dataContextConstructor.GetILGenerator();

         ilGenerator.Emit(OpCodes.Ldarg_0);
         ilGenerator.Emit(OpCodes.Ldarg_1);
         ilGenerator.Emit(OpCodes.Ldarg_2);

         // call the base constructor
         ilGenerator.Emit(OpCodes.Call, baseConstructor);

         ilGenerator.Emit(OpCodes.Ret);

         dataContextTypeBuilder.CreateType();

         assemblyBuilder.Save(fileName);

         return explorerItems;
      }

      private static TableData PrepareTableEntity(IConnectionInfo cxInfo, ModuleBuilder moduleBuilder, IDbConnection dbConnection, string nameSpace, string databaseName, string tableName, ExplorerItem tableExplorerItem)
      {
         // get primary key columns
         var primaryKeyColumns = dbConnection.GetPrimaryKeyColumns(tableName);

         var typeName = $"{nameSpace}.{cxInfo.GetTypeName(tableName)}";

         // ToDo make sure tablename can be used
         var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public, typeof(Entity));

         // add the table attribute to the class
         typeBuilder.AddTableAttribute(tableName);

         var columnAttributeConstructor = typeof(ColumnAttribute).GetConstructor(new[] { typeof(string) });

         var query = SqlHelper.LoadSql("QueryColumns.sql");

         var columns = dbConnection.Query(query, new { DatabaseName = databaseName, TableName = tableName });
         foreach (var column in columns)
         {
            var columnName = cxInfo.GetColumnName((string)column.ColumnName);
            var isPrimaryKeyColumn = primaryKeyColumns.Contains((string)column.ColumnName); // always use the unmodified column name
            var columnDefault = (string)column.ColumnDefault;

            // always make primary key columns nullable (otherwise auto increment can't be used with an insert)
            var fieldType = (Type)SqlHelper.MapDbTypeToType(column.DataType, column.UdtName, "YES".Equals(column.Nullable, StringComparison.InvariantCultureIgnoreCase), cxInfo.UseAdvancedTypes());

            string text;

            if (fieldType != null)
            {
               // ToDo make sure name can be used
               var fieldBuilder = typeBuilder.DefineField(columnName, fieldType, FieldAttributes.Public);
               fieldBuilder.AddColumnAttribute((string) column.ColumnName); // always use the unmodified column name

               if (isPrimaryKeyColumn)
               {
                  // check if the column is an identity column
                  if (!string.IsNullOrEmpty(columnDefault) && columnDefault.ToLower().StartsWith("nextval"))
                  {
                     fieldBuilder.AddIdentityAttribute();
                  }

                  fieldBuilder.AddPrimaryKeyAttribute();
               }

               text = $"{columnName} ({fieldType.GetTypeName()})";
            }
            else
            {
               // field type is not mapped
               text = $"{columnName} (unsupported - {column.DataType})";
            }

            var explorerItem = new ExplorerItem(text, ExplorerItemKind.Property, ExplorerIcon.Column)
            {
               SqlTypeDeclaration = column.DataType
            };

            tableExplorerItem.Children.Add(explorerItem);
         }

         return new TableData(tableName, tableExplorerItem, typeBuilder);
      }

      private void BuildAssociations(IDbConnection connection, ICollection<TableData> preparedTables)
      {
         var query = SqlHelper.LoadSql("QueryForeignKeys.sql");

         var foreignKeys = connection.Query(query);

         foreach (var foreignKey in foreignKeys)
         {
            var table = preparedTables.FirstOrDefault(t => t.Name == foreignKey.TableName);
            var foreignTable = preparedTables.FirstOrDefault(t => t.Name == foreignKey.ForeignTableName);

            var primaryKeyName = (string)foreignKey.ForeignColumnName;
            var foreignKeyName = (string)foreignKey.ColumnName;

            if (table == null || foreignTable == null || string.IsNullOrWhiteSpace(foreignKeyName) || string.IsNullOrWhiteSpace(primaryKeyName))
               continue;

            // create one-to-many association
            table.CreateOneToManyAssociation(foreignTable, primaryKeyName, foreignKeyName);
         }
      }

      public override bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isNewConnection)
      {
         return UIHelper.ShowConfigurationWindow(cxInfo);
      }
   }
}
