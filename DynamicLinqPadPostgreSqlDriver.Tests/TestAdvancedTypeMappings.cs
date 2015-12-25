using System;
using System.Collections;
using Xunit;

namespace DynamicLinqPadPostgreSqlDriver.Tests
{
   public class TestAdvancedTypeMappings : TestBase
   {
      [Theory]
      [InlineData("varbit")]
      [InlineData("bit varying")]
      public void TestBitArrayMapping(string dataType)
      {
         TestNullableType(dataType, new BitArray(5));
      }

      [Fact]
      public void TestGuidMapping()
      {
         TestNonNullableType("uuid", Guid.NewGuid());
      }

      [Fact]
      public void TestNullableGuidMapping()
      {
         TestNullableType<Guid?>("uuid", Guid.NewGuid());
      }
   }
}
