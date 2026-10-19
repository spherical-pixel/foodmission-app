using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Newtonsoft.Json;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Builds objects whose every public field holds a non-default value, so a test fails as soon as a new
    /// field is not copied, not persisted or not handled, without listing the fields by hand.
    /// </summary>
    internal static class StateSamples
    {
        private const int MaxDepth = 5;

        public static T Populated<T>() where T : new()
        {
            var value = new T();
            Populate(value, typeof(T).Name, 0);
            return value;
        }

        public static object Sample(Type type, string seed, int depth = 0)
        {
            if (type == typeof(string))
            {
                return seed;
            }
            if (type == typeof(bool))
            {
                return true;
            }
            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            {
                return Convert.ChangeType(Math.Abs(seed.GetHashCode() % 997) + 3, type);
            }
            if (type == typeof(float) || type == typeof(double))
            {
                return Convert.ChangeType(Math.Abs(seed.GetHashCode() % 997) + 0.5, type);
            }
            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                return values.GetValue(values.Length - 1);
            }
            if (type.IsArray)
            {
                Array array = Array.CreateInstance(type.GetElementType(), 1);
                array.SetValue(Sample(type.GetElementType(), seed + "[0]", depth + 1), 0);
                return array;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = (IList)Activator.CreateInstance(type);
                list.Add(Sample(type.GetGenericArguments()[0], seed + "[0]", depth + 1));
                return list;
            }
            if (depth >= MaxDepth || type.GetConstructor(Type.EmptyTypes) == null)
            {
                return null;
            }
            object instance = Activator.CreateInstance(type);
            Populate(instance, seed, depth + 1);
            return instance;
        }

        private static void Populate(object instance, string seed, int depth)
        {
            foreach (FieldInfo field in instance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsInitOnly)
                {
                    continue;
                }
                object value = Sample(field.FieldType, seed + "." + field.Name, depth);
                if (field.FieldType == typeof(bool))
                {
                    // Flip whatever the default is, so a field left at its default always shows up
                    value = !(bool)field.GetValue(instance);
                }
                field.SetValue(instance, value);
            }
        }

        /// <summary>Same serializer the app uses to persist state (LocalStorageService).</summary>
        public static string Json(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }

        /// <summary>Paths of every mutable object (class instance or array) reachable from both graphs through the same field.</summary>
        public static List<string> SharedMutableObjects(object a, object b, string path = "")
        {
            var shared = new List<string>();
            if (a == null || b == null || a is string || a.GetType().IsValueType)
            {
                return shared;
            }
            if (ReferenceEquals(a, b))
            {
                shared.Add(string.IsNullOrEmpty(path) ? "(root)" : path);
                return shared;
            }
            if (a is IList listA && b is IList listB)
            {
                for (int i = 0; i < Math.Min(listA.Count, listB.Count); i++)
                {
                    shared.AddRange(SharedMutableObjects(listA[i], listB[i], $"{path}[{i}]"));
                }
                return shared;
            }
            foreach (FieldInfo field in a.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                shared.AddRange(SharedMutableObjects(field.GetValue(a), field.GetValue(b), $"{path}.{field.Name}".TrimStart('.')));
            }
            return shared;
        }
    }
}
