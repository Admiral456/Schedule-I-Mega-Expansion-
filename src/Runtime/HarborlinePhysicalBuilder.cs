using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MelonLoader;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// Automatically builds a first physical Harborline dockyard slice.
    /// The location is anchored to the native Docks Warehouse property position,
    /// then selected from nearby flat, ground-collidable and unoccupied patches.
    /// No player hotkey or user-supplied coordinates are required.
    ///
    /// This remains a geometry milestone, not yet a native registered Building/Property.
    /// It is intentionally disabled in multiplayer until network spawning is added.
    /// </summary>
    internal static class HarborlinePhysicalBuilder
    {
        private const string RootName = "ScheduleIMegaExpansion_Harborline_PhysicalPreview";
        private const string PendingRootName = RootName + "_Pending";
        private static bool _built;
        private static object? _rootObject;
        private static object? _rootTransform;
        private static Type? _gameObjectType;
        private static Type? _primitiveType;
        private static Type? _vector3Type;
        private static Type? _quaternionType;
        private static Type? _colorType;
        private static Type? _meshRendererType;

        private readonly struct Anchor
        {
            public Anchor(float x, float y, float z, float yaw)
            {
                X = x; Y = y; Z = z; Yaw = yaw;
            }

            public float X { get; }
            public float Y { get; }
            public float Z { get; }
            public float Yaw { get; }
        }

        private static int _placementAttempts;
        private static bool _reportedMissingDocksAnchor;
        private static bool _reportedNoClearSite;
        private static string? _selectedPlacement;
        private static Anchor? _lastPlacementAnchor;

        public static void ResetForNewSave()
        {
            CleanupGeneratedRoot(RootName);
            CleanupGeneratedRoot(PendingRootName);
            try { CleanupActiveRoot(); }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Active-root cleanup during scene reset failed: " + ex.Message);
            }
            _built = false;
            _rootObject = null;
            _rootTransform = null;
            _placementAttempts = 0;
            _reportedMissingDocksAnchor = false;
            _reportedNoClearSite = false;
            _selectedPlacement = null;
            _lastPlacementAnchor = null;
        }

        /// <summary>
        /// Returns a stable spawn point inside the same candidate footprint used by the physical dockyard.
        /// NPC prefabs may ask for this before the geometry is committed, so the chosen anchor is cached
        /// and reused by TryAutoGenerate rather than independently searching for a second location.
        /// </summary>
        public static bool TryGetPlannedNpcSpawnPosition(int slot, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            if (slot < 0 || slot > 4)
                return false;

            try
            {
                if (!_lastPlacementAnchor.HasValue)
                {
                    if (!TryFindDocksWarehouseAnchor(out Anchor docksAnchor))
                        return false;
                    EnsureUnityTypes();
                    if (!TryFindClearPlacement(docksAnchor, out Anchor planned))
                        return false;
                    _lastPlacementAnchor = planned;
                    _selectedPlacement = "reserved for native NPCs: " + _selectedPlacement;
                }

                Anchor placement = _lastPlacementAnchor.Value;
                float localX = slot switch
                {
                    0 => -4.5f,
                    1 => 4.5f,
                    2 => -17.0f,
                    3 => -1.5f,
                    _ => 15.0f
                };
                float localZ = slot switch
                {
                    0 => 10.0f,
                    1 => 10.0f,
                    2 => -9.0f,
                    3 => -11.0f,
                    _ => 7.0f
                };
                float radians = placement.Yaw * (float)Math.PI / 180f;
                float cos = (float)Math.Cos(radians);
                float sin = (float)Math.Sin(radians);
                x = placement.X + localX * cos + localZ * sin;
                y = placement.Y;
                z = placement.Z - localX * sin + localZ * cos;
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Could not resolve native NPC spawn point: " + ex.Message);
                return false;
            }
        }

        public static bool IsConfirmedSinglePlayerSession()
        {
            return IsConfirmedSinglePlayer(out _);
        }

        public static bool TryAutoGenerate(string gameRoot, Action<string> log, Action<string> warn)
        {
            if (_built)
                return true;

            _placementAttempts++;
            try
            {
                if (!IsConfirmedSinglePlayer(out int playerCount))
                {
                    if (_placementAttempts == 1)
                    {
                        string reason = playerCount == 0
                            ? "S1API player registry is not ready"
                            : playerCount > 1
                                ? "multiple players are connected"
                                : "the S1API player registry is unavailable";
                        warn("Harborline auto-generation is waiting for a confirmed single-player session (" + reason + "). Multiplayer generation requires host-authoritative network spawning.");
                    }
                    return false;
                }

                // Remove an orphaned pending root left by an interrupted earlier attempt.
                CleanupGeneratedRoot(PendingRootName);

                if (FindExistingRoot())
                {
                    if (!_lastPlacementAnchor.HasValue && !TryLoadPlacementReport(gameRoot))
                        warn("The existing Harborline root was found but its placement report could not be read; NPC placement will remain disabled until coordinates are recovered.");
                    _built = true;
                    log("Existing Harborline world root found; skipped duplicate generation.");
                    return true;
                }

                if (!TryFindDocksWarehouseAnchor(out Anchor docksAnchor))
                {
                    if (!_reportedMissingDocksAnchor)
                    {
                        _reportedMissingDocksAnchor = true;
                        warn("Waiting for the native Docks Warehouse property position. Harborline will build automatically when the property registry is ready.");
                    }
                    return false;
                }

                EnsureUnityTypes();
                Anchor placement;
                if (_lastPlacementAnchor.HasValue)
                {
                    placement = _lastPlacementAnchor.Value;
                }
                else if (TryFindClearPlacement(docksAnchor, out placement))
                {
                    _lastPlacementAnchor = placement;
                }
                else
                {
                    if (!_reportedNoClearSite)
                    {
                        _reportedNoClearSite = true;
                        warn("Docks Warehouse was found, but no safe flat unoccupied ground patch was found in the nearby search radius. No geometry was placed.");
                    }
                    return false;
                }

                CreateRoot(placement, out object rootObject, out object rootTransform);
                _rootObject = rootObject;
                _rootTransform = rootTransform;

                Add("Dockyard ground slab", 0f, -0.28f, 0f, 54f, 0.55f, 28f, 0.34f, 0.36f, 0.37f);
                Add("Quayside asphalt road", 10.4f, 0.035f, 0f, 6.2f, 0.07f, 28f, 0.12f, 0.13f, 0.14f);
                Add("Road edge line left", 7.15f, 0.09f, 0f, 0.12f, 0.025f, 27f, 0.85f, 0.78f, 0.46f);
                Add("Road edge line right", 13.65f, 0.09f, 0f, 0.12f, 0.025f, 27f, 0.85f, 0.78f, 0.46f);
                Add("Dock loading apron", -6.3f, 0.09f, -9.2f, 13.4f, 0.14f, 8.2f, 0.42f, 0.43f, 0.41f);

                BuildWarehouse();
                BuildContainers();
                BuildCrane();
                BuildStreetFurniture();
                BuildQuay();

                // Rename only after all geometry has been created. Persist the
                // placement report atomically so a crash cannot leave a truncated
                // JSON file that looks like a completed generation.
                SetProperty(rootObject, "name", RootName);

                string reportDirectory = Path.Combine(gameRoot, "UserData", "ScheduleIMegaExpansion", "Harborline");
                Directory.CreateDirectory(reportDirectory);
                string reportPath = Path.Combine(reportDirectory, "last-generation.json");
                string temporaryReportPath = reportPath + ".tmp";
                File.WriteAllText(
                    temporaryReportPath,
                    JsonSerializer.Serialize(new
                    {
                        generatedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                        anchorSource = "S1API.Property.PropertyManager Docks Warehouse ExteriorSpawnPosition",
                        nativeRegion = "Docks",
                        placementRegionVerified = false,
                        x = placement.X,
                        y = placement.Y,
                        z = placement.Z,
                        rotationY = placement.Yaw,
                        root = RootName,
                        placement = _selectedPlacement,
                        status = "geometry-created; in-game region/road/collision verification still required"
                    }, new JsonSerializerOptions { WriteIndented = true }));
                if (File.Exists(reportPath))
                    File.Delete(reportPath);
                File.Move(temporaryReportPath, reportPath);

                // Only publish the generation as complete after the geometry and
                // its placement report have both been created successfully.
                _built = true;
                _rootObject = null;
                _rootTransform = null;
                log("Harborline generated automatically from the native Docks Warehouse property anchor.");
                log("Physical slice spawned at X=" + F(placement.X) + " Y=" + F(placement.Y) + " Z=" + F(placement.Z) + " yaw=" + F(placement.Yaw));
                log("Created under root object: " + RootName);
                log("This stage is physical geometry only; native property/shop registration, doors, NPCs and multiplayer replication remain separate implementation stages.");
                return true;
            }
            catch (Exception ex)
            {
                // Roll back the pending hierarchy so the next retry cannot mistake
                // a partially built dockyard for a successful one.
                try { CleanupActiveRoot(); }
                catch (Exception cleanupError)
                {
                    warn("Harborline rollback cleanup also failed: " + cleanupError.Message);
                }
                _built = false;
                warn("Harborline physical build failed and pending geometry was rolled back where possible: " + ex.GetType().Name + ": " + ex.Message);
                warn(ex.StackTrace ?? string.Empty);
                return false;
            }
        }

        private static bool TryFindDocksWarehouseAnchor(out Anchor anchor)
        {
            anchor = default;
            Type? managerType = FindType("S1API.Property.PropertyManager");
            MethodInfo? getAll = managerType?.GetMethod("GetAllProperties", BindingFlags.Public | BindingFlags.Static);
            if (getAll == null)
                return false;

            object? collection = getAll.Invoke(null, null);
            if (!(collection is IEnumerable enumerable))
                return false;

            foreach (object? property in enumerable)
            {
                if (property == null)
                    continue;

                string name = Convert.ToString(GetMemberValue(property, "PropertyName"), CultureInfo.InvariantCulture) ?? string.Empty;
                if (name.IndexOf("Docks Warehouse", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                object? position = GetMemberValue(property, "ExteriorSpawnPosition");
                if (position != null && TryReadVector(position, out float x, out float y, out float z))
                {
                    anchor = new Anchor(x, y, z, 0f);
                    return true;
                }
            }

            return false;
        }

        private static bool TryLoadPlacementReport(string gameRoot)
        {
            try
            {
                string reportPath = Path.Combine(gameRoot, "UserData", "ScheduleIMegaExpansion", "Harborline", "last-generation.json");
                if (!File.Exists(reportPath))
                    return false;

                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(reportPath));
                JsonElement root = document.RootElement;
                if (!root.TryGetProperty("x", out JsonElement x)
                    || !root.TryGetProperty("y", out JsonElement y)
                    || !root.TryGetProperty("z", out JsonElement z))
                    return false;

                float yaw = root.TryGetProperty("rotationY", out JsonElement rotation)
                    ? rotation.GetSingle()
                    : 0f;
                _lastPlacementAnchor = new Anchor(x.GetSingle(), y.GetSingle(), z.GetSingle(), yaw);
                _selectedPlacement = "restored from last-generation.json";
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Failed to restore Harborline placement report: " + ex.Message);
                return false;
            }
        }

        private static bool TryFindClearPlacement(Anchor origin, out Anchor placement)
        {
            placement = default;
            Type physics = FindType("UnityEngine.Physics") ?? throw new TypeLoadException("UnityEngine.Physics was not found.");
            Type rayType = FindType("UnityEngine.Ray") ?? throw new TypeLoadException("UnityEngine.Ray was not found.");
            Type hitType = FindType("UnityEngine.RaycastHit") ?? throw new TypeLoadException("UnityEngine.RaycastHit was not found.");

            MethodInfo? checkBox = physics.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == "CheckBox")
                .OrderByDescending(method => method.GetParameters().Length)
                .FirstOrDefault(method =>
                {
                    ParameterInfo[] p = method.GetParameters();
                    return p.Length >= 3
                        && p[0].ParameterType == _vector3Type
                        && p[1].ParameterType == _vector3Type
                        && p[2].ParameterType == _quaternionType;
                })
                ?? physics.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method =>
                    {
                        ParameterInfo[] p = method.GetParameters();
                        return method.Name == "CheckBox"
                            && p.Length == 2
                            && p[0].ParameterType == _vector3Type
                            && p[1].ParameterType == _vector3Type;
                    });
            MethodInfo? raycast = physics.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method =>
                {
                    ParameterInfo[] p = method.GetParameters();
                    return method.Name == "Raycast" && p.Length >= 3 && p[0].ParameterType == rayType
                        && p[1].ParameterType.IsByRef && p[1].ParameterType.GetElementType() == hitType
                        && p[2].ParameterType == typeof(float);
                })
                .OrderByDescending(method => method.GetParameters().Length)
                .FirstOrDefault();

            if (checkBox == null || raycast == null)
                throw new MissingMethodException("Required Physics.CheckBox or Physics.Raycast overload was not found.");

            float[] radii = { 42f, 54f, 66f, 78f, 90f, 104f };
            foreach (float radius in radii)
            {
                for (int angle = 0; angle < 360; angle += 30)
                {
                    float radians = angle * (float)Math.PI / 180f;
                    float x = origin.X + (float)Math.Sin(radians) * radius;
                    float z = origin.Z + (float)Math.Cos(radians) * radius;
                    float yaw = angle;

                    if (IsOccupied(checkBox, x, origin.Y, z, yaw))
                        continue;
                    if (!TrySampleGround(raycast, rayType, x, origin.Y, z, yaw, out float groundY))
                        continue;

                    placement = new Anchor(x, groundY, z, yaw);
                    _selectedPlacement = "clear-ground candidate radius=" + F(radius) + " angle=" + F(yaw);
                    return true;
                }
            }

            return false;
        }

        private static bool IsOccupied(MethodInfo checkBox, float x, float y, float z, float yaw)
        {
            ParameterInfo[] parameters = checkBox.GetParameters();
            object?[] args = new object?[parameters.Length];
            args[0] = NewVector3(x, y + 9f, z);
            args[1] = NewVector3(28f, 8f, 15f);

            for (int i = 2; i < parameters.Length; i++)
            {
                Type t = parameters[i].ParameterType;
                if (t == _quaternionType)
                    args[i] = NewYawQuaternion(yaw);
                else if (t == typeof(int))
                    args[i] = -1;
                else if (t.IsEnum)
                {
                    string choice = Enum.GetNames(t).Contains("Ignore") ? "Ignore" : Enum.GetNames(t)[0];
                    args[i] = Enum.Parse(t, choice);
                }
                else if (parameters[i].HasDefaultValue)
                    args[i] = parameters[i].DefaultValue;
                else
                    args[i] = t.IsValueType ? Activator.CreateInstance(t) : null;
            }

            return Convert.ToBoolean(checkBox.Invoke(null, args), CultureInfo.InvariantCulture);
        }

        private static bool TrySampleGround(MethodInfo raycast, Type rayType,
            float x, float y, float z, float yaw, out float groundY)
        {
            groundY = y;
            float radians = yaw * (float)Math.PI / 180f;
            float[] lateral = { 0f, -23f, 23f };
            float[] forward = { 0f, -11f, 11f };
            var heights = new System.Collections.Generic.List<float>();

            foreach (float side in lateral)
            foreach (float along in forward)
            {
                if (side != 0f && along != 0f)
                    continue;

                float sx = x + side * (float)Math.Cos(radians) + along * (float)Math.Sin(radians);
                float sz = z - side * (float)Math.Sin(radians) + along * (float)Math.Cos(radians);
                object ray = Activator.CreateInstance(rayType, new object[]
                {
                    NewVector3(sx, y + 100f, sz), NewVector3(0f, -1f, 0f)
                })!;

                ParameterInfo[] parameters = raycast.GetParameters();
                object?[] args = new object?[parameters.Length];
                args[0] = ray;
                args[1] = null;
                args[2] = 200f;
                for (int i = 3; i < parameters.Length; i++)
                {
                    Type t = parameters[i].ParameterType;
                    if (t == typeof(int))
                        args[i] = -1;
                    else if (t.IsEnum)
                    {
                        string choice = Enum.GetNames(t).Contains("Ignore") ? "Ignore" : Enum.GetNames(t)[0];
                        args[i] = Enum.Parse(t, choice);
                    }
                    else if (parameters[i].HasDefaultValue)
                        args[i] = parameters[i].DefaultValue;
                    else
                        args[i] = t.IsValueType ? Activator.CreateInstance(t) : null;
                }

                bool hit = Convert.ToBoolean(raycast.Invoke(null, args), CultureInfo.InvariantCulture);
                if (!hit || args[1] == null)
                    return false;

                object hitInfo = args[1]!;
                object? collider = GetMemberValue(hitInfo, "collider");
                if (collider == null || IsLikelyWaterCollider(collider))
                    return false;

                object? point = GetMemberValue(hitInfo, "point");
                object? normal = GetMemberValue(hitInfo, "normal");
                if (point == null || normal == null
                    || !TryReadVector(point, out _, out float hitY, out _)
                    || !TryReadVector(normal, out _, out float normalY, out _)
                    || normalY < 0.92f
                    || Math.Abs(hitY - y) > 5f)
                    return false;

                heights.Add(hitY);
            }

            if (heights.Count < 5 || heights.Max() - heights.Min() > 1.5f)
                return false;

            groundY = heights.Average();
            return true;
        }

        private static bool IsLikelyWaterCollider(object collider)
        {
            object? gameObject = GetMemberValue(collider, "gameObject");
            string identity = string.Join(" ",
                new[]
                {
                    Convert.ToString(GetMemberValue(collider, "name"), CultureInfo.InvariantCulture),
                    Convert.ToString(GetMemberValue(collider, "tag"), CultureInfo.InvariantCulture),
                    Convert.ToString(GetMemberValue(gameObject ?? collider, "name"), CultureInfo.InvariantCulture),
                    Convert.ToString(GetMemberValue(gameObject ?? collider, "tag"), CultureInfo.InvariantCulture)
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

            string normalized = identity.ToLowerInvariant();
            string[] waterTokens = { "water", "ocean", "sea", "lake", "river", "pool", "canal", "flood" };
            if (waterTokens.Any(token => normalized.Contains(token)))
                return true;

            // Unity projects commonly use a dedicated Water/Ocean layer even
            // when the collider GameObject has a generic name.
            object? layerValue = GetMemberValue(gameObject ?? collider, "layer");
            if (layerValue != null)
            {
                try
                {
                    Type? layerMaskType = FindType("UnityEngine.LayerMask");
                    MethodInfo? layerToName = layerMaskType?.GetMethod(
                        "LayerToName", BindingFlags.Public | BindingFlags.Static,
                        null, new[] { typeof(int) }, null);
                    if (layerToName != null)
                    {
                        string layerName = Convert.ToString(
                            layerToName.Invoke(null, new object?[] { Convert.ToInt32(layerValue, CultureInfo.InvariantCulture) }),
                            CultureInfo.InvariantCulture) ?? string.Empty;
                        string layer = layerName.ToLowerInvariant();
                        if (waterTokens.Any(token => layer.Contains(token)))
                            return true;
                    }
                }
                catch
                {
                    // Layer-name lookup is a supplemental water-safety check.
                }
            }

            return false;
        }

        private static bool TryReadVector(object value, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            try
            {
                x = Convert.ToSingle(GetMemberValue(value, "x"), CultureInfo.InvariantCulture);
                y = Convert.ToSingle(GetMemberValue(value, "y"), CultureInfo.InvariantCulture);
                z = Convert.ToSingle(GetMemberValue(value, "z"), CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object? GetMemberValue(object instance, string name)
        {
            Type type = instance.GetType();
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property?.CanRead == true)
                return property.GetValue(instance, null);
            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(instance);
        }

        private static bool FindExistingRoot()
        {
            return FindGameObject(RootName) != null;
        }

        private static object? FindGameObject(string name)
        {
            Type? gameObject = FindType("UnityEngine.GameObject");
            MethodInfo? find = gameObject?.GetMethod("Find", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            return find?.Invoke(null, new object[] { name });
        }

        private static void CleanupActiveRoot()
        {
            if (_rootObject != null)
                DestroyGameObject(_rootObject);
            _rootObject = null;
            _rootTransform = null;
        }

        private static void CleanupGeneratedRoot(string name)
        {
            try
            {
                object? root = FindGameObject(name);
                if (root != null)
                    DestroyGameObject(root);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Could not clean up " + name + ": " + ex.Message);
            }
        }

        private static void DestroyGameObject(object gameObject)
        {
            Type? unityObject = FindType("UnityEngine.Object");
            MethodInfo? destroy = unityObject?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                    method.Name == "Destroy"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType.IsAssignableFrom(gameObject.GetType()));
            if (destroy == null)
                throw new MissingMethodException("UnityEngine.Object.Destroy(Object) was not found for rollback.");
            destroy.Invoke(null, new[] { gameObject });
        }

        private static void BuildWarehouse()
        {
            // Open-front warehouse shell: a real walk-in space with a wide vehicle bay.
            const float x = -7.0f;
            const float z = 1.2f;
            Add("Warehouse concrete floor", x, 0.18f, z, 12.5f, 0.24f, 12.0f, 0.48f, 0.47f, 0.43f);
            Add("Warehouse rear wall", x, 2.65f, z - 6.0f, 12.5f, 5.0f, 0.38f, 0.34f, 0.37f, 0.38f);
            Add("Warehouse left wall", x - 6.05f, 2.65f, z, 0.38f, 5.0f, 12.0f, 0.32f, 0.35f, 0.36f);
            Add("Warehouse right wall", x + 6.05f, 2.65f, z, 0.38f, 5.0f, 12.0f, 0.32f, 0.35f, 0.36f);
            Add("Warehouse front wall left", x - 4.1f, 2.65f, z + 5.8f, 4.3f, 5.0f, 0.38f, 0.30f, 0.33f, 0.34f);
            Add("Warehouse front wall right", x + 4.1f, 2.65f, z + 5.8f, 4.3f, 5.0f, 0.38f, 0.30f, 0.33f, 0.34f);
            Add("Warehouse loading bay lintel", x, 4.75f, z + 5.8f, 3.9f, 0.8f, 0.42f, 0.24f, 0.27f, 0.28f);
            Add("Warehouse roof", x, 5.25f, z, 13.0f, 0.35f, 12.7f, 0.22f, 0.25f, 0.26f);

            // Interior shelving, counter, crates and a clearly readable loading area.
            Add("Warehouse stock rack lower", x - 4.5f, 1.25f, z - 2.0f, 0.45f, 2.1f, 6.0f, 0.22f, 0.25f, 0.27f);
            Add("Warehouse stock rack upper", x - 4.5f, 2.55f, z - 2.0f, 0.48f, 0.16f, 6.0f, 0.72f, 0.37f, 0.13f);
            Add("Warehouse counter", x + 3.9f, 0.85f, z + 2.8f, 2.6f, 1.5f, 0.8f, 0.43f, 0.25f, 0.13f);
            Add("Warehouse cargo pallet A", x - 1.6f, 0.65f, z - 1.2f, 2.0f, 0.55f, 1.6f, 0.46f, 0.29f, 0.14f);
            Add("Warehouse cargo pallet B", x + 1.2f, 0.65f, z - 3.7f, 2.0f, 0.55f, 1.6f, 0.46f, 0.29f, 0.14f);
            Add("Warehouse sign backing", x, 4.55f, z + 6.05f, 4.5f, 0.8f, 0.12f, 0.12f, 0.19f, 0.22f);
            Add("Warehouse sign accent", x, 4.55f, z + 6.14f, 3.4f, 0.09f, 0.08f, 0.88f, 0.52f, 0.18f);

            // Windows are intentionally inset as solid glass-colored geometry for now.
            Add("Warehouse front window left", x - 4.55f, 3.65f, z + 6.02f, 1.4f, 1.0f, 0.08f, 0.28f, 0.62f, 0.72f);
            Add("Warehouse front window right", x + 4.55f, 3.65f, z + 6.02f, 1.4f, 1.0f, 0.08f, 0.28f, 0.62f, 0.72f);
        }

        private static void BuildContainers()
        {
            BuildContainer(8.2f, 1.25f, -6.8f, 7.0f, 2.5f, 2.8f, 0.63f, 0.22f, 0.15f, "Rust red");
            BuildContainer(8.2f, 1.25f, -3.65f, 7.0f, 2.5f, 2.8f, 0.16f, 0.31f, 0.38f, "Blue");
            BuildContainer(8.2f, 3.85f, -6.8f, 7.0f, 2.5f, 2.8f, 0.55f, 0.56f, 0.49f, "Upper sand");
        }

        private static void BuildContainer(float x, float y, float z, float width, float height, float depth,
            float r, float g, float b, string label)
        {
            Add(label + " shipping container body", x, y, z, width, height, depth, r, g, b);
            for (int i = 0; i < 13; i++)
            {
                float ribX = x - width * 0.46f + i * (width * 0.92f / 12f);
                Add(label + " container rib " + i, ribX, y, z + depth * 0.505f, 0.055f, height * 0.9f, 0.08f,
                    Clamp(r + 0.14f), Clamp(g + 0.14f), Clamp(b + 0.14f));
            }
            Add(label + " container locking bar", x + width * 0.44f, y, z + depth * 0.56f, 0.12f, height * 0.92f, 0.1f, 0.17f, 0.18f, 0.18f);
        }

        private static void BuildCrane()
        {
            const float x = 14.1f;
            const float z = -8.2f;
            Add("Quay crane rear leg", x - 2.7f, 4.2f, z - 1.4f, 0.6f, 8.4f, 0.7f, 0.86f, 0.48f, 0.13f);
            Add("Quay crane front leg", x + 2.7f, 4.2f, z - 1.4f, 0.6f, 8.4f, 0.7f, 0.86f, 0.48f, 0.13f);
            Add("Quay crane top beam", x, 8.35f, z - 1.4f, 6.0f, 0.65f, 0.75f, 0.86f, 0.48f, 0.13f);
            Add("Quay crane boom", x, 8.65f, z + 3.0f, 0.7f, 0.55f, 9.0f, 0.86f, 0.48f, 0.13f);
            Add("Quay crane hanging cable", x, 5.5f, z + 5.6f, 0.12f, 5.7f, 0.12f, 0.12f, 0.13f, 0.13f);
            Add("Quay crane hook", x, 2.55f, z + 5.6f, 0.85f, 0.35f, 0.55f, 0.16f, 0.17f, 0.17f);
        }

        private static void BuildStreetFurniture()
        {
            for (int i = 0; i < 4; i++)
            {
                float z = -9.0f + i * 6.0f;
                Add("Harbor lamp post " + i, 5.9f, 3.0f, z, 0.18f, 6.0f, 0.18f, 0.18f, 0.2f, 0.21f);
                Add("Harbor lamp head " + i, 5.9f, 6.1f, z, 1.0f, 0.18f, 0.34f, 0.88f, 0.76f, 0.48f);
                Add("Harbor bollard " + i, 2.4f, 0.55f, z, 0.42f, 1.1f, 0.42f, 0.77f, 0.7f, 0.52f);
            }

            Add("Dock safety barrier", 2.9f, 0.75f, 11.7f, 0.16f, 1.5f, 5.5f, 0.82f, 0.52f, 0.12f);
            Add("Dock notice board", -1.2f, 1.75f, 11.2f, 1.9f, 2.3f, 0.18f, 0.16f, 0.24f, 0.27f);
            Add("Market crate stack lower", -12.5f, 0.7f, -9.4f, 1.5f, 1.2f, 1.4f, 0.46f, 0.28f, 0.13f);
            Add("Market crate stack upper", -12.5f, 1.85f, -9.4f, 1.3f, 1.1f, 1.2f, 0.54f, 0.34f, 0.16f);
        }

        private static void BuildQuay()
        {
            Add("Concrete quay edge", 18.8f, 0.2f, 0f, 1.0f, 0.4f, 28f, 0.48f, 0.49f, 0.47f);
            Add("Quay warning stripe", 18.15f, 0.42f, 0f, 0.12f, 0.06f, 27f, 0.88f, 0.68f, 0.16f);
            Add("Pier platform", 22.3f, -0.02f, -2.8f, 6.0f, 0.45f, 12.0f, 0.36f, 0.29f, 0.22f);
            for (int i = 0; i < 5; i++)
                Add("Pier piling " + i, 20.0f + i * 1.15f, -1.15f, -8.0f, 0.38f, 2.4f, 0.38f, 0.28f, 0.22f, 0.17f);
        }

        private static void Add(string name, float x, float y, float z, float sx, float sy, float sz,
            float r, float g, float b)
        {
            object? cube = null;
            bool parented = false;
            try
            {
                object primitiveValue = Enum.Parse(_primitiveType!, "Cube");
                MethodInfo create = _gameObjectType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(method => method.Name == "CreatePrimitive"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == _primitiveType);
                cube = create.Invoke(null, new[] { primitiveValue })
                    ?? throw new InvalidOperationException("GameObject.CreatePrimitive returned null.");
                SetProperty(cube, "name", name);

                object transform = GetProperty(cube, "transform")
                    ?? throw new MissingMemberException("Created primitive has no Transform component.");
                MethodInfo? setParent = transform.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(method => method.Name == "SetParent"
                        && method.GetParameters().Length == 2
                        && method.GetParameters()[0].ParameterType.IsAssignableFrom(_rootTransform!.GetType()));
                if (setParent == null)
                    throw new MissingMethodException("Transform.SetParent(Transform, bool) was not found.");

                setParent.Invoke(transform, new[] { _rootTransform, (object)false });
                parented = true;

                SetProperty(transform, "localPosition", NewVector3(x, y, z));
                SetProperty(transform, "localScale", NewVector3(sx, sy, sz));
                SetProperty(transform, "localRotation", NewYawQuaternion(0f));
                SetMaterialColor(cube, r, g, b);
            }
            catch
            {
                // If parenting failed, the primitive is outside the pending root
                // and must be destroyed explicitly to avoid an orphan in the map.
                if (cube != null && !parented)
                {
                    try { DestroyGameObject(cube); }
                    catch (Exception cleanupError)
                    {
                        MelonLogger.Warning("[Schedule I Mega Expansion] Could not clean up orphaned primitive '" + name + "': " + cleanupError.Message);
                    }
                }
                throw;
            }
        }

        private static void CreateRoot(Anchor anchor, out object rootObject, out object rootTransform)
        {
            rootObject = Activator.CreateInstance(_gameObjectType!, new object[] { PendingRootName })!;
            rootTransform = GetProperty(rootObject, "transform")!;
            SetProperty(rootTransform, "position", NewVector3(anchor.X, anchor.Y, anchor.Z));
            SetProperty(rootTransform, "rotation", NewYawQuaternion(anchor.Yaw));
        }

        private static void SetMaterialColor(object gameObject, float r, float g, float b)
        {
            try
            {
                object renderer = GetComponent(gameObject, _meshRendererType!);
                object material = GetProperty(renderer, "material")!;
                SetProperty(material, "color", Activator.CreateInstance(_colorType!, new object[] { r, g, b, 1f }));
            }
            catch
            {
                // The primitive still has its default visible material and collider.
            }
        }

        private static object GetComponent(object gameObject, Type componentType)
        {
            MethodInfo method = _gameObjectType!.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(item => item.Name == "GetComponent"
                    && item.GetParameters().Length == 1
                    && item.GetParameters()[0].ParameterType == typeof(Type));
            return method.Invoke(gameObject, new object[] { componentType })!;
        }

        private static bool IsConfirmedSinglePlayer(out int playerCount)
        {
            playerCount = 0;

            // S1API exposes wrapped players through S1API.Entities.Player.All.
            // Do not probe the underlying ScheduleOne Player type: it does not
            // expose the public PlayerList contract used by this mod.
            Type? playerType = FindType("S1API.Entities.Player");
            if (playerType == null)
                return false;

            object? players = playerType.GetField("All",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
                ?? GetStaticProperty(playerType, "All");
            if (players == null)
                return false;

            object? count = GetMemberValue(players, "Count");
            if (count == null)
                return false;

            playerCount = Convert.ToInt32(count, CultureInfo.InvariantCulture);
            // Exactly one fully registered S1API player confirms single-player.
            // Zero means the wrapper registry is not ready; >1 means multiplayer.
            return playerCount == 1;
        }

        private static void EnsureUnityTypes()
        {
            _gameObjectType ??= FindType("UnityEngine.GameObject")
                ?? throw new TypeLoadException("UnityEngine.GameObject was not found.");
            _primitiveType ??= FindType("UnityEngine.PrimitiveType")
                ?? throw new TypeLoadException("UnityEngine.PrimitiveType was not found.");
            _vector3Type ??= FindType("UnityEngine.Vector3")
                ?? throw new TypeLoadException("UnityEngine.Vector3 was not found.");
            _quaternionType ??= FindType("UnityEngine.Quaternion")
                ?? throw new TypeLoadException("UnityEngine.Quaternion was not found.");
            _colorType ??= FindType("UnityEngine.Color")
                ?? throw new TypeLoadException("UnityEngine.Color was not found.");
            _meshRendererType ??= FindType("UnityEngine.MeshRenderer")
                ?? throw new TypeLoadException("UnityEngine.MeshRenderer was not found.");
        }

        private static object NewVector3(float x, float y, float z)
        {
            return Activator.CreateInstance(_vector3Type!, new object[] { x, y, z })!;
        }

        private static object NewYawQuaternion(float yaw)
        {
            MethodInfo method = _quaternionType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(item => item.Name == "Euler"
                    && item.GetParameters().Length == 3
                    && item.GetParameters().All(parameter => parameter.ParameterType == typeof(float)));
            return method.Invoke(null, new object[] { 0f, yaw, 0f })!;
        }

        private static float ForwardX(float yaw) => (float)Math.Sin(yaw * Math.PI / 180.0);
        private static float ForwardZ(float yaw) => (float)Math.Cos(yaw * Math.PI / 180.0);
        private static float Clamp(float value) => Math.Max(0.04f, Math.Min(0.98f, value));
        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static object? GetProperty(object instance, string name)
        {
            return instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(instance, null);
        }

        private static object? GetStaticProperty(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                ?.GetValue(null, null);
        }

        private static void SetProperty(object instance, string name, object? value)
        {
            PropertyInfo? property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property?.CanWrite != true)
                throw new MissingMemberException(instance.GetType().FullName, name + " writable property");
            property.SetValue(instance, value, null);
        }

        private static float ReadFloat(object instance, string name)
        {
            object? value = instance.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance)
                ?? instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance, null);
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
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
    }
}