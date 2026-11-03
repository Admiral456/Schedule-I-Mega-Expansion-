using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ScheduleIMegaExpansion.Diagnostics
{
    /// <summary>
    /// Looks up selected vanilla Schedule I scene objects by their hierarchy path.
    /// Paths are research leads from the user's Daffy's Hills reference DLL and
    /// are checked against the currently loaded game scene. This is read-only:
    /// it never clones, moves, disables or otherwise changes a native object.
    /// </summary>
    internal static class NativeScenePrefabExporter
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly string[] CandidatePaths =
        {
            "Map/Hyland Point/Region_Docks/Clothing store with interior",
            "Map/Hyland Point/Region_Suburbia/Green House",
            "Map/Hyland Point/Region_Suburbia/Wilkinson House",
            "Map/Hyland Point/Region_Suburbia/Kennedy House",
            "Map/Hyland Point/Region_Suburbia/Long House",
            "Map/Hyland Point/Region_Suburbia/Carl's House",
            "Map/Hyland Point/Region_Westville/George and Molly's House",
            "Map/Hyland Point/Region_Westville/Mick's house",
            "Map/Hyland Point/Region_Downtown/Boutique Store",
            "Map/Hyland Point/Region_Downtown/Gas Station/gas station",
            "Map/Hyland Point/Region_Uptown/Pier/Seats/OutdoorBench",
            "Map/Hyland Point/Bus stops/Bus Stop",
            "Map/Hyland Point/Region_Suburbia/StreetLight",
            "Map/Hyland Point/Rocks",
            "Map/Hyland Point/Cliffs",
            "Map/Hyland Point/Region_Docks"
        };

        public static int Export(string outputDirectory, List<string> summary, Action<string> warn)
        {
            var rows = new List<string[]>();
            Type? gameObjectType = FindType("UnityEngine.GameObject")
                ?? FindType("Il2CppUnityEngine.GameObject");
            Type? componentType = FindType("UnityEngine.Component")
                ?? FindType("Il2CppUnityEngine.Component");

            if (gameObjectType == null)
            {
                WriteCsv(Path.Combine(outputDirectory, "native-scene-prefabs.csv"), rows);
                summary.Add("NativeScenePrefabPaths=not available (Unity GameObject type not found)");
                return 0;
            }

            MethodInfo? find = gameObjectType.GetMethods(PublicStatic)
                .FirstOrDefault(method => method.Name == "Find"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == typeof(string));

            if (find == null)
            {
                WriteCsv(Path.Combine(outputDirectory, "native-scene-prefabs.csv"), rows);
                summary.Add("NativeScenePrefabPaths=GameObject.Find(string) unavailable");
                warn("Native scene source lookup skipped: GameObject.Find(string) was not found.");
                return 0;
            }

            int found = 0;
            int networked = 0;
            foreach (string path in CandidatePaths)
            {
                try
                {
                    object? gameObject = find.Invoke(null, new object[] { path });
                    if (gameObject == null)
                    {
                        rows.Add(new[]
                        {
                            path, "not-found", "", "", "", "", "", "not-scanned"
                        });
                        continue;
                    }

                    found++;
                    object? transform = ReadMember(gameObject, "transform");
                    string position = transform == null ? "" : ReadText(transform, "position");
                    string rotation = transform == null ? "" : ReadText(transform, "rotation");
                    string scale = transform == null ? "" : ReadText(transform, "lossyScale");
                    string active = ReadText(gameObject, "activeInHierarchy");
                    string name = ReadText(gameObject, "name");
                    string hierarchy = transform == null ? "" : ReadHierarchy(transform, 24);
                    List<string> components = componentType == null
                        ? new List<string>()
                        : ReadComponentTypes(gameObject, componentType);
                    bool hasNetworkObject = components.Any(component =>
                        component.IndexOf("NetworkObject", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (hasNetworkObject)
                        networked++;

                    rows.Add(new[]
                    {
                        path,
                        "found",
                        name,
                        active,
                        position,
                        rotation,
                        scale,
                        (hasNetworkObject ? "NETWORKED" : "no NetworkObject observed") +
                            "; components=" + string.Join("; ", components) +
                            "; hierarchy=" + hierarchy
                    });
                }
                catch (Exception ex)
                {
                    rows.Add(new[]
                    {
                        path,
                        "error",
                        "",
                        "",
                        "",
                        "",
                        "",
                        ex.GetType().Name + ": " + ex.Message
                    });
                    warn("Native scene prefab lookup failed for '" + path + "': " + ex.Message);
                }
            }

            string outputPath = Path.Combine(outputDirectory, "native-scene-prefabs.csv");
            WriteCsv(outputPath, rows);
            summary.Add("NativeScenePrefabCandidates=" + CandidatePaths.Length.ToString(CultureInfo.InvariantCulture));
            summary.Add("NativeScenePrefabsFound=" + found.ToString(CultureInfo.InvariantCulture));
            summary.Add("NativeScenePrefabsWithNetworkObject=" + networked.ToString(CultureInfo.InvariantCulture));
            summary.Add("NativeScenePrefabScanMode=READ_ONLY GameObject.Find; no cloning or mutation");
            return found;
        }

        private static List<string> ReadComponentTypes(object gameObject, Type componentType)
        {
            var result = new List<string>();
            MethodInfo? getChildren = gameObject.GetType().GetMethods(PublicInstance)
                .FirstOrDefault(method => method.Name == "GetComponentsInChildren"
                    && method.GetParameters().Length == 2
                    && method.GetParameters()[0].ParameterType == typeof(Type)
                    && method.GetParameters()[1].ParameterType == typeof(bool));

            object? values = null;
            if (getChildren != null)
            {
                values = getChildren.Invoke(gameObject, new object[] { componentType, true });
            }
            else
            {
                MethodInfo? getRoot = gameObject.GetType().GetMethods(PublicInstance)
                    .FirstOrDefault(method => method.Name == "GetComponents"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == typeof(Type));
                values = getRoot?.Invoke(gameObject, new object[] { componentType });
            }

            if (values is IEnumerable enumerable)
            {
                foreach (object? component in enumerable)
                {
                    if (component == null)
                        continue;
                    string typeName = component.GetType().FullName ?? component.GetType().Name;
                    if (!result.Contains(typeName, StringComparer.Ordinal))
                        result.Add(typeName);
                }
            }

            return result.OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        private static string ReadHierarchy(object rootTransform, int maxNodes)
        {
            var names = new List<string>();
            var pending = new Queue<object>();
            pending.Enqueue(rootTransform);

            while (pending.Count > 0 && names.Count < maxNodes)
            {
                object current = pending.Dequeue();
                string name = ReadText(current, "name");
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);

                int childCount = ReadInt(current, "childCount");
                MethodInfo? getChild = current.GetType().GetMethods(PublicInstance)
                    .FirstOrDefault(method => method.Name == "GetChild"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == typeof(int));
                if (getChild == null)
                    continue;

                for (int i = 0; i < childCount && names.Count + pending.Count < maxNodes; i++)
                {
                    object? child = getChild.Invoke(current, new object[] { i });
                    if (child != null)
                        pending.Enqueue(child);
                }
            }

            return string.Join(" > ", names);
        }

        private static int ReadInt(object instance, string name)
        {
            object? value = ReadMember(instance, name);
            try { return Convert.ToInt32(value, CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static string ReadText(object instance, string name)
        {
            object? value = ReadMember(instance, name);
            return value == null ? "" :
                Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        private static object? ReadMember(object instance, string name)
        {
            Type type = instance.GetType();
            PropertyInfo? property = type.GetProperty(name, AllInstance);
            if (property?.CanRead == true)
            {
                try { return property.GetValue(instance, null); }
                catch { }
            }

            FieldInfo? field = type.GetField(name, AllInstance);
            if (field != null)
            {
                try { return field.GetValue(instance); }
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
                    if (type != null)
                        return type;
                }
                catch { }
            }

            return null;
        }

        private static void WriteCsv(string path, IEnumerable<string[]> rows)
        {
            using var writer = new StreamWriter(path, false);
            writer.WriteLine("hierarchyPath,status,name,activeInHierarchy,position,rotation,lossyScale,details");
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
