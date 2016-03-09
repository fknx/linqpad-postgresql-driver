using System;
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

         if (mappedArgType.IsPrimitive
             && mappedArgType.IsValueType)
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
   }
}
