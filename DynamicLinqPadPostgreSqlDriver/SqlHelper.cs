using NpgsqlTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;

namespace DynamicLinqPadPostgreSqlDriver
{
   public static class SqlHelper
   {
      private static readonly IDictionary<string, string> SqlCache = new Dictionary<string, string>();

      public static string LoadSql(string name)
      {
         var resourceName = $"DynamicLinqPadPostgreSqlDriver.SQL.{name}";

         string sql;
         if (SqlCache.TryGetValue(resourceName, out sql))
            return sql;

         using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
         {
            if (stream == null)
               throw new Exception($"There is no resource with name '{resourceName}'.");

            using (var streamReader = new StreamReader(stream))
            {
               sql = streamReader.ReadToEnd();
               SqlCache[resourceName] = sql;

               return sql;
            }
         }
      }

      public static Type MapDbTypeToType(string dbType, string udtName, bool nullable, bool useExperimentalTypes)
      {
         if (dbType == null)
            throw new ArgumentNullException(nameof(dbType));

         dbType = dbType.ToLower();

         // See the PostgreSQL datatypes as reference:
         //
         // http://www.npgsql.org/doc/types.html
         // 
         // http://www.postgresql.org/docs/9.4/static/datatype-numeric.html
         // http://www.postgresql.org/docs/9.4/static/datatype-datetime.html
         // http://www.postgresql.org/docs/9.4/static/datatype-binary.html
         // http://www.postgresql.org/docs/9.4/static/datatype-character.html

         // handle the basic types first
         switch (dbType)
         {
            case "int2":
            case "smallint":
               return nullable ? typeof(short?) : typeof(short);
            case "int4":
            case "integer":
            case "serial":
               return nullable ? typeof(int?) : typeof(int);
            case "int8":
            case "bigint":
            case "bigserial":
               return nullable ? typeof(long?) : typeof(long);
            case "decimal":
            case "numeric":
               return nullable ? typeof(decimal?) : typeof(decimal);
            case "real":
               return nullable ? typeof(float?) : typeof(float);
            case "double precision":
               return nullable ? typeof(double?) : typeof(double);
            case "bit":
               return null; // not supported ?
            case "bool":
            case "boolean":
               return nullable ? typeof(bool?) : typeof(bool);
            case "date":
            case "timestamp":
            case "timestamptz":
            case "timestamp with time zone":
            case "timestamp without time zone":
               return nullable ? typeof(DateTime?) : typeof(DateTime);
            case "time":
            case "time without time zone":
            case "interval":
               return nullable ? typeof(TimeSpan?) : typeof(TimeSpan);
            case "timetz":
            case "time with time zone":
               return nullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset);
            case "char":
            case "text":
            case "character":
            case "character varying":
            case "name":
            case "varchar":
               return typeof(string);
            case "bytea":
               return typeof(byte[]);
         }

         if (!useExperimentalTypes)
            return null;

         // handle advanced type mappings
         switch (dbType)
         {
            case "json":
            case "jsonb":
            case "xml":
               return typeof(string);
            case "point":
               return nullable ? typeof(NpgsqlPoint?) : typeof(NpgsqlPoint); // untested
            case "lseg":
               return nullable ? typeof(NpgsqlLSeg?) : typeof(NpgsqlLSeg); // untested
            case "path":
               return nullable ? typeof(NpgsqlPath?) : typeof(NpgsqlPath); // untested
            case "polygon":
               return nullable ? typeof(NpgsqlPolygon?) : typeof(NpgsqlPolygon); // untested
            case "line":
               return nullable ? typeof(NpgsqlLine?) : typeof(NpgsqlLine); // untested
            case "circle":
               return nullable ? typeof(NpgsqlCircle?) : typeof(NpgsqlCircle); // untested
            case "box":
               return nullable ? typeof(NpgsqlBox?) : typeof(NpgsqlBox); // untested
            case "varbit":
            case "bit varying":
               return typeof(BitArray); // untested
            case "hstore":
               return typeof(IDictionary<string, string>); // untested
            case "uuid":
               return nullable ? typeof(Guid?) : typeof(Guid);
            case "cidr":
               return nullable ? typeof(NpgsqlInet?) : typeof(NpgsqlInet); // untested
            case "inet":
               return typeof(IPAddress); // untested
            case "macaddr":
               return typeof(PhysicalAddress); // untested
            case "tsquery":
               return typeof(NpgsqlTsQuery); // untested
            case "tsvector":
               return typeof(NpgsqlTsVector); // untested
            case "oid":
            case "xid":
            case "cid":
               return nullable ? typeof(uint?) : typeof(uint); // untested
            case "oidvector":
               return typeof(uint[]); // untested
            case "geometry":
               return null; // unsupported
            case "record":
               return typeof(object[]); // untested
            case "range":
               return null; // unsupported
            case "array":
               if (string.IsNullOrWhiteSpace(udtName))
                  return null;

               if (udtName.StartsWith("_"))
               {
                  udtName = udtName.Substring(1);
               }

               if ("array".Equals(udtName, StringComparison.InvariantCultureIgnoreCase))
                  return null;

               var type = MapDbTypeToType(udtName, null, false, useExperimentalTypes);
               if (type == null)
                  return null;

               return type.MakeArrayType(); // untested
         }

         return null; // unsupported type
      }
   }
}
