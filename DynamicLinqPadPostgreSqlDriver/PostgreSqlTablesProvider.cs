using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Dapper;
using DynamicLinqPadPostgreSqlDriver.Extensions;
using LINQPad.Extensibility.DataContext;

namespace DynamicLinqPadPostgreSqlDriver
{
   internal class PostgreSqlTablesProvider : IDatabaseObjectProvider
   {
      private readonly IConnectionInfo _cxInfo;
      private readonly ModuleBuilder _moduleBuilder;
      private readonly IDbConnection _connection;
      private readonly string _nameSpace;

      /// <summary>
      /// Tables should appear first in Explorer view
      /// </summary>
      public int Priority { get; } = 0;

      public PostgreSqlTablesProvider(IConnectionInfo cxInfo, ModuleBuilder moduleBuilder, IDbConnection connection, string nameSpace)
      {
         _cxInfo = cxInfo;
         _moduleBuilder = moduleBuilder;
         _connection = connection;
         _nameSpace = nameSpace;
      }

      public ExplorerItem EmitCodeAndGetExplorerItemTree(TypeBuilder dataContextTypeBuilder)
      {
         var query = SqlHelper.LoadSql("QueryTables.sql");
         var tables = _connection.Query(query);

         //  The explorer items below the "Tables" explorer items can be either the
         // schemas or directly the tables, if only one schema is available (or selected).
         var rootExplorerItems = new List<ExplorerItem>();

         var schemas = _cxInfo.GetSchemas();

         var schemaGroups = schemas.Any()
            ? tables.GroupBy(t => t.TableSchema).Where(g => schemas.Contains(((string)g.Key).ToLower())).ToArray()
            : tables.GroupBy(t => t.TableSchema).ToArray();

         // If there is only one schema, then the schema explorer item can be skipped and
         // it is not required to add the schema to the table name.
         var onlyOneSchema = schemaGroups.Length == 1;

         foreach (var schemaGroup in schemaGroups)
         {
            var tableExplorerItems = new List<ExplorerItem>();
            var preparedTables = new List<TableData>();

            foreach (var table in schemaGroup.OrderBy(t => t.TableName))
            {
               var unmodifiedTableName = (string) table.TableName;
               var unmodifiedTableSchema = (string) table.TableSchema;

               var tableName = onlyOneSchema
                  ? _cxInfo.GetTableName(unmodifiedTableName)
                  : _cxInfo.GetTableName(unmodifiedTableSchema + "_" + unmodifiedTableName);

               var explorerItem = new ExplorerItem(tableName, ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
               {
                  IsEnumerable = true,
                  Children = new List<ExplorerItem>(),
                  DragText = tableName,
                  SqlName = $"{unmodifiedTableSchema}.{unmodifiedTableName}"
               };

               var tableData = PrepareTableEntity(_cxInfo, _moduleBuilder, _connection, _nameSpace, table.TableCatalog,
                  unmodifiedTableSchema, unmodifiedTableName, explorerItem);
               
               preparedTables.Add(tableData);
            }

            // build the associations before the types are created
            BuildAssociations(_connection, schemaGroup.Key, preparedTables);

            foreach (var tableData in preparedTables)
            {
               dataContextTypeBuilder.CreateAndAddType(tableData);
               tableExplorerItems.Add(tableData.ExplorerItem);
            }

            if (onlyOneSchema)
            {
               rootExplorerItems.AddRange(tableExplorerItems);
               continue;
            }

            var schemaExplorerItem = new ExplorerItem(schemaGroup.Key, ExplorerItemKind.Category, ExplorerIcon.Schema)
            {
               IsEnumerable = true,
               Children = tableExplorerItems
            };

            rootExplorerItems.Add(schemaExplorerItem);
         }

         return new ExplorerItem("Tables", ExplorerItemKind.Category, ExplorerIcon.Table)
         {
            IsEnumerable = true,
            Children = rootExplorerItems
         };
      }

      private static TableData PrepareTableEntity(IConnectionInfo cxInfo, ModuleBuilder moduleBuilder, IDbConnection dbConnection, string nameSpace, string databaseName, string tableSchema, string tableName, ExplorerItem tableExplorerItem)
      {
         // get primary key columns
         var primaryKeyColumns = dbConnection.GetPrimaryKeyColumns(tableSchema, tableName);

         var typeName = $"{nameSpace}.{tableSchema}.{cxInfo.GetTypeName(tableName)}";

         // ToDo make sure tablename can be used
         var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public, typeof(Entity));

         // add the table attribute to the class
         typeBuilder.AddTableAttribute($"\"{tableSchema}\".\"{tableName}\"");

         var query = SqlHelper.LoadSql("QueryColumns.sql");
         var propertyAndFieldNames = new HashSet<string>();

         var columns = dbConnection.Query(query, new { DatabaseName = databaseName, TableSchema = tableSchema, TableName = tableName });
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
               fieldBuilder.AddColumnAttribute((string)column.ColumnName); // always use the unmodified column name

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
               propertyAndFieldNames.Add(columnName);
            }
            else
            {
               // field type is not mapped
               text = $"{columnName} (unsupported - {column.DataType})";
            }

            var explorerItem = new ExplorerItem(text, ExplorerItemKind.Property, ExplorerIcon.Column)
            {
               SqlTypeDeclaration = column.DataType,
               DragText = columnName
            };

            tableExplorerItem.Children.Add(explorerItem);
         }

         return new TableData(tableName, tableExplorerItem, typeBuilder, propertyAndFieldNames);
      }

      private void BuildAssociations(IDbConnection connection, string tableSchema, ICollection<TableData> preparedTables)
      {
         var query = SqlHelper.LoadSql("QueryForeignKeys.sql");

         var foreignKeys = connection.Query(query, new { TableSchema = tableSchema });

         foreach (var foreignKey in foreignKeys)
         {
            var constraintName = (string)foreignKey.ConstraintName;

            var table = preparedTables.FirstOrDefault(t => t.Name == foreignKey.TableName);
            var foreignTable = preparedTables.FirstOrDefault(t => t.Name == foreignKey.ForeignTableName);

            var columnNames = (string)foreignKey.ColumnNames;
            var foreignColumnNames = (string)foreignKey.ForeignColumnNames;

            if (table == null || foreignTable == null || string.IsNullOrWhiteSpace(columnNames) || string.IsNullOrWhiteSpace(foreignColumnNames))
               continue;

            // create one-to-many association
            table.CreateOneToManyAssociation(foreignTable, columnNames, foreignColumnNames, constraintName);
         }
      }
   }
}