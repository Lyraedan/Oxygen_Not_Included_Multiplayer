using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Helpers
{
	public static class ReflectionHelper
	{
		public static bool TryGetType(string typeName, out Type type)
		{
			type = Type.GetType(typeName);
			if (type == null)
				Debug.LogWarning($"[ReflectionHelper] Type '{typeName}' not found.");
			return type != null;
		}
		public static bool TryGetMethodInfo(string typeName, string methodName, Type[] parameters, out System.Reflection.MethodInfo methodInfo, ArgumentType[]? argumentTypes = null)
		{
			methodInfo = null;
			if (!TryGetType(typeName, out Type type))
				return false;

            if (argumentTypes == null || argumentTypes.Length == 0)
                methodInfo = AccessTools.Method(type, methodName, parameters);
            else
                methodInfo = GetMethodWithArgumentTypes(type, methodName, parameters, argumentTypes);

            if (methodInfo == null)
                Debug.LogWarning($"[ReflectionHelper] method '{methodName}' not found on type {type}");

			return methodInfo != null;
		}
		public static bool TryGetFieldInfo(string typeName, string fieldName, out System.Reflection.FieldInfo fieldInfo)
		{
			fieldInfo = null;
			if (!TryGetType(typeName, out Type type))
				return false;
			fieldInfo = AccessTools.Field(type, fieldName);

			if (fieldInfo == null)
				Debug.LogWarning($"[ReflectionHelper] field '{fieldName}' not found on type {type}");

			return fieldInfo != null;
		}
		public static bool TryGetPropertyGetter(string typeName, string propertyName, out System.Reflection.MethodInfo getter)
		{
			getter = null;
			if (!TryGetType(typeName, out Type type))
				return false;
			getter = AccessTools.PropertyGetter(type, propertyName);

			if (getter == null)
				Debug.LogWarning($"[ReflectionHelper] getter for '{propertyName}' not found on type {type}");

			return getter != null;
		}
		public static bool TryCreateDelegate<T>(string typeName, string methodName, Type[] parameters, out T del, ArgumentType[]? argumentType = null) where T : Delegate
		{
			del = null;
			if (!TryGetMethodInfo(typeName, methodName, parameters, out var methodInfo, argumentType))
				return false;
			del = (T)Delegate.CreateDelegate(typeof(T), methodInfo);
			return del != null;
		}

        public static MethodInfo? GetMethodWithArgumentTypes(Type type, string methodName, Type[] parameterTypes, ArgumentType[] argumentTypes)
        {
            var method = AccessTools.Method(type, methodName, parameterTypes);
            var parameters = method.GetParameters();
            bool match = true;

            if(parameters == null || parameters.Length == 0)
            {
                // Somethings gone wrong or no parameters
                return null;
            }

            for(int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramType = param.ParameterType;
                
                // Strip modifiers to get the base type
                Type baseType =
                    paramType.IsByRef ? paramType.GetElementType() :
                    paramType.IsPointer ? paramType.GetElementType() :
                    paramType;

                if (baseType != parameterTypes[i])
                {
                    match = false;
                    break;
                }

                // ArgumentType list should match the parameter list defining the argument type per parameter
                switch (argumentTypes[i])
                {
                    case ArgumentType.Normal:
                        if (paramType.IsByRef || paramType.IsPointer)
                            match = false;
                        break;

                    case ArgumentType.Ref:
                        if (!paramType.IsByRef || param.IsOut)
                            match = false;
                        break;

                    case ArgumentType.Out:
                        if (!param.IsOut)
                            match = false;
                        break;

                    case ArgumentType.Pointer:
                        if (!paramType.IsPointer)
                            match = false;
                        break;
                }

                if (!match)
                    break;
            }

            if (match)
                return method;

            return null;
        }
    }
}
