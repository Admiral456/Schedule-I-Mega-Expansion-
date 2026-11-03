using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MelonLoader;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// Optional compile-time / required package-time bridge to DooDesch MapExpansions.
    /// The framework owns actual terrain placement, road graph joining, Realtor property
    /// copies, shop interiors/doors and multiplayer replication. This bridge only
    /// constructs public MapExpansions API definitions; it never clones private code.
    ///
    /// The user's first in-game scene survey must supply real scene names, road joins,
    /// property templates and measured coordinates before the expansion is registered.
    /// Empty/unmeasured config fails closed rather than guessing a map location.
    /// </summary>
    internal static class HarborlineMapExpansionsBridge
    {
        private const string ExpansionId = "harborline";
        private const string ApiVersion = "0.4.0";
        private static bool _attempted;
        private static bool _registered;
        private static string _configPath = string.Empty;

        public static bool IsRegistered => _registered;

        public static void RegisterBeforeMainMenu()
        {
            if (_attempted)
                return;
            _attempted = true;

            try
            {
                string userData = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "UserData",
                    "ScheduleIMegaExpansion",
                    "Harborline");
                Directory.CreateDirectory(userData);
                _configPath = Path.Combine(userData, "map-expansions.json");
                EnsureConfigTemplate(_configPath);

                Type? apiType = FindType("MapExpansions.MapExpansionsApi");
                if (apiType == null)
                {
                    MelonLogger.Warning("[Schedule I Mega Expansion] MapExpansions 1.0.0 API is not loaded. Install the declared DooDesch-MapExpansions dependency.");
                    return;
                }

                string apiVersion = Convert.ToString(
                    apiType.GetField("ApiVersion", BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue(),
                    CultureInfo.InvariantCulture) ?? string.Empty;
                if (!string.Equals(apiVersion, ApiVersion, StringComparison.Ordinal))
                {
                    MelonLogger.Warning("[Schedule I Mega Expansion] MapExpansions API version mismatch. Expected " + ApiVersion + ", loaded " + apiVersion + ". Registration skipped.");
                    return;
                }

                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(_configPath));
                JsonElement root = document.RootElement;
                if (!Validate(root, out string reason))
                {
                    MelonLogger.Msg("[Schedule I Mega Expansion] MapExpansions is installed, but Harborline is not registered yet: " + reason);
                    MelonLogger.Msg("[Schedule I Mega Expansion] Configure measured source scene, region polygon, road connection, Realtor templates and shop positions in: " + _configPath);
                    return;
                }

                object definition = BuildDefinition(root);
                MethodInfo? register = apiType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method => method.Name == "Register"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType.IsInstanceOfType(definition));
                if (register == null)
                    throw new MissingMethodException(apiType.FullName, "Register(ExpansionDefinition)");

                object? result = register.Invoke(null, new[] { definition });
                _registered = result is bool ok && ok;
                if (_registered)
                {
                    MelonLogger.Msg("[Schedule I Mega Expansion] Registered Harborline through DooDesch MapExpansions API " + apiVersion + ". The framework owns its scene capture, road joins, properties, shop copies and multiplayer build.");
                    SubscribeReady(apiType);
                }
                else
                {
                    string state = ReadExpansionState(apiType);
                    MelonLogger.Warning("[Schedule I Mega Expansion] MapExpansions rejected Harborline registration. State: " + state);
                }
            }
            catch (Exception ex)
            {
                Exception cause = ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                MelonLogger.Warning("[Schedule I Mega Expansion] MapExpansions registration failed safely: " + cause.GetType().Name + ": " + cause.Message);
            }
        }

        public static bool IsReady()
        {
            if (!_registered)
                return false;
            try
            {
                Type? apiType = FindType("MapExpansions.MapExpansionsApi");
                MethodInfo? ready = apiType?.GetMethod("IsReady", BindingFlags.Public | BindingFlags.Static);
                return ready != null && Convert.ToBoolean(ready.Invoke(null, new object[] { ExpansionId }), CultureInfo.InvariantCulture);
            }
            catch { return false; }
        }

        private static void EnsureConfigTemplate(string path)
        {
            if (File.Exists(path))
                return;

            const string resourceName = "ScheduleIMegaExpansion.map-expansions-template.json";
            using Stream? resource = typeof(HarborlineMapExpansionsBridge).Assembly.GetManifestResourceStream(resourceName);
            if (resource == null)
                throw new FileNotFoundException("Embedded MapExpansions config template was not found: " + resourceName);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var reader = new StreamReader(resource);
            File.WriteAllText(path, reader.ReadToEnd());
            MelonLogger.Msg("[Schedule I Mega Expansion] Created the editable MapExpansions template: " + path);
        }

        private static bool Validate(JsonElement root, out string reason)
        {
            reason = string.Empty;
            if (ReadInt(root, "schemaVersion") != 1)
            {
                reason = "unsupported schemaVersion";
                return false;
            }

            if (!string.Equals(ReadString(root, "id"), ExpansionId, StringComparison.Ordinal))
            {
                reason = "id must be '" + ExpansionId + "'";
                return false;
            }

            string mode = ReadString(root, "mode", "source-scene-expansion");
            bool sourceSceneMode = string.Equals(mode, "source-scene-expansion", StringComparison.OrdinalIgnoreCase);
            bool editLayerMode = string.Equals(mode, "main-world-edit-layer", StringComparison.OrdinalIgnoreCase);
            if (!sourceSceneMode && !editLayerMode)
            {
                reason = "mode must be 'source-scene-expansion' or 'main-world-edit-layer'";
                return false;
            }

            JsonElement source = Get(root, "source");
            string sceneName = ReadString(source, "sceneName");
            if (sourceSceneMode && string.IsNullOrWhiteSpace(sceneName))
            {
                reason = "source.sceneName is empty. Use native-scenes.csv to select a real source scene.";
                return false;
            }
            if (editLayerMode && !string.IsNullOrWhiteSpace(sceneName))
            {
                reason = "main-world-edit-layer must leave source.sceneName empty; it edits the main world in place and does not copy a terrain scene";
                return false;
            }

            JsonElement offset = Get(root, "offset");
            if (sourceSceneMode && !TryReadVector3(offset, out _, out _, out _))
            {
                reason = "source-scene-expansion needs a measured [x,y,z] world offset";
                return false;
            }
            if (editLayerMode && offset.ValueKind != JsonValueKind.Null
                && TryReadVector3(offset, out float ox, out float oy, out float oz)
                && (Math.Abs(ox) > 0.001f || Math.Abs(oy) > 0.001f || Math.Abs(oz) > 0.001f))
            {
                reason = "main-world-edit-layer has no source frame and therefore requires offset null or [0,0,0]";
                return false;
            }

            JsonElement regions = Get(root, "regions");
            if (regions.ValueKind != JsonValueKind.Array || regions.GetArrayLength() == 0)
            {
                reason = "at least one measured region polygon is required";
                return false;
            }
            foreach (JsonElement region in regions.EnumerateArray())
            {
                JsonElement polygon = Get(region, "polygon");
                if (polygon.ValueKind != JsonValueKind.Array || polygon.GetArrayLength() < 3)
                {
                    reason = "region '" + ReadString(region, "name") + "' needs at least three measured [x,z] vertices";
                    return false;
                }
                foreach (JsonElement vertex in polygon.EnumerateArray())
                {
                    if (!TryReadVector2(vertex, out _, out _))
                    {
                        reason = "region polygons must contain measured [x,z] pairs";
                        return false;
                    }
                }
            }

            JsonElement connections = Get(root, "connections");
            if (sourceSceneMode && (connections.ValueKind != JsonValueKind.Array || connections.GetArrayLength() == 0))
            {
                reason = "source-scene-expansion needs at least one measured Docks-to-Harborline connection; no isolated map island will be registered";
                return false;
            }
            if (editLayerMode && connections.ValueKind == JsonValueKind.Array && connections.GetArrayLength() > 0)
            {
                reason = "main-world-edit-layer cannot create connections; switch to source-scene-expansion after a source scene exists";
                return false;
            }
            foreach (JsonElement connection in Enumerate(connections))
            {
                JsonElement route = Get(connection, "route");
                if (route.ValueKind != JsonValueKind.Array || route.GetArrayLength() < 2)
                {
                    reason = "connection '" + ReadString(connection, "name") + "' needs at least two measured route points";
                    return false;
                }
                bool hasMain = false, hasExpansion = false;
                foreach (JsonElement point in route.EnumerateArray())
                {
                    hasMain |= string.Equals(ReadString(point, "side"), "Main", StringComparison.OrdinalIgnoreCase);
                    hasExpansion |= string.Equals(ReadString(point, "side"), "Expansion", StringComparison.OrdinalIgnoreCase);
                    if (!TryReadVector3(Get(point, "position"), out _, out _, out _))
                    {
                        reason = "connection route points need measured [x,y,z] positions";
                        return false;
                    }
                }
                if (!hasMain || !hasExpansion)
                {
                    reason = "each source-scene connection must join Main world points to Expansion-local points";
                    return false;
                }
            }

            JsonElement properties = Get(root, "properties");
            if (properties.ValueKind != JsonValueKind.Array || properties.GetArrayLength() == 0)
            {
                reason = "at least one Realtor property template is required";
                return false;
            }
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonElement property in properties.EnumerateArray())
            {
                string code = ReadString(property, "code");
                if (string.IsNullOrWhiteSpace(code) || !codes.Add(code))
                {
                    reason = "each Realtor property needs a non-empty unique code";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(ReadString(property, "template")))
                {
                    reason = "property '" + code + "' needs a verified template propertyCode from native-properties.csv/native-businesses.csv";
                    return false;
                }
                if (!TryReadVector3(Get(property, "root"), out _, out _, out _))
                {
                    reason = "property '" + code + "' needs a measured " +
                        (editLayerMode ? "main-world" : "expansion-local") + " root position";
                    return false;
                }
            }

            foreach (JsonElement shop in Enumerate(Get(root, "shops")))
            {
                if (string.IsNullOrWhiteSpace(ReadString(shop, "id"))
                    || string.IsNullOrWhiteSpace(ReadString(shop, "seller"))
                    || string.IsNullOrWhiteSpace(ReadString(shop, "shopCode")))
                {
                    reason = "each configured shop needs a stable id, a real native shopCode from native-shops.csv, and a registered seller NPC id";
                    return false;
                }
                if (!TryReadVector3(Get(shop, "counter"), out _, out _, out _))
                {
                    reason = "shop '" + ReadString(shop, "id") + "' needs a measured counter position";
                    return false;
                }
            }

            return true;
        }

        private static object BuildDefinition(JsonElement json)
        {
            object definition = New("ExpansionDefinition");
            Set(definition, "Id", ReadString(json, "id"));
            Set(definition, "DisplayName", ReadString(json, "displayName"));
            Set(definition, "Version", ReadString(json, "version"));
            Set(definition, "QuarterTurns", ReadInt(json, "quarterTurns"));
            Set(definition, "MinimumWorldHeight", ReadFloat(json, "minimumWorldHeight", -5f));
            Set(definition, "SeparateViews", ReadBool(json, "separateViews", false));
            JsonElement offset = Get(json, "offset");
            Set(definition, "Offset",
                TryReadVector3(offset, out float ox, out float oy, out float oz)
                    ? MakeVector3(ox, oy, oz)
                    : MakeVector3(0f, 0f, 0f));

            JsonElement sourceJson = Get(json, "source");
            string sceneName = ReadString(sourceJson, "sceneName");
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                object source = New("WorldSource");
                Set(source, "SceneName", sceneName);
                Set(source, "RootName", ReadString(sourceJson, "rootName", "Map"));
                Set(source, "RoadNodesName", ReadString(sourceJson, "roadNodesName", "RoadNodes"));
                AddStrings(source, "TestedGameVersions", Get(sourceJson, "testedGameVersions"));
                Set(definition, "Source", source);
            }
            // With Source=null, MapExpansions treats the definition as an edit layer
            // in the main world. It has no separate terrain/connection but can still
            // copy native properties, shop interiors/doors and world objects.

            AddRegions(definition, Get(json, "regions"));
            AddConnections(definition, Get(json, "connections"));
            AddLocations(definition, Get(json, "locations"));
            AddBuildings(definition, Get(json, "buildings"));
            AddParkingLots(definition, Get(json, "parkingLots"));
            AddDealLocations(definition, Get(json, "dealLocations"));
            AddProperties(definition, Get(json, "properties"));
            AddShops(definition, Get(json, "shops"));
            AddCopies(definition, Get(json, "copies"));
            AddMapSheet(definition, Get(json, "map"));
            return definition;
        }

        private static void AddRegions(object definition, JsonElement rows)
        {
            foreach (JsonElement row in Enumerate(rows))
            {
                object region = New("RegionDefinition");
                Set(region, "Name", ReadString(row, "name"));
                Set(region, "StandInRegion", ReadInt(row, "standInRegion", 3));
                Set(region, "UnlockedByDefault", ReadBool(row, "unlockedByDefault", true));
                IList polygon = List(region, "Polygon");
                foreach (JsonElement vertex in Enumerate(Get(row, "polygon")))
                {
                    if (TryReadVector2(vertex, out float x, out float z))
                        polygon.Add(MakeVector2(x, z));
                }
                List(definition, "Regions").Add(region);
            }
        }

        private static void AddConnections(object definition, JsonElement rows)
        {
            foreach (JsonElement row in Enumerate(rows))
            {
                object connection = New("ConnectionDefinition");
                Set(connection, "Name", ReadString(row, "name"));
                Set(connection, "RoadWidth", ReadFloat(row, "roadWidth", 8f));
                Set(connection, "WalkwayWidth", ReadFloat(row, "walkwayWidth", 1.4f));
                Set(connection, "CurbHeight", ReadFloat(row, "curbHeight", 0.18f));
                Set(connection, "GuardHeight", ReadFloat(row, "guardHeight", 0.9f));
                Set(connection, "MedianHeight", ReadFloat(row, "medianHeight", 0.8f));
                Set(connection, "ApproachLength", ReadFloat(row, "approachLength", 6f));

                foreach (JsonElement point in Enumerate(Get(row, "route")))
                {
                    if (!TryReadVector3(Get(point, "position"), out float x, out float y, out float z))
                        continue;
                    string side = ReadString(point, "side");
                    object pathPoint = CreatePathPoint(side, x, y, z);
                    object style = EnumValue("RouteStyle", ReadString(point, "style", "Road"));
                    MethodInfo? add = connection.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(method => method.Name == "Add" && method.GetParameters().Length == 2);
                    if (add == null)
                        throw new MissingMethodException(connection.GetType().FullName, "Add(PathPoint, RouteStyle)");
                    add.Invoke(connection, new[] { pathPoint, style });
                }

                JsonElement join = Get(row, "vehicleJoin");
                if (TryReadVector3(join, out float jx, out float jy, out float jz))
                    Set(connection, "VehicleJoin", CreatePathPoint("Main", jx, jy, jz));

                List(definition, "Connections").Add(connection);
            }
        }

        private static void AddLocations(object definition, JsonElement rows)
        {
            foreach (JsonElement row in Enumerate(rows))
            {
                if (!TryReadVector3(Get(row, "position"), out float x, out float y, out float z))
                    continue;
                object location = New("LocationDefinition", ReadString(row, "name"), MakeVector3(x, y, z),
                    ReadFloat(row, "yaw", 0f));
                List(definition, "Locations").Add(location);
            }
        }

        private static void AddBuildings(object definition, JsonElement rows)
        {
            foreach (JsonElement row in Enumerate(rows))
            {
                object building = New("BuildingDefinition");
                Set(building, "Id", ReadString(row, "id"));
                Set(building, "Name", ReadString(row, "name"));
                Set(building, "Knock", ReadBool(row, "knock", false));
                foreach (JsonElement door in Enumerate(Get(row, "doors")))
                {
                    if (TryReadVector3(Get(door, "position"), out float x, out float y, out float z))
                        List(building, "Doors").Add(MakeLocalPoint(x, y, z, ReadFloat(door, "yaw", 0f)));
                    string model = ReadString(door, "model");
                    if (!string.IsNullOrWhiteSpace(model))
                        List(building, "DoorModels").Add(model);
                }
                List(definition, "Buildings").Add(building);
            }
        }

        private static void AddParkingLots(object definition, JsonElement rows)
        {
            foreach (JsonElement row in Enumerate(rows))
            {
                object lot = New("ParkingLotDefinition");
                Set(lot, "Id", ReadString(row, "id"));
                Set(lot, "Name", ReadString(row, "name"));
                if (TryReadVector3(Get(row, "entry"), out float ex, out float ey, out float ez))
                    Set(lot, "Entry", MakeLocalPoint(ex, ey, ez, ReadFloat(row, "entryYaw", 0f)));
                foreach (JsonElement spot in Enumerate(Get(row, "spots")))
                    if (TryReadVector3(Get(spot, "position"), out float x, out float y, out float z))
                        List(lot, "Spots").Add(MakeLocalPoint(x, y, z, ReadFloat(spot, "yaw", 0f)));
                List(definition, "ParkingLots").Add(lot);
            }
        }

        private static void AddDealLocations(object definition, JsonElement rows)
        {
            foreach (JsonElement row in Enumerate(rows))
            {
                object deal = New("DealLocationDefinition");
                Set(deal, "Id", ReadString(row, "id"));
                Set(deal, "Name", ReadString(row, "name"));
                Set(deal, "Description", ReadString(row, "description"));
                Set(deal, "Region", ReadString(row, "region"));
                if (TryReadVector3(Get(row, "standPoint"), out float x, out float y, out float z))
                    Set(deal, "StandPoint", MakeLocalPoint(x, y, z, ReadFloat(row, "yaw", 0f)));
                List(definition, "DealLocations").Add(deal);
            }
        }

        private static void AddProperties(object definition, JsonElement rows)
        {
            foreach (JsonElement row in Enumerate(rows))
            {
                object property = New("PropertyDefinition");
                Set(property, "Id", ReadString(row, "id"));
                Set(property, "Code", ReadString(row, "code"));
                Set(property, "Name", ReadString(row, "name"));
                Set(property, "Template", ReadString(row, "template"));
                Set(property, "SoldByRealtor", ReadBool(row, "soldByRealtor", true));
                Set(property, "RealtorText", ReadString(row, "realtorText"));
                if (Has(row, "price") && Get(row, "price").ValueKind == JsonValueKind.Number)
                    Set(property, "Price", ReadFloat(row, "price"));
                if (Has(row, "employeeCapacity") && Get(row, "employeeCapacity").ValueKind == JsonValueKind.Number)
                    Set(property, "EmployeeCapacity", ReadInt(row, "employeeCapacity"));
                if (TryReadVector3(Get(row, "root"), out float x, out float y, out float z))
                    Set(property, "Root", MakeLocalPoint(x, y, z, ReadFloat(row, "yaw", 0f)));
                Set(property, "Mirrored", ReadBool(row, "mirrored", false));
                AddStrings(property, "Exclude", Get(row, "exclude"));
                List(definition, "Properties").Add(property);
            }
        }

        private static void AddShops(object definition, JsonElement rows)
        {
            foreach (JsonElement row in Enumerate(rows))
            {
                object shop = New("ShopDefinition");
                Set(shop, "Id", ReadString(row, "id"));
                Set(shop, "Kind", EnumValue("ShopKind", ReadString(row, "kind", "Store")));
                Set(shop, "ShopCode", ReadString(row, "shopCode"));
                Set(shop, "Seller", ReadString(row, "seller"));
                Set(shop, "ChoiceText", ReadString(row, "choiceText"));
                Set(shop, "CounterReach", ReadFloat(row, "counterReach", 1.5f));
                if (TryReadVector3(Get(row, "counter"), out float x, out float y, out float z))
                    Set(shop, "Counter", MakeLocalPoint(x, y, z, ReadFloat(row, "counterYaw", 0f)));
                AddCopies(shop, "Copies", Get(row, "copies"));
                List(definition, "Shops").Add(shop);
            }
        }

        private static void AddCopies(object definition, JsonElement rows)
        {
            foreach (JsonElement row in Enumerate(rows))
            {
                object copy = CreateCopy(row);
                List(definition, "Copies").Add(copy);
            }
        }

        private static void AddMapSheet(object definition, JsonElement map)
        {
            string pngPath = ReadString(map, "pngPath");
            if (string.IsNullOrWhiteSpace(pngPath))
                return;
            if (!Path.IsPathRooted(pngPath) && !string.IsNullOrWhiteSpace(_configPath))
                pngPath = Path.Combine(Path.GetDirectoryName(_configPath)!, pngPath);
            if (!File.Exists(pngPath))
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] MapExpansions map PNG was not found: " + pngPath);
                return;
            }

            object sheet = New("MapSheetDefinition");
            Set(sheet, "Picture", EnumValue("MapPictureSource", "Png"));
            Set(sheet, "Png", File.ReadAllBytes(pngPath));
            if (TryReadVector2(Get(map, "centre"), out float x, out float z))
                Set(sheet, "Centre", MakeVector2(x, z));
            Set(sheet, "HalfSize", ReadFloat(map, "halfSize", 0f));

            foreach (JsonElement label in Enumerate(Get(map, "labels")))
            {
                if (!TryReadVector2(Get(label, "position"), out float lx, out float lz))
                    continue;
                object style = EnumValue("MapLabelStyle", ReadString(label, "style", "Building"));
                Type labelType = ApiType("MapLabel");
                object mapLabel = Activator.CreateInstance(labelType,
                    new[] { (object)ReadString(label, "text"), MakeVector2(lx, lz), style })!;
                List(sheet, "Labels").Add(mapLabel);
            }
            Set(definition, "Map", sheet);
        }

        private static object CreateCopy(JsonElement row)
        {
            object copy = New("CopyDefinition");
            Set(copy, "Id", ReadString(row, "id"));
            Set(copy, "Template", ReadString(row, "template"));
            Set(copy, "Frame", ReadString(row, "frame"));
            Set(copy, "TemplateSide", EnumValue("WorldSide", ReadString(row, "templateSide", "Main")));
            Set(copy, "OwnMesh", ReadBool(row, "ownMesh", true));
            Set(copy, "Scale", ReadFloat(row, "scale", 1f));
            Set(copy, "VisualOnly", ReadBool(row, "visualOnly", false));
            if (TryReadVector3(Get(row, "at"), out float x, out float y, out float z))
                Set(copy, "At", MakeLocalPoint(x, y, z, ReadFloat(row, "yaw", 0f)));
            string prefab = ReadString(row, "prefab");
            if (!string.IsNullOrWhiteSpace(prefab))
                Set(copy, "Prefab", prefab);
            string resource = ReadString(row, "resource");
            if (!string.IsNullOrWhiteSpace(resource))
                Set(copy, "Resource", resource);
            return copy;
        }

        private static object CreatePathPoint(string side, float x, float y, float z)
        {
            Type type = ApiType("PathPoint");
            string methodName = string.Equals(side, "Expansion", StringComparison.OrdinalIgnoreCase)
                ? "Expansion" : "Main";
            MethodInfo? factory = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(float), typeof(float), typeof(float) }, null);
            if (factory == null)
                throw new MissingMethodException(type.FullName, methodName + "(float,float,float)");
            return factory.Invoke(null, new object[] { x, y, z })!;
        }

        private static object MakeLocalPoint(float x, float y, float z, float yaw)
        {
            Type type = ApiType("LocalPoint");
            Type vector = UnityType("Vector3");
            return Activator.CreateInstance(type, new[] { MakeVector3(x, y, z), (object)yaw })!;
        }

        private static object MakeVector3(float x, float y, float z)
        {
            Type vector = UnityType("Vector3");
            return Activator.CreateInstance(vector, new object[] { x, y, z })!;
        }

        private static object Vector3(JsonElement value)
        {
            if (!TryReadVector3(value, out float x, out float y, out float z))
                throw new InvalidDataException("Expected [x,y,z] Vector3.");
            return MakeVector3(x, y, z);
        }

        private static object MakeVector2(float x, float z)
        {
            Type vector = UnityType("Vector2");
            return Activator.CreateInstance(vector, new object[] { x, z })!;
        }

        private static Type UnityType(string name)
        {
            return FindType("UnityEngine." + name)
                ?? throw new TypeLoadException("UnityEngine." + name + " was not found.");
        }

        private static Type ApiType(string name)
        {
            return FindType("MapExpansions." + name)
                ?? throw new TypeLoadException("MapExpansions." + name + " was not found.");
        }

        private static object New(string typeName, params object[] args)
        {
            Type type = ApiType(typeName);
            return Activator.CreateInstance(type, args)
                ?? throw new InvalidOperationException("Could not create MapExpansions." + typeName);
        }

        private static object EnumValue(string typeName, string name)
        {
            Type type = ApiType(typeName);
            return Enum.Parse(type, name, true);
        }

        private static IList List(object target, string propertyName)
        {
            PropertyInfo? property = target.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                throw new MissingMemberException(target.GetType().FullName, propertyName);
            object? list = property.GetValue(target, null);
            return list as IList ?? throw new InvalidOperationException(propertyName + " is not an IList.");
        }

        private static void AddStrings(object target, string propertyName, JsonElement values)
        {
            foreach (JsonElement value in Enumerate(values))
            {
                if (value.ValueKind == JsonValueKind.String)
                    List(target, propertyName).Add(value.GetString());
            }
        }

        private static void Set(object target, string propertyName, object? value)
        {
            PropertyInfo? property = target.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanWrite == true)
            {
                property.SetValue(target, value, null);
                return;
            }
            FieldInfo? field = target.GetType().GetField(propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            if (field != null && !field.IsInitOnly)
            {
                field.SetValue(target, value);
                return;
            }
            throw new MissingMemberException(target.GetType().FullName, propertyName);
        }

        private static string ReadExpansionState(Type apiType)
        {
            try
            {
                MethodInfo? getState = apiType.GetMethod("GetState", BindingFlags.Public | BindingFlags.Static);
                return getState?.Invoke(null, new object[] { ExpansionId })?.ToString() ?? "unknown";
            }
            catch { return "unknown"; }
        }

        private static void SubscribeReady(Type apiType)
        {
            try
            {
                EventInfo? ready = apiType.GetEvent("ExpansionReady", BindingFlags.Public | BindingFlags.Static);
                if (ready?.EventHandlerType == null)
                    return;
                MethodInfo handlerMethod = typeof(HarborlineMapExpansionsBridge).GetMethod(
                    nameof(OnExpansionReady),
                    BindingFlags.NonPublic | BindingFlags.Static)!;
                Delegate handler = Delegate.CreateDelegate(ready.EventHandlerType, handlerMethod);
                ready.AddEventHandler(null, handler);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Could not subscribe to MapExpansions readiness: " + ex.Message);
            }
        }

        private static void OnExpansionReady(string id)
        {
            if (string.Equals(id, ExpansionId, StringComparison.Ordinal))
                MelonLogger.Msg("[Schedule I Mega Expansion] MapExpansions reports Harborline Ready: terrain, road joins, native properties and configured content are placed.");
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

        private static JsonElement Get(JsonElement element, string name)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement value)
                ? value : default;
        }

        private static bool Has(JsonElement element, string name) =>
            element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out _);

        private static string ReadString(JsonElement element, string name, string fallback = "")
        {
            JsonElement value = Get(element, name);
            return value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;
        }

        private static int ReadInt(JsonElement element, string name, int fallback = 0)
        {
            JsonElement value = Get(element, name);
            return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result) ? result : fallback;
        }

        private static float ReadFloat(JsonElement element, string name, float fallback = 0f)
        {
            JsonElement value = Get(element, name);
            return value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float result) ? result : fallback;
        }

        private static bool ReadBool(JsonElement element, string name, bool fallback = false)
        {
            JsonElement value = Get(element, name);
            return value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;
        }

        private static bool TryReadVector3(JsonElement value, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 3)
                return false;
            try
            {
                x = value[0].GetSingle();
                y = value[1].GetSingle();
                z = value[2].GetSingle();
                return true;
            }
            catch { return false; }
        }

        private static bool TryReadVector2(JsonElement value, out float x, out float z)
        {
            x = z = 0f;
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2)
                return false;
            try
            {
                x = value[0].GetSingle();
                z = value[1].GetSingle();
                return true;
            }
            catch { return false; }
        }

        private static IEnumerable<JsonElement> Enumerate(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.Array)
                return Array.Empty<JsonElement>();
            return value.EnumerateArray().ToArray();
        }
    }
}
