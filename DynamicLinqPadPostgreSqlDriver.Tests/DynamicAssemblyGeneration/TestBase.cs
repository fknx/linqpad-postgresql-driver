using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;
using LINQPad.Extensibility.DataContext;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace DynamicLinqPadPostgreSqlDriver.Tests.DynamicAssemblyGeneration
{
   public class TestBase : IClassFixture<DatabaseFixture>, IDisposable
   {
      protected const string AssemblyName = "TypedDataContext_test";
      protected const string NameSpace = "LINQPad.User";
      protected const string TypeName = "TypedDataContext";

      protected IDbConnection DBConnection { get; }

      // Each generated dynamic assembly needs an id unique to the test run, because
      // we cannot unload an assembly once loaded into the app domain.
      private static int _assemblyId;

      public TestBase(DatabaseFixture databaseFixture)
      {
         DBConnection = databaseFixture.DBConnection;
      }

      protected void TestSimpleTypeMultiValueReturn<T>(string pgTypeName, T value1, T value2)
      {
         dynamic dc = ArrangeDataContext(db =>
         {
            DBConnection.Execute($"CREATE FUNCTION public.return_array(p_n1 {pgTypeName}, p_n2 {pgTypeName}) RETURNS SETOF {pgTypeName} AS 'SELECT p_n1 UNION ALL SELECT p_n2;' LANGUAGE SQL;");
         });

         var array = ((IEnumerable<T>)dc.return_array(value1, value2)).ToList();

         Assert.Equal(new[] { value1, value2 }, array);
      }

      public void TestSimpleTypeArrayInputParameter<T>(string pgTypeName, T[] values)
      {
         dynamic dc = ArrangeDataContext(db =>
         {
            DBConnection.Execute(string.Format(
               "CREATE FUNCTION public.join_array(p_n {0}, p_arr {0}[]) RETURNS TABLE(n1 {0}, n2 {0}) AS 'SELECT p_n,a FROM unnest(p_arr) a;' LANGUAGE SQL;",
               pgTypeName));
         });

         var records = ((IEnumerable<object>)dc.join_array(values[0], values)).ToList();

         for (int i = 0; i < records.Count; i++)
         {
            dynamic r = records[i];

            Assert.Equal(values[0], r.n1);
            Assert.Equal(values[i], r.n2);
         }
      }

      public void TestInputParametersWithDefault(params InputParameterTestData[] testData)
      {
         var parameterDeclaration = string.Join(",", testData.Select(x => $"IN p_{x.Name} {x.PgSqlType}{(x.Default != null ? " DEFAULT " + x.Default : "")}"));
         var returnTableDeclaration = string.Join(",", testData.Select(x => $"{x.Name} {x.PgSqlType}"));
         var selectColumns = string.Join(",", testData.Select(x => $"p_{x.Name}"));

         // This crashes with an NpgsqlException when the argument defaults are not handled separately. Npgsql seems to be unable to determine the type.
         ArrangeDataContext(db =>
         {
            DBConnection.Execute($"CREATE FUNCTION public.echo_params({parameterDeclaration}) RETURNS TABLE({returnTableDeclaration}) AS 'SELECT {selectColumns};' LANGUAGE SQL;");
         });
      }

      public TypedDataContextBase ArrangeDataContext(Action<IDbConnection> prepareDatabase)
      {
         prepareDatabase(DBConnection);
         return BuildAssemblyAndCreateTypedDataContext();
      }

      protected static TypedDataContextBase BuildAssemblyAndCreateTypedDataContext()
      {
         var cxInfo = CreateConnectionInfo();
         var asmName = CreateDynamicAssembly(cxInfo);

         return CreateTypedDataContext(asmName);
      }

      private static AssemblyName CreateDynamicAssembly(IConnectionInfo cxInfo)
      {
         var asmName = new AssemblyName(AssemblyName + (++_assemblyId));
         asmName.CodeBase = Path.Combine(Environment.CurrentDirectory, asmName.Name + ".dll");

         var nameSpace = NameSpace;
         var typeName = TypeName;

         var driver = new DynamicPostgreSqlDriver();
         driver.GetSchemaAndBuildAssembly(cxInfo, asmName, ref nameSpace, ref typeName);

         return asmName;
      }

      private static IConnectionInfo CreateConnectionInfo()
      {
         var fixture = new Fixture();
         fixture.Customize(new AutoConfiguredMoqCustomization());
         var cxInfo = fixture.Create<IConnectionInfo>();
         Mock.Get(cxInfo).Setup(x => x.DatabaseInfo).ReturnsUsingFixture(fixture);
         Mock.Get(cxInfo.DatabaseInfo).Setup(x => x.CustomCxString).Returns(DatabaseFixture.ConnectionString);

         return cxInfo;
      }

      private static TypedDataContextBase CreateTypedDataContext(AssemblyName asmName)
      {
         var createdAssembly = Assembly.Load(asmName);
         var dcType = createdAssembly.GetType($"{NameSpace}.{TypeName}");
         var dc = Activator.CreateInstance(dcType, DatabaseFixture.ProviderName, DatabaseFixture.ConnectionString);
         return (TypedDataContextBase)dc;
      }

      public void Dispose()
      {
         DBConnection.Execute("DROP SCHEMA public CASCADE;");
         DBConnection.Execute("CREATE SCHEMA public;");

         File.Delete(AssemblyName + _assemblyId + ".dll");
      }
   }
}
