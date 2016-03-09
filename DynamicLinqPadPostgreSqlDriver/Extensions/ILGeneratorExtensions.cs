using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using LinqToDB.Data;

namespace DynamicLinqPadPostgreSqlDriver.Extensions
{
   internal static class ILGeneratorExtensions
   {
      public static void EmitBodyBeginForGetTable(this ILGenerator ilgen, FunctionData func)
      {
         ilgen.Emit(OpCodes.Ldarg_0);
         ilgen.Emit(OpCodes.Ldarg_0);

         var getCurrentMethod = typeof(MethodBase).GetMethod("GetCurrentMethod", BindingFlags.Static | BindingFlags.Public);
         ilgen.Emit(OpCodes.Call, getCurrentMethod);
         ilgen.Emit(OpCodes.Castclass, typeof(MethodInfo));

         ilgen.Emit(OpCodes.Ldc_I4, func.ArgumentCount);
         ilgen.Emit(OpCodes.Newarr, typeof(object));
      }

      public static void EmitParameterBodyForGetTable(this ILGenerator ilgen, int i, Type mappedArgType)
      {
         ilgen.Emit(OpCodes.Dup);
         ilgen.Emit(OpCodes.Ldc_I4, i);
         ilgen.Emit(OpCodes.Ldarg, i + 1);

         if (mappedArgType.IsPrimitive && mappedArgType.IsValueType)
         {
            ilgen.Emit(OpCodes.Box, mappedArgType);
         }

         ilgen.Emit(OpCodes.Stelem_Ref);
      }

      public static void EmitBodyEndForGetTable(this ILGenerator ilgen, Type mappedReturnType)
      {
         var getTableMethod = typeof(DataConnection).GetMethod("GetTable", new Type[] { typeof(object), typeof(MethodInfo), typeof(object[]) }).MakeGenericMethod(mappedReturnType);
         ilgen.Emit(OpCodes.Call, getTableMethod);

         ilgen.Emit(OpCodes.Ret);
      }


      public static void EmitBodyBeginForGetEnumerable(this ILGenerator ilgen, FunctionData func, TypeInfo funcReturnTypeInfo)
      {
         ilgen.Emit(OpCodes.Ldarg_0);
         ilgen.Emit(OpCodes.Ldarg_0);

         var getMapperMethod = typeof(TypedDataContextBase).GetMethod("GetMapper", BindingFlags.NonPublic | BindingFlags.Instance)
            .MakeGenericMethod(funcReturnTypeInfo.ElementType);
         ilgen.Emit(OpCodes.Call, getMapperMethod);

         ilgen.Emit(OpCodes.Ldstr, func.Name);

         ilgen.Emit(OpCodes.Ldc_I4, func.ArgumentCount); // Array length
         ilgen.Emit(OpCodes.Newarr, typeof(DataParameter));
      }

      public static void EmitParameterBodyForGetEnumerable(this ILGenerator ilgen, int i, string argName, Type mappedArgType)
      {
         ilgen.Emit(OpCodes.Dup);
         ilgen.Emit(OpCodes.Ldc_I4, i); // Array index

         ilgen.Emit(OpCodes.Ldstr, argName);
         ilgen.Emit(OpCodes.Ldarg, i+1);

         if (mappedArgType.IsPrimitive && mappedArgType.IsValueType)
         {
            ilgen.Emit(OpCodes.Box, mappedArgType);
         }

         var dataParameterConstructor = typeof(DataParameter).GetConstructor(new Type[] { typeof(string), typeof(object) });
         ilgen.Emit(OpCodes.Newobj, dataParameterConstructor);

         ilgen.Emit(OpCodes.Stelem_Ref); // Set created object to array index
      }

      public static void EmitBodyEndForGetEnumerable(this ILGenerator ilgen, TypeInfo funcReturnTypeInfo)
      {
         var queryProcMethod = typeof(DataConnectionExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Single(x => x.Name == "QueryProc" && x.GetParameters().Length == 4)
            .MakeGenericMethod(funcReturnTypeInfo.ElementType);
         ilgen.Emit(OpCodes.Call, queryProcMethod);

         ilgen.Emit(OpCodes.Ret);
      }
   }
}
