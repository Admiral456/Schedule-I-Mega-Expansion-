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
    /// Read-only discovery of documented S1MAPI/FishNet prefab references.
    /// PrefabRef.Find() retrieves registered prefab objects; this exporter never
    /// calls Instantiate or InstantiateNetworked and never changes the game world.
    /// </summary>
    internal static class NativePrefabCatalogExporter
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private static readonly string[] DocumentedPrefabNames =
        {
            "ATM",
            "ModularSwitch",
            "SlidingDoors",
            "Door",
            "Storage",
            "MetalGlassDoor",
            "ClassicalWoodenDoor"
        };

        public static int Export(string directory, List<string> summary, Action<string> warn)
        {
            var rows = new List<string[]>();
            Type? prefabRefType = FindType("S1MAPI.Core.PrefabRef");
            if (prefabRefType == null)
            {
                WriteCsv(Path.Combine(directory, "native-prefabs.csv"), rows);
                summary.Add("S1MAPIPrefabCatalog=not available (S1MAPI.Core.PrefabRef not loaded)");
                return 0;
            }

            ConstructorInfo? constructor = prefabRefType.GetConstructor(new[] { typeof(string) });
            MethodInfo? find = prefabRefType.GetMethod("Find", BindingFlags.Public | BindingFlags.Instance);
            if (constructor == null || find == null || find.GetParameters().Length != 0)
            {
                WriteCsv(Path.Combine(directory, "native-prefabs.csv"), rows);
                summary.Add("S1MAPIPrefabCatalog=API shape not recognized; no prefabs inspected");
                warn("S1MAPI PrefabRef API was present, but its documented constructor/Find signature was not found.");
                return 0;
            }

            var candidates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in DocumentedPrefabNames)
                candidates[name] = "documented-name-probe";

            Type? knownPrefabsType = FindType("S1MAPI.S1.Prefabs");
            if (knownPrefabsType != null)
            {
                foreach (FieldInfo field in knownPrefabsType.GetFields(PublicStatic))
                {
                    if (prefabRefType.IsAssignableFrom(field.FieldType))
                        candidates[field.Name] = "S1MAPI.S1.Prefabs-field";
                }

                foreach (PropertyInfo property in knownPrefabsType.GetProperties(PublicStatic))
                {
                    if (prefabRefType.IsAssignableFrom(property.PropertyType) && property.GetIndexParameters().Length == 0)
                        candidates[property.Name] = "S1MAPI.S1.Prefabs-property";
                }
            }

            int found = 0;
            int networked = 0;
            foreach (KeyValuePair<string, string> candidate in candidates.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    object reference = constructor.Invoke(new object[] { candidate.Key });
                    object? prefab = find.Invoke(reference, null);
                    if (prefab == null)
                    {
                        rows.Add(new[] { candidate.Key, candidate.Value, "false", "", "false", "", "not-found" });
                        continue;
                    }

                    string instanceName = Convert.ToString(ReadMember(prefab, "name"), CultureInfo.InvariantCulture) ?? candidate.Key;
                    List<string> components = ReadComponentTypes(prefab);
                    bool hasNetworkObject = components.Any(name =>
                        name.IndexOf("NetworkObject", StringComparison.OrdinalIgnoreCase) >= 0);
                    found++;
                    if (hasNetworkObject)
                        networked++;

                    rows.Add(new[]
                    {
                        candidate.Key,
                        candidate.Value,
                        "true",
                        instanceName,
                        hasNetworkObject.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(),
                        string.Join("; ", components),
                        hasNetworkObject ? "network-spawn-required" : "review-before-placement"
                    });
                }
                catch (Exception ex)
                {
                    rows.Add(new[] { candidate.Key, candidate.Value, "error", "", "", "", ex.GetType().Name + ": " + ex.Message });
                    warn("Prefab catalog probe failed for " + candidate.Key + ": " + ex.Message);
                }
            }

            WriteCsv(Path.Combine(directory, "native-prefabs.csv"), rows);
            summary.Add("S1MAPIPrefabCatalog=available");
            summary.Add("S1MAPIPrefabCandidates=" + candidates.Count.ToString(CultureInfo.InvariantCulture));
            summary.Add("S1MAPIPrefabsFound=" + found.ToString(CultureInfo.InvariantCulture));
            summary.Add("S1MAPINetworkedPrefabsFound=" + networked.ToString(CultureInfo.InvariantCulture));
            summary.Add("S1MAPIPrefabScanMode=read-only Find(); no Instantiate/InstantiateNetworked calls");
            return found;
        }

        private static List<string> ReadComponentTypes(object prefab)
        {
            var result = new List<string>();
            try
            {
                MethodInfo? getComponents = prefab.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(method =>
                        method.Name == "GetComponents"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == typeof(Type));
                if (getComponents == null)
                    return result;

                Type? componentType = FindType("UnityEngine.Component")
                    ?? FindType("Il2CppUnityEngine.Component");
                if (componentType == null)
                    return result;

                object? values = getComponents.Invoke(prefab, new object[] { componentType });
                if (values is IEnumerable enumerable)
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
            }
            catch
            {
                // Component inspection is optional; a successful Find() is still useful.
            }

            return result.OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        private static object? ReadMember(object instance, string name)
        {
            PropertyInfo? property = instance.GetType().GetProperty(
                name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property?.CanRead == true)
                return property.GetValue(instance, null);

            FieldInfo? field = instance.GetType().GetField(
                name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(instance);
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
            writer.WriteLine("referenceName,source,found,instanceName,hasNetworkObject,components,recommendedSpawnHandling");
            foreach (string[] row in rows)
                writer.WriteLine(string.Join(",", row.Select(EscapeCsv)));
        }

        private static string EscapeCsv(string? value)
        {
            value ??= string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;

            string quote = new string((char)34, 1);
            string escaped = new string((char)34, 2);
            return quote + value.Replace(quote, escaped) + quote;
        }
    }
}