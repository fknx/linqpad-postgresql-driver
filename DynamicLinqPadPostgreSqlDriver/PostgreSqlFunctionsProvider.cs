using System;
using System.Collections.Generic;
using System.Data;
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
            var argumentTypes = connection.Query(SqlHelper.LoadSql("QueryTypeByOid.sql").Replace("@oids", string.Join(",", func.ArgumentTypeOids)))
               .ToDictionary(x => (int)x.oid, x => (string)x.typname);

            var funcType = func.IsMultiValueReturn ? ExplorerIcon.TableFunction : ExplorerIcon.ScalarFunction;

            var mappedReturnType = SqlHelper.MapDbTypeToType(func.ReturnType, null, false, false);
            if (mappedReturnType != null
                && func.IsMultiValueReturn)
            {
               mappedReturnType = mappedReturnType.MakeArrayType();
            }
            if (mappedReturnType == null)
            {
               var userTypeName = cxInfo.GetTypeName(func.ReturnType);
               mappedReturnType = moduleBuilder.GetType($"{nameSpace}.{userTypeName}");
            }
            if (mappedReturnType == null)
            {
               mappedReturnType = typeof (object);
            }

            // Not supported by GetTable
            if (mappedReturnType.IsValueType)
            {
               continue;
            }

            var methodBuilder = dataContextTypeBuilder.DefineMethod(func.Name, MethodAttributes.Public);

            var tableOfReturnType = typeof (ITable<>).MakeGenericType(mappedReturnType);

            methodBuilder.SetReturnType(tableOfReturnType);

            var ilgen = methodBuilder.GetILGenerator();

            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Ldarg_0);

            var getCurrentMethod = typeof (MethodBase).GetMethod("GetCurrentMethod", BindingFlags.Static | BindingFlags.Public);
            ilgen.Emit(OpCodes.Call, getCurrentMethod);
            ilgen.Emit(OpCodes.Castclass, typeof (MethodInfo));

            ilgen.Emit(OpCodes.Ldc_I4, func.ArgumentCount);
            ilgen.Emit(OpCodes.Newarr, typeof (object));

            var paramTypes = new List<Tuple<int, string, Type>>();
            var paramExplorerItems = new List<ExplorerItem>();
            for (int i = 0; i < func.ArgumentCount; i++)
            {
               var argName = func.ArgumentNames[i];
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

               paramTypes.Add(new Tuple<int, string, Type>(i, argName, mappedArgType));

               var itemText = $"{argName} ({mappedArgType?.Name ?? $"unknown type: {argType}"})";

               paramExplorerItems.Add(new ExplorerItem(itemText, ExplorerItemKind.Parameter, ExplorerIcon.Parameter));
            }

            methodBuilder.SetParameters(paramTypes.Select(x => x.Item3).Where(x => x != null).ToArray());

            foreach (var paramType in paramTypes)
            {
               var i = paramType.Item1;
               var argName = paramType.Item2;
               var mappedArgType = paramType.Item3;
               if (mappedArgType == null)
               {
                  continue;
               }

               methodBuilder.DefineParameter(i + 1, ParameterAttributes.In, argName);

               ilgen.Emit(OpCodes.Dup);
               ilgen.Emit(OpCodes.Ldc_I4, i);
               ilgen.Emit(OpCodes.Ldarg, i + 1);

               if (mappedArgType.IsPrimitive
                   && mappedArgType.IsValueType)
               {
                  ilgen.Emit(OpCodes.Box, mappedArgType);
               }

               ilgen.Emit(OpCodes.Stelem_Ref);
            }

            var getTableMethod = typeof (DataConnection).GetMethod("GetTable", new Type[] {typeof (object), typeof (MethodInfo), typeof (object[])}).MakeGenericMethod(mappedReturnType);
            ilgen.Emit(OpCodes.Call, getTableMethod);

            ilgen.Emit(OpCodes.Ret);

            var explorerItem = new ExplorerItem(func.Name, ExplorerItemKind.QueryableObject, funcType)
            {
               IsEnumerable = true,
               Children = paramExplorerItems
            };

            functionExplorerItems.Add(explorerItem);

            var sqlFunctionAttributeConstructor = typeof (Sql.TableFunctionAttribute).GetConstructor(new Type[] {typeof (string)});
            methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(sqlFunctionAttributeConstructor, new object[]{ func.Name }));
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
      public string Name { get; set; }
      public string ReturnType { get; set; }
      public int ArgumentCount { get; set; }
      public string[] ArgumentNames { get; set; }
      public int[] ArgumentTypeOids { get; set; }
      public object[] ArgumentDefaults { get; set; }
      public bool IsMultiValueReturn { get; set; }
   }
}
