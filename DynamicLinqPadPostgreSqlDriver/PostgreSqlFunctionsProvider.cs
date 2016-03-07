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

            var functions = new List<FunctionData>();
            using (var rdr = connection.ExecuteReader(sql))
            {
                while (rdr.Read())
                {
                    functions.Add(new FunctionData(null, (string)rdr["proname"]));
                }
            }

            var functionExplorerItems = new List<ExplorerItem>();
            foreach (var func in functions.OrderBy(x => x.Name))
            {
                var explorerItem = new ExplorerItem(func.Name, ExplorerItemKind.QueryableObject, ExplorerIcon.TableFunction)
                {
                    IsEnumerable = true,
                    Children = new List<ExplorerItem>()
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