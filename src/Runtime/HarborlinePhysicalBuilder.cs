using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MelonLoader;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// Builds a first physical Harborline dockyard slice from Unity primitives.
    /// Every block is a real GameObject with a MeshRenderer and primitive collider.
    /// The slice is deliberately placed from a player-captured anchor instead of
    /// guessing world coordinates from a 2D map.
    ///
    /// This is a geometry milestone, not yet a native registered Building/Property.
    /// It is intentionally disabled in multiplayer until network spawning is added.
    /// </summary>
    internal static class HarborlinePhysicalBuilder
    {
        private const string RootName = "ScheduleIMegaExpansion_Harborline_PhysicalPreview";
        private static bool _built;
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

        public static void ResetForNewSave()
        {
            _built = false;
            _rootTransform = null;
        }

        public static void CaptureAnchor(string outputDirectory, Action<string> log, Action<string> warn)
        {
            try
            {
                if (!TryGetCameraPose(out Anchor anchor))
                    throw new InvalidOperationException("Main camera is not available. Load into the playable world first.");

                Directory.CreateDirectory(outputDirectory);
                string path = Path.Combine(outputDirectory, "harborline-anchor.json");
                var payload = new
                {
                    schemaVersion = 1,
                    capturedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    source = "Main camera position; floor height approximated by cameraY - 1.6m",
                    nativeRegionTarget = "Docks",
                    x = anchor.X,
                    y = anchor.Y,
                    z = anchor.Z,
                    rotationY = anchor.Yaw,
                    note = "Capture while standing at the intended Harborline connection point inside Docks. This is a measured in-game anchor, not an inferred map coordinate."
                };

                File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
                log("Harborline anchor captured at X=" + F(anchor.X) + " Y=" + F(anchor.Y) + " Z=" + F(anchor.Z) + " yaw=" + F(anchor.Yaw));
                log("Anchor file: " + path);
            }
            catch (Exception ex)
            {
                warn("Could not capture Harborline anchor: " + ex.Message);
            }
        }

        public static void BuildPhysicalSlice(string outputDirectory, Action<string> log, Action<string> warn)
        {
            if (_built)
            {
                warn("Harborline physical slice is already built in this session. Reload the save before rebuilding.");
                return;
            }

            try
            {
                if (IsMultiplayerSession())
                {
                    warn("Physical prototype is currently single-player only. No unsynchronized world geometry was spawned.");
                    return;
                }

                Anchor anchor;
                string anchorPath = Path.Combine(outputDirectory, "harborline-anchor.json");
                if (!TryReadAnchor(anchorPath, out anchor))
                {
                    if (!TryGetCameraPose(out anchor))
                        throw new InvalidOperationException("No anchor file and no playable main camera. Press F6 in the target Docks location first.");
                    warn("No saved anchor was found; using the current player-facing direction as a temporary prototype anchor.");
                }

                EnsureUnityTypes();
                CreateRoot(anchor, out object rootObject, out object rootTransform);
                _rootTransform = rootTransform;

                // The center is intentionally offset forward so the player does not
                // spawn inside the footprint when capturing the anchor.
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

                _built = true;
                log("Harborline physical dockyard slice spawned. Real Unity GameObjects, visible meshes and primitive colliders were created.");
                log("Created under root object: " + RootName);
                log("This first slice is physical geometry only; native building/property/shop registration, doors, NPCs and multiplayer replication are separate implementation stages.");
            }
            catch (Exception ex)
            {
                warn("Harborline physical build failed: " + ex.GetType().Name + ": " + ex.Message);
                warn(ex.StackTrace ?? string.Empty);
            }
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
            object primitiveValue = Enum.Parse(_primitiveType!, "Cube");
            MethodInfo create = _gameObjectType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(method => method.Name == "CreatePrimitive"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == _primitiveType);
            object cube = create.Invoke(null, new[] { primitiveValue })!;
            SetProperty(cube, "name", name);

            object transform = GetProperty(cube, "transform")!;
            MethodInfo? setParent = transform.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(method => method.Name == "SetParent"
                    && method.GetParameters().Length == 2
                    && method.GetParameters()[0].ParameterType.IsAssignableFrom(_rootTransform!.GetType()));
            if (setParent == null)
                throw new MissingMethodException("Transform.SetParent(Transform, bool) was not found.");
            setParent.Invoke(transform, new[] { _rootTransform, (object)false });

            SetProperty(transform, "localPosition", NewVector3(x, y, z));
            SetProperty(transform, "localScale", NewVector3(sx, sy, sz));
            SetProperty(transform, "localRotation", NewYawQuaternion(0f));
            SetMaterialColor(cube, r, g, b);
        }

        private static void CreateRoot(Anchor anchor, out object rootObject, out object rootTransform)
        {
            rootObject = Activator.CreateInstance(_gameObjectType!, new object[] { RootName })!;
            rootTransform = GetProperty(rootObject, "transform")!;
            SetProperty(rootTransform, "position", NewVector3(anchor.X + ForwardX(anchor.Yaw) * 22f, anchor.Y, anchor.Z + ForwardZ(anchor.Yaw) * 22f));
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

        private static bool TryGetCameraPose(out Anchor anchor)
        {
            anchor = default;
            Type? cameraType = FindType("UnityEngine.Camera");
            if (cameraType == null)
                return false;

            object? camera = GetStaticProperty(cameraType, "main");
            if (camera == null)
                return false;

            object? transform = GetProperty(camera, "transform");
            if (transform == null)
                return false;

            object? position = GetProperty(transform, "position");
            object? euler = GetProperty(transform, "eulerAngles");
            if (position == null || euler == null)
                return false;

            float x = ReadFloat(position, "x");
            float y = ReadFloat(position, "y") - 1.6f;
            float z = ReadFloat(position, "z");
            float yaw = ReadFloat(euler, "y");
            anchor = new Anchor(x, y, z, yaw);
            return true;
        }

        private static bool TryReadAnchor(string path, out Anchor anchor)
        {
            anchor = default;
            if (!File.Exists(path))
                return false;

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = document.RootElement;
            anchor = new Anchor(
                root.GetProperty("x").GetSingle(),
                root.GetProperty("y").GetSingle(),
                root.GetProperty("z").GetSingle(),
                root.GetProperty("rotationY").GetSingle());
            return true;
        }

        private static bool IsMultiplayerSession()
        {
            // Be conservative: any connected player list with more than one
            // participant means a local-only preview would desynchronize clients.
            Type? playerType = FindType("Player");
            if (playerType == null)
                return false;

            object? players = GetStaticProperty(playerType, "PlayerList");
            if (players == null)
                return false;

            object? count = GetProperty(players, "Count");
            return count != null && Convert.ToInt32(count, CultureInfo.InvariantCulture) > 1;
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
            if (property?.CanWrite == true)
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