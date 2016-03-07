using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using LINQPad.Extensibility.DataContext;

namespace DynamicLinqPadPostgreSqlDriver
{
    public class DatabaseObjectProviderManager
    {
        public IDatabaseObjectProvider[] DatabaseObjectProviders { get; }

        public DatabaseObjectProviderManager(IConnectionInfo cxInfo, ModuleBuilder moduleBuilder, IDbConnection connection, string nameSpace)
        {
            var providerTypes = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof (IDatabaseObjectProvider)));
            this.DatabaseObjectProviders = providerTypes.Select(t => (IDatabaseObjectProvider)Activator.CreateInstance(t, cxInfo, moduleBuilder, connection, nameSpace)).ToArray();
        }
    }
}