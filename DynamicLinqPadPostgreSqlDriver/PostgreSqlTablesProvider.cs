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
      private readonly IConnectionInfo cxInfo;
      private readonly ModuleBuilder moduleBuilder;
      private readonly IDbConnection connection;
      private readonly string nameSpace;

      /// <summary>
      /// Tables should appear first in Explorer view
      /// </summary>
      public int Priority { get; } = 0;

      public PostgreSqlTablesProvider(IConnectionInfo cxInfo, ModuleBuilder moduleBuilder, IDbConnection connection, string nameSpace)
      {
         this.cxInfo = cxInfo;
         this.moduleBuilder = moduleBuilder;
         this.connection = connection;
         this.nameSpace = nameSpace;
      }

      public ExplorerItem EmitCodeAndGetExplorerItemTree(TypeBuilder dataContextTypeBuilder)
      {
         var query = SqlHelper.LoadSql("QueryTables.sql");
         var tables = connection.Query(query);

         var explorerItems = new List<ExplorerItem>();

         foreach (var group in tables.GroupBy(t => t.TableCatalog))
         {
            var databaseName = group.Key;

            var preparedTables = new List<TableData>();

            foreach (var table in group.OrderBy(t => t.TableName))
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

         return new ExplorerItem("Tables", ExplorerItemKind.Category, ExplorerIcon.Table)
         {
            IsEnumerable = true,
            Children = explorerItems
         };
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

         var query = SqlHelper.LoadSql("QueryColumns.sql");
         var propertyAndFieldNames = new HashSet<string>();

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
   }
}