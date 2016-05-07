using System;
using Xunit;

namespace DynamicLinqPadPostgreSqlDriver.Tests
{
   public class TestBasicTypeMappings : TestBase
   {
      [Fact]
      public void TestShortMapping()
      {
         TestNonNullableType("smallint", (short) 42);
      }

      [Fact]
      public void TestNullableShortMapping()
      {
         TestNullableType<short?>("smallint", 42);
      }

      [Fact]
      public void TestIntMapping()
      {
         TestNonNullableType("int", 42);
      }

      [Fact]
      public void TestNullableIntMapping()
      {
         TestNullableType<int?>("int", 42);
      }

      [Fact]
      public void TestLongMapping()
      {
         TestNonNullableType("bigint", 42L);
      }

      [Fact]
      public void TestNullableLongMapping()
      {
         TestNullableType<long?>("bigint", 42L);
      }

      [Theory]
      [InlineData("decimal")]
      [InlineData("numeric")]
      public void TestDecimalMapping(string dataType)
      {
         TestNonNullableType(dataType, 42m);
      }

      [Theory]
      [InlineData("decimal")]
      [InlineData("numeric")]
      public void TestNullableDecimalMapping(string dataType)
      {
         TestNullableType<decimal?>(dataType, 42m);
      }

      [Fact]
      public void TestFloatMapping()
      {
         TestNonNullableType("real", 42f);
      }

      [Fact]
      public void TestNullableFloatMapping()
      {
         TestNullableType<float?>("real", 42f);
      }

      [Fact]
      public void TestDoubleMapping()
      {
         TestNonNullableType("double precision", 42d);
      }

      [Fact]
      public void TestNullableDoubleMapping()
      {
         TestNullableType<double?>("double precision", 42d);
      }

      [Theory]
      [InlineData("bool")]
      [InlineData("boolean")]
      public void TestBoolMapping(string dataType)
      {
         TestNonNullableType(dataType, true);
      }

      [Theory]
      [InlineData("bool")]
      [InlineData("boolean")]
      public void TestNullableBoolMapping(string dataType)
      {
         TestNullableType<bool?>(dataType, true);
      }

      [Theory]
      [InlineData("date")]
      [InlineData("timestamp")]
      [InlineData("timestamptz")]
      [InlineData("timestamp with time zone")]
      [InlineData("timestamp without time zone")]
      public void TestDateTimeMapping(string dataType)
      {
         TestNonNullableType(dataType, DateTime.Today);
      }

      [Theory]
      [InlineData("date")]
      [InlineData("timestamp")]
      [InlineData("timestamptz")]
      [InlineData("timestamp with time zone")]
      [InlineData("timestamp without time zone")]
      public void TestNullableDateTimeMapping(string dataType)
      {
         TestNullableType<DateTime?>(dataType, DateTime.Today);
      }

      [Theory]
      [InlineData("time")]
      [InlineData("time without time zone")]
      [InlineData("interval")]
      public void TestTimeSpanMapping(string dataType)
      {
         TestNonNullableType(dataType, TimeSpan.FromMinutes(90));
      }

      [Theory]
      [InlineData("time")]
      [InlineData("time without time zone")]
      [InlineData("interval")]
      public void TestNullableTimeSpanMapping(string dataType)
      {
         TestNullableType<TimeSpan?>(dataType, TimeSpan.FromMinutes(90));
      }

      [Theory]
      [InlineData("timetz")]
      [InlineData("time with time zone")]
      public void TestDateTimeOffsetMapping(string dataType)
      {
         var dateTime = new DateTime(0001, 01, 01, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
         var dateTimeOffset = new DateTimeOffset(dateTime.Ticks, TimeSpan.FromHours(1));

         TestNonNullableType(dataType, dateTimeOffset);
      }

      [Theory]
      [InlineData("timetz")]
      [InlineData("time with time zone")]
      public void TestNullableDateTimeOffsetMapping(string dataType)
      {
         var dateTime = new DateTime(0001, 01, 01, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
         var dateTimeOffset = new DateTimeOffset(dateTime.Ticks, TimeSpan.FromHours(1));

         TestNullableType<DateTimeOffset?>(dataType, dateTimeOffset);
      }

      [Theory]
      [InlineData("char", "4")]
      [InlineData("text", "42")]
      [InlineData("character (2)", "42")]
      [InlineData("character varying (2)", "42")]
      [InlineData("name", "42")]
      [InlineData("varchar (2)", "42")]
      public void TestStringMapping(string dataType, string value)
      {
         TestNullableType(dataType, value);
      }

      [Fact]
      public void TestByteArrayMapping()
      {
         TestNullableType("bytea", new byte[] { 42 });
      }      
   }
}
