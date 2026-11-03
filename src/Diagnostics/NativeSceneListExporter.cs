using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ScheduleIMegaExpansion.Diagnostics
{
    /// <summary>
    /// Exports loaded Unity scene names and top-level hierarchy roots so a MapExpansions
    /// WorldSource can be configured against a real scene and root, not a guessed name.
    /// This is read-only and does not load additive scenes or alter the active scene.
    /// </summary>
    internal static class NativeSceneListExporter
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static int Export(string outputDirectory, List<string> summary, Action<string> warn)
        {
            var rows = new List<string[]>();
            try
            {
                Type? sceneManager = FindType("UnityEngine.SceneManagement.SceneManager");
                if (sceneManager == null)
                    throw new TypeLoadException("UnityEngine.SceneManagement.SceneManager was not found.");

                int count = Convert.ToInt32(sceneManager.GetProperty("sceneCount", PublicStatic)?.GetValue(null, null),
                    CultureInfo.InvariantCulture);
                object? active = sceneManager.GetMethod("GetActiveScene", PublicStatic)?.Invoke(null, null);
                string activeName = ReadText(active, "name");

                MethodInfo? getAt = sceneManager.GetMethods(PublicStatic)
                    .FirstOrDefault(method => method.Name == "GetSceneAt"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == typeof(int));
                if (getAt == null)
                    throw new MissingMethodException(sceneManager.FullName, "GetSceneAt(int)");

                int rootCount = 0;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        object? scene = getAt.Invoke(null, new object[] { i });
                        if (scene == null)
                            continue;

                        string name = ReadText(scene, "name");
                        bool isLoaded = ReadBool(scene, "isLoaded");
                        MethodInfo? getRoots = scene.GetType().GetMethods(PublicInstance)
                            .FirstOrDefault(method => method.Name == "GetRootGameObjects"
                                && method.GetParameters().Length == 0);
                        object? rawRoots = getRoots?.Invoke(scene, null);
                        List<object> roots = ToObjects(rawRoots);
                        rootCount += roots.Count;

                        rows.Add(new[]
                        {
                            i.ToString(CultureInfo.InvariantCulture),
                            name,
                            isLoaded.ToString().ToLowerInvariant(),
                            (name == activeName).ToString().ToLowerInvariant(),
                            roots.Count.ToString(CultureInfo.InvariantCulture),
                            string.Join(" | ", roots.Select(root => ReadText(root, "name")))
                        });

                        foreach (object root in roots)
                        {
                            object? transform = ReadMember(root, "transform");
                            rows.Add(new[]
                            {
                                i.ToString(CultureInfo.InvariantCulture),
                                name + "/" + ReadText(root, "name"),
                                isLoaded.ToString().ToLowerInvariant(),
                                (name == activeName).ToString().ToLowerInvariant(),
                                transform == null ? "" : ReadText(transform, "childCount"),
                                transform == null ? "" : ReadText(transform, "position")
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        warn("Scene index " + i + " could not be exported: " + ex.Message);
                    }
                }

                WriteCsv(outputDirectory, rows);
                summary.Add("NativeLoadedScenes=" + count.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeSceneRoots=" + rootCount.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeActiveScene=" + activeName);
                summary.Add("NativeSceneListMode=READ_ONLY; no additive scene loads or scene changes");
                return count;
            }
            catch (Exception ex)
            {
                WriteCsv(outputDirectory, rows);
                summary.Add("NativeSceneList=ERROR: " + ex.GetType().Name + ": " + ex.Message);
                warn("Native scene list export failed: " + ex.Message);
                return 0;
            }
        }

        private static bool ReadBool(object target, string name)
        {
            try { return Convert.ToBoolean(ReadMember(target, name), CultureInfo.InvariantCulture); }
            catch { return false; }
        }

        private static string ReadText(object? target, string name)
        {
            if (target == null) return "";
            object? value = ReadMember(target, name);
            return value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        private static List<object> ToObjects(object? value)
        {
            var result = new List<object>();
            if (value is System.Collections.IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                    if (item != null)
                        result.Add(item);
            }
            return result;
        }

        private static object? ReadMember(object target, string name)
        {
            PropertyInfo? property = target.GetType().GetProperty(name, AllInstance);
            if (property?.CanRead == true)
            {
                try { return property.GetValue(target, null); }
                catch { }
            }
            FieldInfo? field = target.GetType().GetField(name, AllInstance);
            if (field != null)
            {
                try { return field.GetValue(target); }
                catch { }
            }
            return null;
        }

        private static Type? FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type? type = assembly.GetType(fullName, false);
                    if (type != null) return type;
                }
                catch { }
            }
            return null;
        }

        private static void WriteCsv(string directory, IEnumerable<string[]> rows)
        {
            using var writer = new StreamWriter(Path.Combine(directory, "native-scenes.csv"), false);
            writer.WriteLine("sceneIndex,sceneOrRootPath,isLoaded,isActive,rootCountOrChildCount,rootsOrPosition");
            foreach (string[] row in rows)
                writer.WriteLine(string.Join(",", row.Select(EscapeCsv)));
        }

        private static string EscapeCsv(string? value)
        {
            value ??= "";
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;
            string quote = new string((char)34, 1);
            string escaped = new string((char)34, 2);
            return quote + value.Replace(quote, escaped) + quote;
        }
    }
}
