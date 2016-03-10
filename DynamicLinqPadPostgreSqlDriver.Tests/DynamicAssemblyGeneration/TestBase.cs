using System;
using System.Data;
using System.IO;
using System.Reflection;
using LINQPad.Extensibility.DataContext;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace DynamicLinqPadPostgreSqlDriver.Tests.DynamicAssemblyGeneration
{
   public class TestBase : IClassFixture<DatabaseFixture>
   {
      protected const string AssemblyName = "TypedDataContext_test";
      protected const string NameSpace = "LINQPad.User";
      protected const string TypeName = "TypedDataContext";
      
      protected IDbConnection DBConnection { get; }

      public TestBase(DatabaseFixture dbContext)
      {
         DBConnection = dbContext.DBConnection;
      }

      protected static TypedDataContextBase BuildAssemblyAndCreateTypedDataContext()
      {
         var cxInfo = CreateConnectionInfo();
         var asmName = CreateDynamicAssembly(cxInfo);

         return CreateTypedDataContext(asmName);
      }

      private static AssemblyName CreateDynamicAssembly(IConnectionInfo cxInfo)
      {
         var asmName = new AssemblyName(AssemblyName);
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
   }
}
