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
    /// Read-only survey of the game's existing Unity NavMesh and native seat sets.
    /// This deliberately does not build a second navigation system or alter the NavMesh.
    /// </summary>
    internal static class NativeNavigationSurveyExporter
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private sealed class Point3
        {
            public float X;
            public float Y;
            public float Z;
        }

        public static int Export(string outputDirectory, List<string> summary, Action<string> warn)
        {
            Directory.CreateDirectory(outputDirectory);

            Type? vectorType = FindType("UnityEngine.Vector3");
            Type? navMeshType = FindType("UnityEngine.AI.NavMesh");
            Type? hitType = FindType("UnityEngine.AI.NavMeshHit");
            Type? pathType = FindType("UnityEngine.AI.NavMeshPath");

            MethodInfo? sampleMethod = navMeshType?.GetMethods(PublicStatic)
                .FirstOrDefault(method =>
                {
                    if (method.Name != "SamplePosition") return false;
                    ParameterInfo[] p = method.GetParameters();
                    return p.Length == 4
                        && p[0].ParameterType == vectorType
                        && p[1].ParameterType.IsByRef
                        && p[1].ParameterType.GetElementType() == hitType
                        && p[2].ParameterType == typeof(float)
                        && p[3].ParameterType == typeof(int);
                });

            MethodInfo? calculatePathMethod = navMeshType?.GetMethods(PublicStatic)
                .FirstOrDefault(method =>
                {
                    if (method.Name != "CalculatePath") return false;
                    ParameterInfo[] p = method.GetParameters();
                    return p.Length == 4
                        && p[0].ParameterType == vectorType
                        && p[1].ParameterType == vectorType
                        && p[2].ParameterType == typeof(int)
                        && p[3].ParameterType == pathType;
                });

            Point3? docksAnchor = TryReadDocksWarehouseAnchor();
            Point3? navAnchor = null;
            float anchorSampleDistance = float.NaN;
            string anchorStatus = "Docks Warehouse anchor unavailable";

            if (docksAnchor != null && sampleMethod != null && hitType != null && vectorType != null)
            {
                if (TrySample(sampleMethod, hitType, vectorType, docksAnchor, 24f,
                        out Point3 sampled, out anchorSampleDistance, out string sampleStatus))
                {
                    navAnchor = sampled;
                    anchorStatus = "sampled:" + sampleStatus;
                }
                else
                {
                    anchorStatus = "not-on-navmesh:" + sampleStatus;
                }
            }
            else if (sampleMethod == null || hitType == null || vectorType == null)
            {
                anchorStatus = "Unity NavMesh.SamplePosition API unavailable";
            }

            var anchorRows = new[]
            {
                new[]
                {
                    docksAnchor?.X.ToString("R", CultureInfo.InvariantCulture) ?? "",
                    docksAnchor?.Y.ToString("R", CultureInfo.InvariantCulture) ?? "",
                    docksAnchor?.Z.ToString("R", CultureInfo.InvariantCulture) ?? "",
                    navAnchor?.X.ToString("R", CultureInfo.InvariantCulture) ?? "",
                    navAnchor?.Y.ToString("R", CultureInfo.InvariantCulture) ?? "",
                    navAnchor?.Z.ToString("R", CultureInfo.InvariantCulture) ?? "",
                    float.IsNaN(anchorSampleDistance) ? "" : anchorSampleDistance.ToString("R", CultureInfo.InvariantCulture),
                    anchorStatus,
                    sampleMethod != null ? "true" : "false",
                    calculatePathMethod != null ? "true" : "false"
                }
            };
            WriteCsv(Path.Combine(outputDirectory, "native-navmesh-anchor.csv"),
                new[] { "docksWarehouseX", "docksWarehouseY", "docksWarehouseZ", "navMeshX", "navMeshY", "navMeshZ", "sampleDistance", "status", "samplePositionApiFound", "calculatePathApiFound" },
                anchorRows);

            var spawnRows = new List<string[]>();
            for (int slot = 0; slot <= 10; slot++)
            {
                if (!ScheduleIMegaExpansion.Runtime.HarborlinePhysicalBuilder.TryGetPlannedNpcSpawnPosition(
                        slot, out float x, out float y, out float z))
                {
                    spawnRows.Add(new[]
                    {
                        slot.ToString(CultureInfo.InvariantCulture), "", "", "", "", "", "", "",
                        "no-position-or-paused-by-external-map-guard"
                    });
                    continue;
                }

                var proposed = new Point3 { X = x, Y = y, Z = z };
                if (sampleMethod == null || hitType == null || vectorType == null)
                {
                    spawnRows.Add(new[]
                    {
                        slot.ToString(CultureInfo.InvariantCulture), F(x), F(y), F(z), "", "", "", "",
                        "NavMesh.SamplePosition API unavailable"
                    });
                    continue;
                }

                if (!TrySample(sampleMethod, hitType, vectorType, proposed, 4f,
                        out Point3 snapped, out float distance, out string sampleStatus))
                {
                    spawnRows.Add(new[]
                    {
                        slot.ToString(CultureInfo.InvariantCulture), F(x), F(y), F(z), "", "", "",
                        F(distance), "not-on-navmesh:" + sampleStatus
                    });
                    continue;
                }

                string routeStatus = navAnchor == null
                    ? "anchor-not-on-navmesh"
                    : calculatePathMethod == null || pathType == null
                        ? "NavMesh.CalculatePath API unavailable"
                        : CalculatePath(calculatePathMethod, pathType, vectorType!, navAnchor, snapped);

                spawnRows.Add(new[]
                {
                    slot.ToString(CultureInfo.InvariantCulture), F(x), F(y), F(z),
                    F(snapped.X), F(snapped.Y), F(snapped.Z), F(distance), routeStatus
                });
            }

            WriteCsv(Path.Combine(outputDirectory, "harborline-spawn-navigation.csv"),
                new[] { "slot", "proposedX", "proposedY", "proposedZ", "navMeshX", "navMeshY", "navMeshZ", "sampleDistance", "pathFromDocksWarehouse" },
                spawnRows);

            var seatRows = new List<string[]>();
            Type? seatSetType = FindType("Il2CppScheduleOne.AvatarFramework.Animation.AvatarSeatSet")
                ?? FindType("ScheduleOne.AvatarFramework.Animation.AvatarSeatSet");

            if (seatSetType != null && vectorType != null && sampleMethod != null && hitType != null)
            {
                foreach (object seatSet in FindSceneComponents(seatSetType))
                {
                    object? gameObject = GetMember(seatSet, "gameObject");
                    object? transform = GetMember(seatSet, "transform")
                        ?? (gameObject == null ? null : GetMember(gameObject, "transform"));
                    if (gameObject == null || transform == null)
                        continue;

                    Point3? position = ReadVector(GetMember(transform, "position"));
                    if (position == null)
                        continue;

                    string pathStatus = navAnchor == null
                        ? "anchor-not-on-navmesh"
                        : calculatePathMethod == null || pathType == null
                            ? "NavMesh.CalculatePath API unavailable"
                            : "not-sampled";
                    string sampleStatus = "not-sampled";
                    float sampleDistance = float.NaN;
                    Point3? snapped = null;

                    if (TrySample(sampleMethod, hitType, vectorType, position, 4f,
                            out Point3 navPoint, out sampleDistance, out sampleStatus))
                    {
                        snapped = navPoint;
                        pathStatus = navAnchor == null
                            ? "anchor-not-on-navmesh"
                            : calculatePathMethod == null || pathType == null
                                ? "NavMesh.CalculatePath API unavailable"
                                : CalculatePath(calculatePathMethod, pathType, vectorType, navAnchor, navPoint);
                    }

                    object? active = GetMember(gameObject, "activeInHierarchy");
                    seatRows.Add(new[]
                    {
                        BuildHierarchyPath(transform),
                        Convert.ToString(GetMember(gameObject, "name"), CultureInfo.InvariantCulture) ?? "",
                        Convert.ToString(active, CultureInfo.InvariantCulture) ?? "",
                        F(position.X), F(position.Y), F(position.Z),
                        snapped == null ? "" : F(snapped.X),
                        snapped == null ? "" : F(snapped.Y),
                        snapped == null ? "" : F(snapped.Z),
                        float.IsNaN(sampleDistance) ? "" : F(sampleDistance),
                        sampleStatus,
                        pathStatus
                    });
                }
            }
            else
            {
                warn("[Schedule I Mega Expansion] Native AvatarSeatSet type or NavMesh reflection API was not available for the navigation survey.");
            }

            WriteCsv(Path.Combine(outputDirectory, "native-seat-sets.csv"),
                new[] { "hierarchyPath", "gameObjectName", "activeInHierarchy", "worldX", "worldY", "worldZ", "navMeshX", "navMeshY", "navMeshZ", "sampleDistance", "sampleStatus", "pathFromDocksWarehouse" },
                seatRows);

            summary.Add("NativeNavMeshSamplePositionApi=" + (sampleMethod != null ? "found" : "missing"));
            summary.Add("NativeNavMeshCalculatePathApi=" + (calculatePathMethod != null ? "found" : "missing"));
            summary.Add("NativeSeatSets=" + seatRows.Count.ToString(CultureInfo.InvariantCulture));
            summary.Add("HarborlineSpawnNavMeshSlots=" + spawnRows.Count(row => row.Length > 8 && row[8] == "PathComplete").ToString(CultureInfo.InvariantCulture)
                + "/" + spawnRows.Count.ToString(CultureInfo.InvariantCulture));
            summary.Add("NativeNavigationSurveyMode=read-only; uses the game's Unity NavMesh and AvatarSeatSet objects");
            return spawnRows.Count + seatRows.Count;
        }

        private static Point3? TryReadDocksWarehouseAnchor()
        {
            Type? managerType = FindType("S1API.Property.PropertyManager");
            MethodInfo? getAll = managerType?.GetMethod("GetAllProperties", PublicStatic);
            if (getAll == null)
                return null;

            object? raw = getAll.Invoke(null, null);
            if (!(raw is IEnumerable properties))
                return null;

            foreach (object? property in properties)
            {
                if (property == null)
                    continue;
                string name = Convert.ToString(GetMember(property, "PropertyName"), CultureInfo.InvariantCulture) ?? "";
                if (name.IndexOf("Docks Warehouse", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                return ReadVector(GetMember(property, "ExteriorSpawnPosition"));
            }

            return null;
        }

        private static bool TrySample(
            MethodInfo method,
            Type hitType,
            Type vectorType,
            Point3 source,
            float maxDistance,
            out Point3 point,
            out float distance,
            out string status)
        {
            point = new Point3();
            distance = float.NaN;
            status = "sample-failed";
            try
            {
                object hitValue = Activator.CreateInstance(hitType)!;
                object?[] args =
                {
                    Activator.CreateInstance(vectorType, new object[] { source.X, source.Y, source.Z }),
                    hitValue,
                    maxDistance,
                    -1
                };
                object? result = method.Invoke(null, args);
                if (!(result is bool success) || !success || args[1] == null)
                {
                    status = "no-nearby-navmesh";
                    return false;
                }

                Point3? sampled = ReadVector(GetMember(args[1]!, "position"));
                if (sampled == null)
                {
                    status = "NavMeshHit.position unavailable";
                    return false;
                }

                point = sampled;
                object? rawDistance = GetMember(args[1]!, "distance");
                if (rawDistance != null)
                    distance = Convert.ToSingle(rawDistance, CultureInfo.InvariantCulture);
                status = "ok";
                return true;
            }
            catch (Exception ex)
            {
                status = ex.GetType().Name + ":" + ex.Message;
                return false;
            }
        }

        private static string CalculatePath(
            MethodInfo method,
            Type pathType,
            Type vectorType,
            Point3 start,
            Point3 end)
        {
            try
            {
                object path = Activator.CreateInstance(pathType)!;
                object?[] args =
                {
                    Activator.CreateInstance(vectorType, new object[] { start.X, start.Y, start.Z }),
                    Activator.CreateInstance(vectorType, new object[] { end.X, end.Y, end.Z }),
                    -1,
                    path
                };
                object? result = method.Invoke(null, args);
                string status = Convert.ToString(GetMember(path, "status"), CultureInfo.InvariantCulture) ?? "unknown";
                return (result is bool ok && ok ? "" : "calculate-failed:") + status;
            }
            catch (Exception ex)
            {
                return "exception:" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private static IEnumerable<object> FindSceneComponents(Type componentType)
        {
            Type? unityObjectType = FindType("UnityEngine.Object");
            if (unityObjectType == null)
                yield break;

            MethodInfo? find = unityObjectType.GetMethods(PublicStatic)
                .FirstOrDefault(method =>
                {
                    if (method.Name != "FindObjectsOfType") return false;
                    ParameterInfo[] p = method.GetParameters();
                    return p.Length == 1 && p[0].ParameterType == typeof(Type);
                });
            if (find == null)
                yield break;

            object? result;
            try { result = find.Invoke(null, new object[] { componentType }); }
            catch { yield break; }

            if (!(result is IEnumerable items))
                yield break;
            foreach (object? item in items)
            {
                if (item != null)
                    yield return item;
            }
        }

        private static string BuildHierarchyPath(object transform)
        {
            var names = new List<string>();
            object? current = transform;
            int safety = 0;
            while (current != null && safety++ < 128)
            {
                string name = Convert.ToString(GetMember(current, "name"), CultureInfo.InvariantCulture) ?? "";
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
                current = GetMember(current, "parent");
            }
            names.Reverse();
            return string.Join("/", names);
        }

        private static Point3? ReadVector(object? value)
        {
            if (value == null)
                return null;
            try
            {
                return new Point3
                {
                    X = Convert.ToSingle(GetMember(value, "x"), CultureInfo.InvariantCulture),
                    Y = Convert.ToSingle(GetMember(value, "y"), CultureInfo.InvariantCulture),
                    Z = Convert.ToSingle(GetMember(value, "z"), CultureInfo.InvariantCulture)
                };
            }
            catch
            {
                return null;
            }
        }

        private static object? GetMember(object instance, string name)
        {
            Type type = instance.GetType();
            PropertyInfo? property = type.GetProperty(name, InstanceMembers);
            if (property?.CanRead == true)
                return property.GetValue(instance, null);
            FieldInfo? field = type.GetField(name, InstanceMembers);
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

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static void WriteCsv(string path, string[] header, IEnumerable<string[]> rows)
        {
            var lines = new List<string> { string.Join(",", header.Select(Escape)) };
            lines.AddRange(rows.Select(row => string.Join(",", row.Select(Escape))));
            File.WriteAllLines(path, lines);
        }

        private static string Escape(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
