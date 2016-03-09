using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LINQPad.Extensibility.DataContext;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace DynamicLinqPadPostgreSqlDriver.Tests
{
   public class TestDynamicAssemblyGeneration
   {
      private const string ProviderName = "PostgreSql";
      private const string ConnectionString = "Server=localhost;Port=5433;Database=contractdata;User Id=postgres;Password=postgres;";

      [Fact]
      public void TestTypedDataContextCreation()
      {
         var fixture = new Fixture();
         fixture.Customize(new AutoConfiguredMoqCustomization());
         var cxInfo = fixture.Create<IConnectionInfo>();
         Mock.Get(cxInfo).Setup(x => x.DatabaseInfo).ReturnsUsingFixture(fixture);
         Mock.Get(cxInfo.DatabaseInfo).Setup(x => x.CustomCxString).Returns(ConnectionString);

         var asmName = new AssemblyName("TypedDataContext_test");
         asmName.CodeBase = Path.Combine(Environment.CurrentDirectory,asmName.Name + ".dll");

         var nameSpace = "LINQPad.User";
         var typeName = "TypedDataContext";

         var driver = new DynamicPostgreSqlDriver();
         driver.GetSchemaAndBuildAssembly(cxInfo, asmName, ref nameSpace, ref typeName);

         var createdAssembly = Assembly.Load(asmName);
         var dcType = createdAssembly.GetType($"{nameSpace}.{typeName}");
         dynamic dc = Activator.CreateInstance(dcType, ProviderName, ConnectionString);

         var resultset = (IEnumerable<object>)dc.get_allotmentsets(1,150243,1);
         var results = resultset.ToList();

         // TODO write tests for different kinds of postgresql functions
      }
   }
}
