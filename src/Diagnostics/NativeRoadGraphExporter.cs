using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ScheduleIMegaExpansion.Diagnostics
{
    /// <summary>
    /// Exports each child of the vanilla RoadNodes hierarchy as a candidate route node.
    /// This is deliberately a discovery tool: it does not create links, update A* graphs,
    /// move road nodes or recalculate navigation.
    /// </summary>
    internal static class NativeRoadGraphExporter
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const string RoadNodesPath = "Map/Pathfinding/RoadNodes";

        public static int Export(string outputDirectory, List<string> summary, Action<string> warn)
        {
            var rows = new List<string[]>();
            try
            {
                Type? gameObjectType = FindType("UnityEngine.GameObject")
                    ?? FindType("Il2CppUnityEngine.GameObject");
                Type? componentType = FindType("UnityEngine.Component")
                    ?? FindType("Il2CppUnityEngine.Component");
                Type? vectorType = FindType("UnityEngine.Vector3")
                    ?? FindType("Il2CppUnityEngine.Vector3");

                if (gameObjectType == null || vectorType == null)
                {
                    WriteCsv(outputDirectory, rows);
                    summary.Add("NativeRoadGraph=Unity GameObject/Vector3 unavailable");
                    return 0;
                }

                MethodInfo? find = gameObjectType.GetMethods(PublicStatic)
                    .FirstOrDefault(method => method.Name == "Find"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == typeof(string));
                if (find == null)
                {
                    WriteCsv(outputDirectory, rows);
                    summary.Add("NativeRoadGraph=GameObject.Find unavailable");
                    return 0;
                }

                object? root = find.Invoke(null, new object[] { RoadNodesPath });
                if (root == null)
                {
                    WriteCsv(outputDirectory, rows);
                    summary.Add("NativeRoadGraphRoot=not-found: " + RoadNodesPath);
                    return 0;
                }

                object rootTransform = ReadMember(root, "transform")
                    ?? throw new MissingMemberException("RoadNodes root has no Transform.");
                var seen = new HashSet<int>();
                Traverse(rootTransform, RoadNodesPath, 0, componentType, vectorType, seen, rows, warn);

                WriteCsv(outputDirectory, rows);
                summary.Add("NativeRoadGraphRoot=found: " + RoadNodesPath);
                summary.Add("NativeRoadGraphNodes=" + rows.Count.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeRoadGraphMode=READ_ONLY transform hierarchy; no A* graph mutation");
                return rows.Count;
            }
            catch (Exception ex)
            {
                WriteCsv(outputDirectory, rows);
                summary.Add("NativeRoadGraph=ERROR: " + ex.GetType().Name + ": " + ex.Message);
                warn("Native road graph export failed: " + ex.Message);
                return rows.Count;
            }
        }

        private static void Traverse(
            object transform,
            string parentPath,
            int depth,
            Type? componentType,
            Type vectorType,
            HashSet<int> seen,
            List<string[]> rows,
            Action<string> warn)
        {
            int instanceId = GetInstanceId(transform);
            if (instanceId != 0 && !seen.Add(instanceId))
                return;

            string name = ReadText(transform, "name");
            object? position = ReadMember(transform, "position");
            object? localPosition = ReadMember(transform, "localPosition");
            int childCount = ReadInt(transform, "childCount");
            string nodePath = string.IsNullOrEmpty(name) ? parentPath : parentPath + "/" + name;

            object? gameObject = ReadMember(transform, "gameObject");
            string components = "";
            if (gameObject != null && componentType != null)
                components = string.Join("; ", ReadComponentTypes(gameObject, componentType));

            rows.Add(new[]
            {
                nodePath,
                depth.ToString(CultureInfo.InvariantCulture),
                name,
                Convert.ToString(position, CultureInfo.InvariantCulture) ?? "",
                Convert.ToString(localPosition, CultureInfo.InvariantCulture) ?? "",
                childCount.ToString(CultureInfo.InvariantCulture),
                components
            });

            MethodInfo? getChild = transform.GetType().GetMethods(PublicInstance)
                .FirstOrDefault(method => method.Name == "GetChild"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == typeof(int));
            if (getChild == null)
                return;

            for (int i = 0; i < childCount; i++)
            {
                try
                {
                    object? child = getChild.Invoke(transform, new object[] { i });
                    if (child != null)
                        Traverse(child, nodePath, depth + 1, componentType, vectorType, seen, rows, warn);
                }
                catch (Exception ex)
                {
                    warn("Road-node child scan failed at '" + nodePath + "': " + ex.Message);
                }
            }
        }

        private static List<string> ReadComponentTypes(object gameObject, Type componentType)
        {
            var result = new List<string>();
            MethodInfo? get = gameObject.GetType().GetMethods(PublicInstance)
                .FirstOrDefault(method => method.Name == "GetComponents"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == typeof(Type));
            object? values = get?.Invoke(gameObject, new object[] { componentType });
            if (values is System.Collections.IEnumerable enumerable)
            {
                foreach (object? component in enumerable)
                {
                    if (component == null)
                        continue;
                    string name = component.GetType().FullName ?? component.GetType().Name;
                    if (!result.Contains(name, StringComparer.Ordinal))
                        result.Add(name);
                }
            }
            return result.OrderBy(item => item, StringComparer.Ordinal).ToList();
        }

        private static int GetInstanceId(object instance)
        {
            try
            {
                MethodInfo? method = instance.GetType().GetMethods(PublicInstance)
                    .FirstOrDefault(candidate => candidate.Name == "GetInstanceID"
                        && candidate.GetParameters().Length == 0);
                return method == null ? 0 : Convert.ToInt32(method.Invoke(instance, null), CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        private static int ReadInt(object target, string member)
        {
            try { return Convert.ToInt32(ReadMember(target, member), CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static string ReadText(object target, string member)
        {
            object? value = ReadMember(target, member);
            return value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        private static object? ReadMember(object target, string member)
        {
            PropertyInfo? property = target.GetType().GetProperty(member, AllInstance);
            if (property?.CanRead == true)
            {
                try { return property.GetValue(target, null); }
                catch { }
            }
            FieldInfo? field = target.GetType().GetField(member, AllInstance);
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
                    if (type != null)
                        return type;
                }
                catch { }
            }
            return null;
        }

        private static void WriteCsv(string directory, IEnumerable<string[]> rows)
        {
            string path = Path.Combine(directory, "native-road-nodes.csv");
            using var writer = new StreamWriter(path, false);
            writer.WriteLine("hierarchyPath,depth,nodeName,worldPosition,localPosition,childCount,components");
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
