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
    /// Read-only survey of Unity terrain dimensions, texture layers and sampled heights.
    /// It provides the same kind of ground-profile evidence used by map expansion mods
    /// before a road or hillside is sculpted. It never edits TerrainData or tree instances.
    /// </summary>
    internal static class NativeTerrainSurveyExporter
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static int Export(string outputDirectory, List<string> summary, Action<string> warn)
        {
            var terrainRows = new List<string[]>();
            var layerRows = new List<string[]>();
            var heightRows = new List<string[]>();

            Type? terrainType = FindType("UnityEngine.Terrain")
                ?? FindType("Il2CppUnityEngine.Terrain");
            Type? vectorType = FindType("UnityEngine.Vector3")
                ?? FindType("Il2CppUnityEngine.Vector3");

            if (terrainType == null || vectorType == null)
            {
                WriteAll(outputDirectory, terrainRows, layerRows, heightRows);
                summary.Add("NativeTerrainSurvey=not available (Unity Terrain/Vector3 type missing)");
                return 0;
            }

            try
            {
                PropertyInfo? activeProperty = terrainType.GetProperty("activeTerrains", PublicStatic);
                object? rawTerrains = activeProperty?.GetValue(null, null);
                List<object> terrains = ToObjects(rawTerrains);
                int heightSamples = 0;

                foreach (object terrain in terrains)
                {
                    try
                    {
                        string terrainName = ReadText(terrain, "name");
                        object? transform = ReadMember(terrain, "transform");
                        object? position = transform == null ? null : ReadMember(transform, "position");
                        object? data = ReadMember(terrain, "terrainData");
                        object? size = data == null ? null : ReadMember(data, "size");

                        terrainRows.Add(new[]
                        {
                            terrainName,
                            Convert.ToString(position, CultureInfo.InvariantCulture) ?? "",
                            Convert.ToString(size, CultureInfo.InvariantCulture) ?? "",
                            ReadText(data, "heightmapResolution"),
                            ReadText(data, "alphamapResolution"),
                            ReadText(data, "detailResolution"),
                            ReadText(data, "treeInstanceCount"),
                            CountArray(ReadMember(data, "terrainLayers")).ToString(CultureInfo.InvariantCulture)
                        });

                        ExportLayers(terrainName, data, layerRows);
                        heightSamples += ExportHeightSamples(terrain, terrainName, transform, position,
                            size, vectorType, heightRows, warn);
                    }
                    catch (Exception ex)
                    {
                        warn("Terrain survey row failed: " + ex.GetType().Name + ": " + ex.Message);
                    }
                }

                WriteAll(outputDirectory, terrainRows, layerRows, heightRows);
                summary.Add("NativeTerrainObjects=" + terrains.Count.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeTerrainLayers=" + layerRows.Count.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeTerrainHeightSamples=" + heightSamples.ToString(CultureInfo.InvariantCulture));
                summary.Add("NativeTerrainSurveyMode=READ_ONLY; no heightmap/alphamap/tree data modified");
                return terrains.Count;
            }
            catch (Exception ex)
            {
                WriteAll(outputDirectory, terrainRows, layerRows, heightRows);
                summary.Add("NativeTerrainSurvey=ERROR: " + ex.GetType().Name + ": " + ex.Message);
                warn("Native terrain survey failed: " + ex.Message);
                return 0;
            }
        }

        private static int ExportHeightSamples(
            object terrain,
            string terrainName,
            object? transform,
            object? position,
            object? size,
            Type vectorType,
            List<string[]> rows,
            Action<string> warn)
        {
            if (position == null || size == null)
                return 0;

            if (!TryReadVector(position, out float originX, out float originY, out float originZ)
                || !TryReadVector(size, out float sizeX, out _, out float sizeZ))
                return 0;

            MethodInfo? sample = terrain.GetType().GetMethods(PublicInstance)
                .FirstOrDefault(method => method.Name == "SampleHeight"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == vectorType);
            if (sample == null)
                return 0;

            int count = 0;
            const int divisions = 4;
            for (int z = 0; z <= divisions; z++)
            {
                for (int x = 0; x <= divisions; x++)
                {
                    float worldX = originX + (sizeX * x / divisions);
                    float worldZ = originZ + (sizeZ * z / divisions);
                    try
                    {
                        object point = Activator.CreateInstance(vectorType,
                            new object[] { worldX, originY, worldZ })
                            ?? throw new InvalidOperationException("Could not create terrain sample position.");
                        object? sampled = sample.Invoke(terrain, new[] { point });
                        rows.Add(new[]
                        {
                            terrainName,
                            x.ToString(CultureInfo.InvariantCulture),
                            z.ToString(CultureInfo.InvariantCulture),
                            worldX.ToString("0.###", CultureInfo.InvariantCulture),
                            worldZ.ToString("0.###", CultureInfo.InvariantCulture),
                            Convert.ToString(sampled, CultureInfo.InvariantCulture) ?? ""
                        });
                        count++;
                    }
                    catch (Exception ex)
                    {
                        warn("Terrain height sample failed for '" + terrainName + "': " + ex.Message);
                    }
                }
            }
            return count;
        }

        private static void ExportLayers(string terrainName, object? data, List<string[]> rows)
        {
            object? values = data == null ? null : ReadMember(data, "terrainLayers");
            foreach (object layer in ToObjects(values))
            {
                object? texture = ReadMember(layer, "diffuseTexture");
                rows.Add(new[]
                {
                    terrainName,
                    ReadText(layer, "name"),
                    ReadText(texture, "name"),
                    Convert.ToString(ReadMember(layer, "tileSize"), CultureInfo.InvariantCulture) ?? "",
                    Convert.ToString(ReadMember(layer, "tileOffset"), CultureInfo.InvariantCulture) ?? ""
                });
            }
        }

        private static bool TryReadVector(object value, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            try
            {
                x = Convert.ToSingle(ReadMember(value, "x"), CultureInfo.InvariantCulture);
                y = Convert.ToSingle(ReadMember(value, "y"), CultureInfo.InvariantCulture);
                z = Convert.ToSingle(ReadMember(value, "z"), CultureInfo.InvariantCulture);
                return true;
            }
            catch { return false; }
        }

        private static int CountArray(object? value) => ToObjects(value).Count;

        private static List<object> ToObjects(object? value)
        {
            var result = new List<object>();
            if (value is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                    if (item != null)
                        result.Add(item);
            }
            return result;
        }

        private static string ReadText(object? target, string name)
        {
            if (target == null)
                return "";
            object? value = ReadMember(target, name);
            return value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
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
                    if (type != null)
                        return type;
                }
                catch { }
            }
            return null;
        }

        private static void WriteAll(
            string directory,
            IEnumerable<string[]> terrainRows,
            IEnumerable<string[]> layerRows,
            IEnumerable<string[]> heightRows)
        {
            WriteCsv(Path.Combine(directory, "native-terrain-surfaces.csv"),
                new[] { "terrainName", "worldPosition", "size", "heightmapResolution", "alphamapResolution", "detailResolution", "treeInstanceCount", "terrainLayerCount" },
                terrainRows);
            WriteCsv(Path.Combine(directory, "native-terrain-layers.csv"),
                new[] { "terrainName", "layerName", "diffuseTexture", "tileSize", "tileOffset" },
                layerRows);
            WriteCsv(Path.Combine(directory, "native-terrain-height-samples.csv"),
                new[] { "terrainName", "gridX", "gridZ", "worldX", "worldZ", "sampledHeight" },
                heightRows);
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
            value ??= "";
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;
            string quote = new string((char)34, 1);
            string escaped = new string((char)34, 2);
            return quote + value.Replace(quote, escaped) + quote;
        }
    }
}
