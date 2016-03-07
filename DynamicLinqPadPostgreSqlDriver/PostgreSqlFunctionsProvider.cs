using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using Dapper;
using LINQPad.Extensibility.DataContext;

namespace DynamicLinqPadPostgreSqlDriver
{
    internal class PostgreSqlFunctionsProvider : IDatabaseObjectProvider
    {
        private readonly IConnectionInfo cxInfo;
        private readonly ModuleBuilder moduleBuilder;
        private readonly IDbConnection connection;
        private readonly string nameSpace;

        /// <summary>
        /// Functions should appear after tables in Explorer view
        /// </summary>
        public int Priority { get; } = 1;

        public PostgreSqlFunctionsProvider(IConnectionInfo cxInfo, ModuleBuilder moduleBuilder, IDbConnection connection, string nameSpace)
        {
            this.cxInfo = cxInfo;
            this.moduleBuilder = moduleBuilder;
            this.connection = connection;
            this.nameSpace = nameSpace;
        }

        public ExplorerItem EmitCodeAndGetExplorerItemTree(TypeBuilder dataContextTypeBuilder)
        {
            var sql = SqlHelper.LoadSql("QueryFunctions.sql");

            var functionData = connection.Query<FunctionData2>(sql).ToList();

            var functionExplorerItems = new List<ExplorerItem>();
            foreach (var func in functionData.OrderBy(x => x.Name))
            {
                var argumentTypes = connection.Query(SqlHelper.LoadSql("QueryTypeByOid.sql").Replace("@oids", string.Join(",", func.ArgumentTypeOids)))
                    .ToDictionary(x => (int)x.oid, x => (string)x.typname);

                var funcType = func.IsMultiValueReturn ? ExplorerIcon.TableFunction : ExplorerIcon.ScalarFunction;

                var paramExplorerItems = new List<ExplorerItem>();
                for (int i = 0; i < func.ArgumentCount; i++)
                {
                    var argName = func.ArgumentNames[i];
                    var argType = argumentTypes[func.ArgumentTypeOids[i]];
                    Type mappedArgType;

                    if (argType.StartsWith("_"))
                    {
                        mappedArgType = SqlHelper.MapDbTypeToType("array", argType, false, true);
                    }
                    else
                    {
                        mappedArgType = SqlHelper.MapDbTypeToType(argType, null, false, false);
                    }

                    if (mappedArgType == null)
                    {
                        continue;
                        
                        throw new InvalidOperationException($"No mapping found for database type {argType}");
                    }

                    var itemText = $"{argName} ({mappedArgType.Name})";

                    paramExplorerItems.Add(new ExplorerItem(itemText, ExplorerItemKind.Parameter, ExplorerIcon.Parameter));
                }

                var explorerItem = new ExplorerItem(func.Name, ExplorerItemKind.QueryableObject, funcType)
                {
                    IsEnumerable = true,
                    Children = paramExplorerItems
                };

                functionExplorerItems.Add(explorerItem);
            }

            return new ExplorerItem("Functions", ExplorerItemKind.Category, ExplorerIcon.ScalarFunction)
            {
                IsEnumerable = true,
                Children = functionExplorerItems
            };
        }
    }

    internal class FunctionData2
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public int ArgumentCount { get; set; }
        public string[] ArgumentNames { get; set; }
        public int[] ArgumentTypeOids { get; set; }
        public object[] ArgumentDefaults { get; set; }
        public bool IsMultiValueReturn { get; set; }
    }

    internal class FunctionData
    {
        public FunctionData(ExplorerItem explorerItem, string name)
        {
            this.ExplorerItem = explorerItem;
            this.Name = name;
        }

        public string Name { get; }

        public ExplorerItem ExplorerItem { get; }
    }
}