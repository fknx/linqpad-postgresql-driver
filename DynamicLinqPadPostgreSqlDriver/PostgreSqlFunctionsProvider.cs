using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Dapper;
using DynamicLinqPadPostgreSqlDriver.Extensions;
using LinqToDB;
using LinqToDB.Data;
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

         var functionData = connection.Query<FunctionData>(sql).ToList();

         var functionExplorerItems = new List<ExplorerItem>();
         foreach (var func in functionData.OrderBy(x => x.Name))
         {
            Dictionary<int, string> argumentTypes;

            if (func.ArgumentTypeOids.Any())
            {
               argumentTypes = connection.Query(SqlHelper.LoadSql("QueryTypeByOid.sql").Replace("@oids", string.Join(",", func.ArgumentTypeOids)))
                  .ToDictionary(x => (int)x.oid, x => (string)x.typname);
            }
            else
            {
               argumentTypes = new Dictionary<int, string>();
            }

            var funcType = func.IsMultiValueReturn ? ExplorerIcon.TableFunction : ExplorerIcon.ScalarFunction;
            
            var funcReturnTypeInfo = new FunctionReturnTypeInfo();

            var mappedReturnType = SqlHelper.MapDbTypeToType(func.ReturnType, null, false, false);

            if (mappedReturnType != null)
            {
               funcReturnTypeInfo.ElementType = mappedReturnType;
               
               if (func.IsMultiValueReturn)
               {
                  funcReturnTypeInfo.CollectionType = typeof(IEnumerable<>).MakeGenericType(mappedReturnType);
               }
            }
            
            if (mappedReturnType == null)
            {
               var userTypeName = cxInfo.GetTypeName(func.ReturnType);
               mappedReturnType = moduleBuilder.GetType($"{nameSpace}.{userTypeName}");

               if (mappedReturnType != null)
               {
                  funcReturnTypeInfo.ExistsAsTable = true;
                  funcReturnTypeInfo.ElementType = mappedReturnType;
                  funcReturnTypeInfo.CollectionType = typeof(ITable<>).MakeGenericType(mappedReturnType);
               }
            }
            
            if (mappedReturnType == null)
            {
               mappedReturnType = typeof (ExpandoObject);
               funcReturnTypeInfo.ElementType = mappedReturnType;
               funcReturnTypeInfo.CollectionType = typeof (IEnumerable<>).MakeGenericType(mappedReturnType);
            }
            
            var methodBuilder = dataContextTypeBuilder.DefineMethod(func.Name, MethodAttributes.Public);
            
            methodBuilder.SetReturnType(funcReturnTypeInfo.CollectionType?? funcReturnTypeInfo.ElementType);

            var ilgen = methodBuilder.GetILGenerator();

            if (funcReturnTypeInfo.ExistsAsTable)
            {
               ilgen.EmitBodyBeginForGetTable(func);
            }
            else
            {
               ilgen.EmitBodyBeginForGetEnumerable(func, funcReturnTypeInfo);
            }

            var paramTypes = new List<Tuple<int, string, Type>>();
            var paramExplorerItems = new List<ExplorerItem>();
            for (int i = 0; i < func.ArgumentCount; i++)
            {
               var argName = func.ArgumentNames?[i];
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
                  bool isArray = argType.StartsWith("_");
                  var pgTypeName = argType.TrimStart('_');
                  var udtAttributes = connection.Query(SqlHelper.LoadSql("QueryUdtAttributes.sql"), new { typname = pgTypeName }).ToList();
                  if (udtAttributes.Any())
                  {
                     var typeName = $"{nameSpace}.{cxInfo.GetTypeName(pgTypeName)}";
                     var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

                     foreach (var attr in udtAttributes)
                     {
                        var attrName = cxInfo.GetColumnName((string)attr.AttributeName);
                        var attrType = SqlHelper.MapDbTypeToType(attr.AttributeType, null, false, true);
                        if (attrType == null)
                        {
                           throw new InvalidOperationException("Unknown type: " + attr.AttributeType);
                        }

                        typeBuilder.DefineField(attrName, attrType, FieldAttributes.Public);
                     }

                     var udt = typeBuilder.CreateType();

                     if (isArray)
                     {
                        mappedArgType = udt.MakeArrayType();
                     }
                     else
                     {
                        mappedArgType = typeBuilder.CreateType();
                     }
                  }
               }

               paramTypes.Add(new Tuple<int, string, Type>(i, argName, mappedArgType));

               var itemText = $"{argName} ({mappedArgType?.Name ?? $"unknown type: {argType}"})";

               paramExplorerItems.Add(new ExplorerItem(itemText, ExplorerItemKind.Parameter, ExplorerIcon.Parameter));
            }

            methodBuilder.SetParameters(paramTypes.Select(x => x.Item3).Where(x => x != null).ToArray());

            foreach (var paramType in paramTypes)
            {
               var i = paramType.Item1;
               var argName = paramType.Item2?? $"param{i}";
               var mappedArgType = paramType.Item3;
               if (mappedArgType == null)
               {
                  continue;
               }

               methodBuilder.DefineParameter(i + 1, ParameterAttributes.In, argName);

               if (funcReturnTypeInfo.ExistsAsTable)
               {
                  ilgen.EmitParameterBodyForGetTable(i, mappedArgType);
               }
               else
               {
                  ilgen.EmitParameterBodyForGetEnumerable(i, argName, mappedArgType);
               }
            }

            if (funcReturnTypeInfo.ExistsAsTable)
            {
               ilgen.EmitBodyEndForGetTable(mappedReturnType);

               var sqlFunctionAttributeConstructor = typeof (Sql.TableFunctionAttribute).GetConstructor(new Type[] {typeof (string)});
               methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(sqlFunctionAttributeConstructor, new object[] {func.Name}));
            }
            else
            {
               ilgen.EmitBodyEndForGetEnumerable(funcReturnTypeInfo);
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
}
