﻿// Needed for NET40

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Theraot.Core
{
    [DebuggerNonUserCode]
    public static partial class TypeHelper
    {
        public static bool CanBe<T>(this Type type, T value)
        {
            if (ReferenceEquals(value, null))
            {
                return type.CanBeNull();
            }
            return value.GetType().IsAssignableTo(type);
        }

        public static TAttribute[] GetAttributes<TAttribute>(this ICustomAttributeProvider item, bool inherit)
            where TAttribute : Attribute
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            return (TAttribute[])item.GetCustomAttributes(typeof(TAttribute), inherit);
        }

        public static MethodInfo GetDelegateMethodInfo(Type delegateType)
        {
            if (delegateType == null)
            {
                throw new ArgumentNullException(nameof(delegateType));
            }
            var delegateTypeInfo = delegateType.GetTypeInfo();
            if (delegateTypeInfo.BaseType != typeof(MulticastDelegate))
            {
                throw new ArgumentException("Not a delegate.");
            }
            var methodInfo = delegateType.GetMethod("Invoke");
            if (methodInfo == null)
            {
                throw new ArgumentException("Not a delegate.");
            }
            return methodInfo;
        }

        public static ParameterInfo[] GetDelegateParameters(Type delegateType)
        {
            return GetDelegateMethodInfo(delegateType).GetParameters();
        }

        public static Type GetDelegateReturnType(Type delegateType)
        {
            return GetDelegateMethodInfo(delegateType).ReturnType;
        }

        public static Type GetNonRefType(this ParameterInfo parameterInfo)
        {
            var parameterType = parameterInfo.ParameterType;
            if (parameterType.IsByRef)
            {
                parameterType = parameterType.GetElementType();
            }
            return parameterType;
        }

        public static bool HasAttribute<TAttribute>(this ICustomAttributeProvider item)
            where TAttribute : Attribute
        {
            var attributes = item.GetAttributes<TAttribute>(true);
            if (attributes != null)
            {
                return attributes.Length > 0;
            }
            return false;
        }

        public static bool HasConstructor(this Type type, params Type[] typeArguments)
        {
            var constructorInfo = type.GetConstructor(typeArguments);
            return constructorInfo == null;
        }

        public static bool IsArrayTypeAssignableTo(Type type, Type target)
        {
            if (!type.IsArray || !target.IsArray)
            {
                return false;
            }
            if (type.GetArrayRank() != target.GetArrayRank())
            {
                return false;
            }
            return type.GetElementType().IsAssignableTo(target.GetElementType());
        }

        public static bool IsArrayTypeAssignableToInterface(Type type, Type target)
        {
            if (!type.IsArray)
            {
                return false;
            }
            return
                (
                    target.IsGenericInstanceOf(typeof(IList<>))
                    || target.IsGenericInstanceOf(typeof(ICollection<>))
                    || target.IsGenericInstanceOf(typeof(IEnumerable<>))
                )
                && type.GetElementType() == target.GetGenericArguments()[0];
        }

        public static bool IsAssignableTo(this Type type, Type target)
        {
            return target.IsAssignableFrom(type)
                || IsArrayTypeAssignableTo(type, target)
                || IsArrayTypeAssignableToInterface(type, target);
        }

        public static bool IsAssignableTo(this Type type, ParameterInfo parameterInfo)
        {
            return IsAssignableTo(GetNotNullableType(type), parameterInfo.GetNonRefType());
        }

        public static bool IsBinaryPortable(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            return IsBinaryPortableExtracted(type);
        }

        public static bool IsBlittable(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            return IsBlittableExtracted(type);
        }

        public static bool IsGenericImplementationOf(this Type type, Type interfaceGenericTypeDefinition)
        {
            foreach (var currentInterface in type.GetInterfaces())
            {
                if (currentInterface.IsGenericInstanceOf(interfaceGenericTypeDefinition))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsGenericImplementationOf(this Type type, params Type[] interfaceGenericTypeDefinitions)
        {
            foreach (var currentInterface in type.GetInterfaces())
            {
                var info = currentInterface.GetTypeInfo();
                if (info.IsGenericTypeDefinition)
                {
                    var match = currentInterface.GetGenericTypeDefinition();
                    if (Array.Exists(interfaceGenericTypeDefinitions, item => item == match))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsGenericImplementationOf(this Type type, out Type interfaceType, Type interfaceGenericTypeDefinition)
        {
            foreach (var currentInterface in type.GetInterfaces())
            {
                if (currentInterface.IsGenericInstanceOf(interfaceGenericTypeDefinition))
                {
                    interfaceType = currentInterface;
                    return true;
                }
            }
            interfaceType = null;
            return false;
        }

        public static bool IsGenericImplementationOf(this Type type, out Type interfaceType, params Type[] interfaceGenericTypeDefinitions)
        {
            var implementedInterfaces = type.GetInterfaces();
            foreach (var currentInterface in interfaceGenericTypeDefinitions)
            {
                var index = Array.FindIndex(implementedInterfaces, item => item.IsGenericInstanceOf(currentInterface));
                if (index != -1)
                {
                    interfaceType = implementedInterfaces[index];
                    return true;
                }
            }
            interfaceType = null;
            return false;
        }

        public static bool IsImplementationOf(this Type type, Type interfaceType)
        {
            foreach (var currentInterface in type.GetInterfaces())
            {
                if (currentInterface == interfaceType)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsImplementationOf(this Type type, params Type[] interfaceTypes)
        {
            foreach (var currentInterface in type.GetInterfaces())
            {
                if (Array.Exists(interfaceTypes, item => currentInterface == item))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsImplementationOf(this Type type, out Type interfaceType, params Type[] interfaceTypes)
        {
            var implementedInterfaces = type.GetInterfaces();
            foreach (var currentInterface in interfaceTypes)
            {
                var index = Array.FindIndex(implementedInterfaces, item => item == currentInterface);
                if (index != -1)
                {
                    interfaceType = implementedInterfaces[index];
                    return true;
                }
            }
            interfaceType = null;
            return false;
        }

        public static bool IsValueTypeRecursive(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            return IsValueTypeRecursiveExtracted(type);
        }

        internal static bool CanCache(this Type type)
        {
            var info = type.GetTypeInfo();
            var assembly = info.Assembly;
            if (Array.IndexOf(_knownAssemblies, assembly) == -1)
            {
                return false;
            }
            if (info.IsGenericType)
            {
                foreach (var genericArgument in type.GetGenericArguments())
                {
                    if (!CanCache(genericArgument))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool GetBinaryPortableResult(Type type)
        {
            var info = type.GetTypeInfo();
            if (info.IsPrimitive)
            {
                return type != typeof(IntPtr)
                    && type != typeof(UIntPtr)
                    && type != typeof(char)
                    && type != typeof(bool);
            }
            if (info.IsValueType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!IsBinaryPortableExtracted(field.FieldType))
                    {
                        return false;
                    }
                }
                // ReSharper disable once PossibleNullReferenceException
                return !info.IsAutoLayout && info.StructLayoutAttribute.Pack > 0;
            }
            return false;
        }

        private static bool GetBlittableResult(Type type)
        {
            var info = type.GetTypeInfo();
            if (info.IsPrimitive)
            {
                if
                (
                    type == typeof(char)
                    || type == typeof(bool)
                )
                {
                    return false;
                }
                return true;
            }
            if (info.IsValueType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!IsBlittableExtracted(field.FieldType))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static bool GetValueTypeRecursiveResult(Type type)
        {
            var info = type.GetTypeInfo();
            if (info.IsPrimitive)
            {
                return true;
            }
            if (info.IsValueType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!IsValueTypeRecursiveExtracted(field.FieldType))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static bool IsBinaryPortableExtracted(Type type)
        {
            var info = type.GetTypeInfo();
            if (!info.IsValueType)
            {
                return false;
            }
            if (CanCache(type))
            {
                var property = typeof(BinaryPortableInfo<>).MakeGenericType(type).GetProperty("Result", BindingFlags.Public | BindingFlags.Static);
                // ReSharper disable once PossibleNullReferenceException
                return (bool)property.GetValue(null, null);
            }
            return GetBinaryPortableResult(type);
        }

        private static bool IsBlittableExtracted(Type type)
        {
            var info = type.GetTypeInfo();
            if (!info.IsValueType)
            {
                return false;
            }
            if (CanCache(type))
            {
                var property = typeof(BlittableInfo<>).MakeGenericType(type).GetProperty("Result", BindingFlags.Public | BindingFlags.Static);
                // ReSharper disable once PossibleNullReferenceException
                return (bool)property.GetValue(null, null);
            }
            return GetBlittableResult(type);
        }

        private static bool IsValueTypeRecursiveExtracted(Type type)
        {
            var info = type.GetTypeInfo();
            if (!info.IsValueType)
            {
                return false;
            }
            if (CanCache(type))
            {
                var property = typeof(ValueTypeRecursiveInfo<>).MakeGenericType(type).GetProperty("Result", BindingFlags.Public | BindingFlags.Static);
                // ReSharper disable once PossibleNullReferenceException
                return (bool)property.GetValue(null, null);
            }
            return GetValueTypeRecursiveResult(type);
        }

        private static class BinaryPortableInfo<T>
        {
            static BinaryPortableInfo()
            {
                Result = GetBinaryPortableResult(typeof(T));
            }

            // Used via reflection
            // ReSharper disable once StaticMemberInGenericType
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once MemberCanBePrivate.Local
            public static bool Result { get; }
        }

        private static class BlittableInfo<T>
        {
            static BlittableInfo()
            {
                Result = GetBlittableResult(typeof(T));
            }

            // Used via reflection
            // ReSharper disable once StaticMemberInGenericType
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once MemberCanBePrivate.Local
            public static bool Result { get; }
        }

        private static class ValueTypeRecursiveInfo<T>
        {
            static ValueTypeRecursiveInfo()
            {
                Result = GetValueTypeRecursiveResult(typeof(T));
            }

            // Used via reflection
            // ReSharper disable once StaticMemberInGenericType
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            // ReSharper disable once MemberCanBePrivate.Local
            public static bool Result { get; }
        }
    }

#if NET20 || NET30 || NET35 || NET40

    public static partial class TypeHelper
    {
        public static object GetValue(this PropertyInfo info, object obj)
        {
            //Added in .NET 4.5
            return info.GetValue(obj, null);
        }

        public static void SetValue(this PropertyInfo info, object obj, object value)
        {
            //Added in .NET 4.5
            info.SetValue(obj, value, null);
        }
    }

#endif
}