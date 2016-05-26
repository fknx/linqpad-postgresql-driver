using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Xunit;

namespace DynamicLinqPadPostgreSqlDriver.Tests.DynamicAssemblyGeneration
{
   /// <summary>
   /// Tests for different PostgreSql function signatures to verify that the generated IL code works in all cases.
   /// </summary>
   public class TestPostgreSqlFunctions : TestBase
   {
      public TestPostgreSqlFunctions(DatabaseFixture databaseFixture)
         : base(databaseFixture)
      {
      }

      [Fact]
      public void TestKnownTableReturnType()
      {
         dynamic dc = ArrangeDataContext(db =>
         {
            db.Execute("CREATE TABLE testtable (id int, name text)");
            db.Execute("INSERT INTO testtable VALUES (1, 'test')");
            db.Execute("CREATE FUNCTION public.get_record_from_testtable() RETURNS SETOF testtable AS 'SELECT * from testtable;' LANGUAGE SQL;");
         });

         dynamic entity = ((IEnumerable<object>)dc.get_record_from_testtable()).First();

         Assert.Equal(1, entity.Id);
         Assert.Equal("test", entity.Name);
         Assert.Equal("Testtable", entity.GetType().Name);
      }

      [Fact]
      public void TestUnkownTableReturnType()
      {
         dynamic dc = ArrangeDataContext(db =>
         {
            DBConnection.Execute("CREATE FUNCTION public.return_table(p_n1 int, p_n2 int) RETURNS TABLE(id int, idstr text) AS $$ SELECT p_n1 AS id, '' || p_n1 AS name UNION ALL SELECT p_n2,'' || p_n2; $$ LANGUAGE SQL;");
         });

         var records = ((IEnumerable<object>)dc.return_table(1, 2)).ToList();
         Assert.Equal(2, records.Count);

         for (int i = 0; i < records.Count; i++)
         {
            dynamic r = records[i];
            Assert.Equal(i + 1, r.id);
            Assert.Equal((i + 1).ToString(), r.idstr);
         }
      }

      [Fact]
      public void TestScalarReturnType()
      {
         dynamic dc = ArrangeDataContext(db =>
         {
            DBConnection.Execute("CREATE FUNCTION public.add_numbers(p_n1 int, p_n2 int) RETURNS integer AS 'SELECT p_n1 + p_n2;' LANGUAGE SQL;");
         });

         var scalar = (int)dc.add_numbers(1, 2);

         Assert.Equal(3, scalar);
      }

      [Fact]
      public void TestMultiValueReturnInt32()
      {
         TestSimpleTypeMultiValueReturn("integer", 1, 2);
      }

      [Theory]
      [InlineData("text")]
      [InlineData("varchar")]
      public void TestMultiValueReturnString(string dataTypeName)
      {
         TestSimpleTypeMultiValueReturn(dataTypeName, "a", "b");
      }

      [Fact]
      public void TestMultiValueReturnDecimal()
      {
         TestSimpleTypeMultiValueReturn("decimal", 1.5M, 3.75M);
      }

      [Fact]
      public void TestArrayInputParameterInt32()
      {
         TestSimpleTypeArrayInputParameter("integer", new [] {1,2,3});
      }

      [Theory]
      [InlineData("text")]
      [InlineData("varchar")]
      public void TestArrayInputParameterString(string dataTypeName)
      {
         TestSimpleTypeArrayInputParameter(dataTypeName, new[] { "a", "b", "c" });
      }

      [Fact]
      public void TestArrayInputParameterDecimal()
      {
         TestSimpleTypeArrayInputParameter("decimal", new[] { 1.5M, 2.5M, 3.333M });
      }

      [Fact]
      public void TestUserDefinedTypeRecognition()
      {
         dynamic dc = ArrangeDataContext(db =>
         {
            DBConnection.Execute("CREATE TYPE testtype AS (id int, name text, arrayprop integer[]);");
            DBConnection.Execute("CREATE FUNCTION public.echo_udt(testtype) RETURNS testtype AS 'SELECT $1' LANGUAGE SQL;");
         });

         Type dcType = dc.GetType();
         var userDefinedType = dcType.Assembly.GetType(NameSpace + ".Testtype");

         Assert.NotNull(userDefinedType);

         dynamic customObj = Activator.CreateInstance(userDefinedType);
         customObj.Id = 1;
         customObj.Name = "1";

         // verify function is found and has correct signature
         var query = dc.echo_udt(customObj);

         // Enumerating 'query' throws something like the following:
         // System.NotSupportedExceptionThis .NET type is not supported in Npgsql or your PostgreSQL: LINQPad.User.Testtype
         // So for now, user-defined types are recognized, but functions expecting them as parameters cannot be called via LINQPad.

         // dynamic result = ((IEnumerable<object>)query).ToList();
      }

      [Fact]
      public void TestReturnJson()
      {
         dynamic dc = ArrangeDataContext(db =>
         {
            DBConnection.Execute("CREATE FUNCTION public.get_json() RETURNS json AS $$ SELECT array_to_json('{{1,5},{99,100}}'::int[]) $$ LANGUAGE SQL;");
         });

         var result = ((IEnumerable<object>)dc.get_json()).ToList();

         Assert.Equal(1, result.Count);

         dynamic record = result[0];
         Assert.Equal("[[1,5],[99,100]]", record.get_json);
      }
   }
}
