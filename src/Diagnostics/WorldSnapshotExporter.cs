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
    /// Reads the S1API wrapper registries through their documented public
    /// read methods. All game changes are deliberately excluded.
    /// </summary>
    internal static class WorldSnapshotExporter
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        public static string Export(string outputRoot, Action<string> log, Action<string> warn)
        {
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            string outputDirectory = Path.Combine(outputRoot, stamp);
            Directory.CreateDirectory(outputDirectory);

            Type? buildingType = FindLoadedType("S1API.Map.Building");
            Assembly? s1ApiAssembly = buildingType?.Assembly;

            var summary = new List<string>
            {
                "Schedule I Mega Expansion - native world snapshot",
                "CapturedUtc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                "S1APIAssembly=" + (s1ApiAssembly?.GetName().Name ?? "not found"),
                "S1APIVersion=" + (s1ApiAssembly?.GetName().Version?.ToString() ?? "unknown"),
                "Mode=READ_ONLY",
                "Note=Standalone MapPOI markers are not native shop/property map pins."
            };

            WriteRegions(outputDirectory, summary, warn);
            WriteBuildings(outputDirectory, summary, warn);
            WriteProperties(outputDirectory, summary, warn);
            WriteBusinesses(outputDirectory, summary, warn);
            WriteStandaloneMapPois(outputDirectory, summary, warn);
            try
            {
                int prefabsFound = NativePrefabCatalogExporter.Export(outputDirectory, summary, warn);
                summary.Add("NativePrefabReferencesFound=" + prefabsFound.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                AddError("NativePrefabs", ex, summary, warn);
            }

            try
            {
                int inspected = NativeApiSurfaceScanner.Export(outputDirectory, warn);
                summary.Add("NativeApiSurfaceMembers=" + inspected.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                AddError("NativeApiSurface", ex, summary, warn);
            }

            string summaryPath = Path.Combine(outputDirectory, "summary.txt");
            File.WriteAllLines(summaryPath, summary);
            log("Native world snapshot complete.");
            return summaryPath;
        }

        private static void WriteRegions(string directory, List<string> summary, Action<string> warn)
        {
            try
            {
                Type? regionType = FindLoadedType("S1API.Map.Region");
                if (regionType == null || !regionType.IsEnum)
                    throw new TypeLoadException("S1API.Map.Region enum was not found.");

                var rows = Enum.GetValues(regionType).Cast<object>().Select(value => new[]
                {
                    value.ToString() ?? string.Empty,
                    Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                });

                WriteCsv(
                    Path.Combine(directory, "native-regions.csv"),
                    new[] { "regionName", "enumValue" },
                    rows);

                summary.Add("NativeRegionValues=" + string.Join(", ", Enum.GetNames(regionType)));
            }
            catch (Exception ex)
            {
                AddError("NativeRegions", ex, summary, warn);
            }
        }

        private static void WriteBuildings(string directory, List<string> summary, Action<string> warn)
        {
            try
            {
                Type? buildingType = FindLoadedType("S1API.Map.Building");
                List<object> buildings = InvokeStaticCollection(buildingType, "GetAll");
                var rows = buildings.Select(building => new[]
                {
                    ReadValue(building, "Name")
                });

                WriteCsv(
                    Path.Combine(directory, "native-buildings.csv"),
                    new[] { "registeredBuildingName" },
                    rows);

                summary.Add("NativeEnterableBuildings=" + buildings.Count.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                AddError("NativeBuildings", ex, summary, warn);
            }
        }

        private static void WriteProperties(string directory, List<string> summary, Action<string> warn)
        {
            try
            {
                Type? managerType = FindLoadedType("S1API.Property.PropertyManager");
                List<object> properties = InvokeStaticCollection(managerType, "GetAllProperties");

                var rows = properties.Select(property => new[]
                {
                    ReadValue(property, "PropertyName"),
                    ReadValue(property, "PropertyCode"),
                    ReadValue(property, "Price"),
                    ReadValue(property, "IsOwned"),
                    ReadValue(property, "ExteriorSpawnPosition"),
                    ReadValue(property, "InteriorSpawnPosition"),
                    ReadValue(property, "NPCSpawnPosition")
                });

                WriteCsv(
                    Path.Combine(directory, "native-properties.csv"),
                    new[] { "propertyName", "propertyCode", "price", "isOwned", "exteriorSpawnPosition", "interiorSpawnPosition", "npcSpawnPosition" },
                    rows);

                int ownedCount = properties.Count(property => ReadBool(property, "IsOwned"));
                summary.Add("NativeProperties=" + properties.Count.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeOwnedProperties=" + ownedCount.ToString(CultureInfo.InvariantCulture));
                summary.Add("PropertyRegistryAccess=read-only through S1API PropertyManager");
            }
            catch (Exception ex)
            {
                AddError("NativeProperties", ex, summary, warn);
            }
        }

        private static void WriteBusinesses(string directory, List<string> summary, Action<string> warn)
        {
            try
            {
                Type? managerType = FindLoadedType("S1API.Property.BusinessManager");
                List<object> businesses = InvokeStaticCollection(managerType, "GetAllBusinesses");

                var rows = businesses.Select(business => new[]
                {
                    ReadValue(business, "PropertyName"),
                    ReadValue(business, "PropertyCode"),
                    ReadValue(business, "Price"),
                    ReadValue(business, "IsOwned"),
                    ReadValue(business, "ExteriorSpawnPosition"),
                    ReadValue(business, "InteriorSpawnPosition"),
                    ReadValue(business, "LaunderCapacity")
                });

                WriteCsv(
                    Path.Combine(directory, "native-businesses.csv"),
                    new[] { "businessName", "propertyCode", "price", "isOwned", "exteriorSpawnPosition", "interiorSpawnPosition", "launderCapacity" },
                    rows);

                int ownedCount = businesses.Count(business => ReadBool(business, "IsOwned"));
                summary.Add("NativeBusinesses=" + businesses.Count.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeOwnedBusinesses=" + ownedCount.ToString(CultureInfo.InvariantCulture));
                summary.Add("BusinessRegistryAccess=read-only through S1API BusinessManager");
            }
            catch (Exception ex)
            {
                AddError("NativeBusinesses", ex, summary, warn);
            }
        }

        private static void WriteStandaloneMapPois(string directory, List<string> summary, Action<string> warn)
        {
            try
            {
                Type? managerType = FindLoadedType("S1API.Map.MapPOIManager");
                if (managerType == null)
                {
                    WriteCsv(
                        Path.Combine(directory, "standalone-phone-map-pois.csv"),
                        new[] { "id", "label", "worldPosition", "isReady", "isVisible", "iconKind" },
                        Array.Empty<string[]>());
                    summary.Add("StandalonePhoneMapPOIAPI=not available in installed S1API assembly");
                    return;
                }

                PropertyInfo? allProperty = managerType.GetProperty("All", PublicStatic);
                object? allValue = allProperty?.GetValue(null);
                List<object> markers = ToObjects(allValue);

                var rows = markers.Select(marker =>
                {
                    object? icon = ReadPropertyObject(marker, "Icon");
                    return new[]
                    {
                        ReadValue(marker, "Id"),
                        ReadValue(marker, "Label"),
                        ReadValue(marker, "Position"),
                        ReadValue(marker, "IsReady"),
                        ReadValue(marker, "IsVisible"),
                        icon == null ? "native/default" : "custom"
                    };
                });

                WriteCsv(
                    Path.Combine(directory, "standalone-phone-map-pois.csv"),
                    new[] { "id", "label", "worldPosition", "isReady", "isVisible", "iconKind" },
                    rows);

                summary.Add("StandalonePhoneMapPOIs=" + markers.Count.ToString(CultureInfo.InvariantCulture));
                summary.Add("StandalonePhoneMapPOIAPI=available (read-only snapshot)");
            }
            catch (Exception ex)
            {
                AddError("StandalonePhoneMapPOIs", ex, summary, warn);
            }
        }

        private static Type? FindLoadedType(string fullName)
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

        private static List<object> InvokeStaticCollection(Type? type, string methodName)
        {
            if (type == null)
                throw new TypeLoadException("Required S1API type was not found: " + methodName);

            MethodInfo? method = type.GetMethod(methodName, PublicStatic);
            if (method == null)
                throw new MissingMethodException(type.FullName, methodName);

            object? result = method.Invoke(null, null);
            return ToObjects(result);
        }

        private static List<object> ToObjects(object? value)
        {
            var result = new List<object>();
            if (value is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    if (item != null)
                        result.Add(item);
                }
            }
            else if (value != null)
            {
                throw new InvalidOperationException("Expected an enumerable registry result but received " + value.GetType().FullName);
            }

            return result;
        }

        private static object? ReadPropertyObject(object instance, string propertyName)
        {
            try
            {
                PropertyInfo? property = instance.GetType().GetProperty(propertyName, PublicInstance);
                return property != null && property.CanRead ? property.GetValue(instance, null) : null;
            }
            catch
            {
                return null;
            }
        }

        private static string ReadValue(object instance, string propertyName)
        {
            object? value = ReadPropertyObject(instance, propertyName);
            return value == null
                ? string.Empty
                : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static bool ReadBool(object instance, string propertyName)
        {
            object? value = ReadPropertyObject(instance, propertyName);
            return value is bool flag && flag;
        }

        private static void WriteCsv(string path, string[] header, IEnumerable<string[]> rows)
        {
            using var writer = new StreamWriter(path, false);
            writer.WriteLine(string.Join(",", header.Select(EscapeCsv)));
            foreach (string[] row in rows)
                writer.WriteLine(string.Join(",", row.Select(EscapeCsv)));
        }

        private static string EscapeCsv(string? value)
        {
            value ??= string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;

            string quote = new string((char)34, 1);
            string escapedQuote = new string((char)34, 2);
            return quote + value.Replace(quote, escapedQuote) + quote;
        }

        private static void AddError(string section, Exception ex, List<string> summary, Action<string> warn)
        {
            summary.Add(section + "=ERROR: " + ex.GetType().Name + ": " + ex.Message);
            warn(section + " snapshot failed: " + ex.Message);
        }
    }
}
