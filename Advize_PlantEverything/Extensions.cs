using System;
using System.Collections.Generic;
using System.Reflection;

namespace Advize_PlantEverything
{
    internal static class CloneManager
    {
        /// <summary>
        ///     Extension for 'Object' that copies all fields from the source to the calling object.
        ///     Including private and static fields.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        public static void CopyFields<T>(this T target, T source)
        {
            // If any this null throw an exception
            if (target == null || source == null)
            {
                throw new Exception("Target or/and Source Objects are null");
            }

            // Iterate over the fields of type T and copy them from the source instance to the target instance
            FieldInfo[] fieldInfos = FieldInfoCache.GetFieldInfo(source.GetType());
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                if (fieldInfo == null || !fieldInfo.IsInitOnly)
                {
                    continue;
                }

                // Passed all tests, lets set the value
                fieldInfo.SetValue(target, fieldInfo.GetValue(source));
            }
        }

        private static class FieldInfoCache
        {
            private const BindingFlags AllBindings = BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.Instance | BindingFlags.Static |
                                                    BindingFlags.GetField | BindingFlags.SetField |
                                                    BindingFlags.GetProperty | BindingFlags.SetProperty;

            private static readonly Dictionary<Type, FieldInfo[]> dictionaryCache = new();

            /// <summary>
            ///     Get FieldInfo array from Cache if it exists or cache the FieldInfo array and return it.
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            internal static FieldInfo[] GetFieldInfo(Type type)
            {
                if (dictionaryCache.TryGetValue(type, out var fieldInfos))
                {
                    return fieldInfos;
                }
                else
                {
                    CacheTypeFieldInfo(type);
                }
                return dictionaryCache[type];
            }

            /// <summary>
            ///     Create the FieldInfo array and cache it if it is not already cached.
            /// </summary>
            /// <param name="type"></param>
            internal static void CacheTypeFieldInfo(Type type)
            {
                if (!dictionaryCache.ContainsKey(type))
                {
                    dictionaryCache.Add(type, type.GetFields(AllBindings));
                }
            }
        }
    }
}