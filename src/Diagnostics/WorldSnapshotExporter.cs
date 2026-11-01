using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using S1API.Map;
using S1API.Property;

namespace ScheduleIMegaExpansion.Diagnostics
{
    /// <summary>
    /// Read-only snapshot of the native game registries exposed by S1API.
    /// It never creates, removes, purchases, or modifies a game object.
    /// </summary>
    internal static class WorldSnapshotExporter
    {
        public static string Export(string outputRoot, Action<string> log, Action<string> warn)
        {
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            string outputDirectory = Path.Combine(outputRoot, stamp);
            Directory.CreateDirectory(outputDirectory);

            var summary = new List<string>
            {
                "Schedule I Mega Expansion - native world snapshot",
                "CapturedUtc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                "S1APIAssembly=" + typeof(Building).Assembly.GetName().Name,
                "S1APIVersion=" + (typeof(Building).Assembly.GetName().Version?.ToString() ?? "unknown"),
                "Mode=READ_ONLY",
                "Note=Standalone MapPOI markers are not native shop/property map pins."
            };

            WriteRegions(outputDirectory, summary, warn);
            WriteBuildings(outputDirectory, summary, warn);
            WriteProperties(outputDirectory, summary, warn);
            WriteBusinesses(outputDirectory, summary, warn);
            WriteStandaloneMapPois(outputDirectory, summary, warn);

            string summaryPath = Path.Combine(outputDirectory, "summary.txt");
            File.WriteAllLines(summaryPath, summary);
            log("Native world snapshot complete.");
            return summaryPath;
        }

        private static void WriteRegions(string directory, List<string> summary, Action<string> warn)
        {
            try
            {
                var rows = Enum.GetValues(typeof(Region))
                    .Cast<Region>()
                    .Select(region => new[]
                    {
                        region.ToString(),
                        ((int)region).ToString(CultureInfo.InvariantCulture)
                    });

                WriteCsv(
                    Path.Combine(directory, "native-regions.csv"),
                    new[] { "regionName", "enumValue" },
                    rows);

                summary.Add("NativeRegionValues=" + string.Join(", ", Enum.GetNames(typeof(Region))));
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
                Building[] buildings = Building.GetAll();
                var rows = buildings
                    .Where(building => building != null)
                    .Select(building => new[] { building.Name ?? string.Empty });

                WriteCsv(
                    Path.Combine(directory, "native-buildings.csv"),
                    new[] { "registeredBuildingName" },
                    rows);

                summary.Add("NativeEnterableBuildings=" + buildings.Length.ToString(CultureInfo.InvariantCulture));
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
                List<PropertyWrapper> properties = PropertyManager.GetAllProperties();
                var rows = properties.Select(property => new[]
                {
                    property.PropertyName ?? string.Empty,
                    property.PropertyCode ?? string.Empty,
                    FormatNumber(property.Price),
                    property.IsOwned.ToString(),
                    ReadOptionalProperty(property, "ExteriorSpawnPosition"),
                    ReadOptionalProperty(property, "InteriorSpawnPosition"),
                    ReadOptionalProperty(property, "NPCSpawnPosition")
                });

                WriteCsv(
                    Path.Combine(directory, "native-properties.csv"),
                    new[] { "propertyName", "propertyCode", "price", "isOwned", "exteriorSpawnPosition", "interiorSpawnPosition", "npcSpawnPosition" },
                    rows);

                int ownedCount = properties.Count(property => property.IsOwned);
                summary.Add("NativeProperties=" + properties.Count.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeOwnedProperties=" + ownedCount.ToString(CultureInfo.InvariantCulture));
                summary.Add("PropertyRegistryAccess=read-only through S1API.Property.PropertyManager");
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
                List<BusinessWrapper> businesses = BusinessManager.GetAllBusinesses();
                var rows = businesses.Select(business => new[]
                {
                    business.PropertyName ?? string.Empty,
                    business.PropertyCode ?? string.Empty,
                    FormatNumber(business.Price),
                    business.IsOwned.ToString(),
                    ReadOptionalProperty(business, "ExteriorSpawnPosition"),
                    ReadOptionalProperty(business, "InteriorSpawnPosition"),
                    ReadOptionalProperty(business, "LaunderCapacity")
                });

                WriteCsv(
                    Path.Combine(directory, "native-businesses.csv"),
                    new[] { "businessName", "propertyCode", "price", "isOwned", "exteriorSpawnPosition", "interiorSpawnPosition", "launderCapacity" },
                    rows);

                int ownedCount = businesses.Count(business => business.IsOwned);
                summary.Add("NativeBusinesses=" + businesses.Count.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeOwnedBusinesses=" + ownedCount.ToString(CultureInfo.InvariantCulture));
                summary.Add("BusinessRegistryAccess=read-only through S1API.Property.BusinessManager");
            }
            catch (Exception ex)
            {
                AddError("NativeBusinesses", ex, summary, warn);
            }
        }

        /// <summary>
        /// MapPOI was added after some published package versions. Reflection
        /// keeps this diagnostic mod compatible when that optional API is absent.
        /// </summary>
        private static void WriteStandaloneMapPois(string directory, List<string> summary, Action<string> warn)
        {
            try
            {
                Type? managerType = typeof(Building).Assembly.GetType("S1API.Map.MapPOIManager", false);
                PropertyInfo? allProperty = managerType?.GetProperty(
                    "All",
                    BindingFlags.Public | BindingFlags.Static);

                object? allValue = allProperty?.GetValue(null);
                var rows = new List<string[]>();

                if (allValue is IEnumerable items)
                {
                    foreach (object? item in items)
                    {
                        if (item == null)
                            continue;

                        object? icon = GetPropertyValue(item, "Icon");
                        rows.Add(new[]
                        {
                            ReadOptionalProperty(item, "Id"),
                            ReadOptionalProperty(item, "Label"),
                            ReadOptionalProperty(item, "Position"),
                            ReadOptionalProperty(item, "IsReady"),
                            ReadOptionalProperty(item, "IsVisible"),
                            icon == null ? "native/default" : "custom"
                        });
                    }
                }

                WriteCsv(
                    Path.Combine(directory, "standalone-phone-map-pois.csv"),
                    new[] { "id", "label", "worldPosition", "isReady", "isVisible", "iconKind" },
                    rows);

                summary.Add("StandalonePhoneMapPOIs=" + rows.Count.ToString(CultureInfo.InvariantCulture));
                summary.Add(managerType == null
                    ? "StandalonePhoneMapPOIAPI=not available in this S1API assembly"
                    : "StandalonePhoneMapPOIAPI=available (read-only snapshot)");
            }
            catch (Exception ex)
            {
                AddError("StandalonePhoneMapPOIs", ex, summary, warn);
            }
        }

        private static object? GetPropertyValue(object instance, string propertyName)
        {
            try
            {
                PropertyInfo? property = instance.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Public | BindingFlags.Instance);
                return property != null && property.CanRead ? property.GetValue(instance) : null;
            }
            catch
            {
                return null;
            }
        }

        private static string ReadOptionalProperty(object instance, string propertyName)
        {
            object? value = GetPropertyValue(instance, propertyName);
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string FormatNumber(float value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);

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

            return """ + value.Replace(""", """") + """;
        }

        private static void AddError(string section, Exception ex, List<string> summary, Action<string> warn)
        {
            summary.Add(section + "=ERROR: " + ex.GetType().Name + ": " + ex.Message);
            warn(section + " snapshot failed: " + ex.Message);
        }
    }
}
