using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// Reuses only render geometry from already loaded vanilla scene objects.
    /// It copies MeshFilter/MeshRenderer and Light data into fresh local GameObjects.
    /// It deliberately does NOT clone colliders, MonoBehaviours, InteractableObjects,
    /// NPCEnterableBuilding, DoorController or FishNet NetworkObjects.
    /// This follows the vanilla-mesh reuse approach observed in Daffy's Hills,
    /// without copying that mod's code or assets.
    /// </summary>
    internal static class HarborlineNativeVisualReuse
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type? _gameObjectType;
        private static Type? _transformType;
        private static Type? _meshFilterType;
        private static Type? _meshRendererType;
        private static Type? _lightType;
        private static Type? _quaternionType;
        private static Type? _vector3Type;
        private static Type? _unityObjectType;

        public static bool TryCreateVisualClone(
            string sourceHierarchyPath,
            object targetParentTransform,
            string cloneName,
            float x,
            float y,
            float z,
            float yaw,
            Action<string> log,
            Action<string> warn)
        {
            object? cloneRoot = null;
            try
            {
                EnsureTypes();
                MethodInfo find = _gameObjectType!.GetMethods(PublicStatic)
                    .First(method => method.Name == "Find"
                        && method.GetParameters().Length == 1
                        && method.GetParameters()[0].ParameterType == typeof(string));

                object? source = find.Invoke(null, new object[] { sourceHierarchyPath });
                if (source == null)
                    return false;

                cloneRoot = CreateGameObject(cloneName);
                object rootTransform = ReadMember(cloneRoot, "transform")
                    ?? throw new MissingMemberException("New visual root has no Transform.");

                ParentAndPlace(rootTransform, targetParentTransform, x, y, z, yaw);
                int copiedMeshes = 0;
                int copiedLights = 0;
                var visited = new HashSet<int>();
                object sourceTransform = ReadMember(source, "transform")
                    ?? throw new MissingMemberException("Native visual source has no Transform.");

                CopyHierarchy(sourceTransform, rootTransform, true, visited,
                    ref copiedMeshes, ref copiedLights);

                if (copiedMeshes == 0)
                {
                    DestroyObject(cloneRoot);
                    cloneRoot = null;
                    warn("Vanilla visual source '" + sourceHierarchyPath +
                        "' contained no copyable MeshFilter/MeshRenderer pairs; kept the procedural fallback.");
                    return false;
                }

                log("Reused " + copiedMeshes + " vanilla render meshes and " + copiedLights +
                    " lights from '" + sourceHierarchyPath +
                    "'. No native scripts, colliders or NetworkObjects were cloned.");
                return true;
            }
            catch (Exception ex)
            {
                if (cloneRoot != null)
                {
                    try { DestroyObject(cloneRoot); }
                    catch { }
                }

                warn("Vanilla visual reuse failed for '" + sourceHierarchyPath +
                    "'; kept the procedural fallback: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static void CopyHierarchy(
            object sourceTransform,
            object targetParent,
            bool isSourceRoot,
            HashSet<int> visited,
            ref int copiedMeshes,
            ref int copiedLights)
        {
            int instanceId = GetInstanceId(sourceTransform);
            if (instanceId != 0 && !visited.Add(instanceId))
                return;

            object? sourceGameObject = ReadMember(sourceTransform, "gameObject");
            string name = isSourceRoot
                ? "NativeVisualSource"
                : Convert.ToString(ReadMember(sourceTransform, "name")) ?? "NativeVisualPart";

            object targetGameObject = CreateGameObject(name);
            object targetTransform = ReadMember(targetGameObject, "transform")
                ?? throw new MissingMemberException("Cloned visual part has no Transform.");

            MethodInfo setParent = targetTransform.GetType().GetMethods(PublicInstance)
                .First(method => method.Name == "SetParent"
                    && method.GetParameters().Length == 2
                    && method.GetParameters()[0].ParameterType.IsAssignableFrom(targetParent.GetType()));
            setParent.Invoke(targetTransform, new[] { targetParent, (object)false });

            if (isSourceRoot)
            {
                // The source object lives in its original district coordinates.
                // Keep only its authored local scale; the clone receives the
                // requested Harborline position/rotation from ParentAndPlace().
                SetMember(targetTransform, "localPosition",
                    Activator.CreateInstance(_vector3Type!, new object[] { 0f, 0f, 0f }));
                SetMember(targetTransform, "localRotation",
                    _quaternionType!.GetProperty("identity", PublicStatic)?.GetValue(null, null));
                object? sourceScale = ReadMember(sourceTransform, "localScale");
                if (sourceScale != null)
                    SetMember(targetTransform, "localScale", sourceScale);
            }
            else
            {
                CopyTransform(sourceTransform, targetTransform);
            }

            if (sourceGameObject != null)
            {
                object? meshFilter = GetComponent(sourceGameObject, _meshFilterType!);
                object? meshRenderer = GetComponent(sourceGameObject, _meshRendererType!);
                if (meshFilter != null && meshRenderer != null)
                {
                    object? sharedMesh = ReadMember(meshFilter, "sharedMesh");
                    object? sharedMaterials = ReadMember(meshRenderer, "sharedMaterials");
                    if (sharedMesh != null && sharedMaterials != null)
                    {
                        object targetFilter = AddComponent(targetGameObject, _meshFilterType!);
                        object targetRenderer = AddComponent(targetGameObject, _meshRendererType!);
                        SetMember(targetFilter, "sharedMesh", sharedMesh);
                        SetMember(targetRenderer, "sharedMaterials", sharedMaterials);
                        CopyOptionalProperties(meshRenderer, targetRenderer,
                            "shadowCastingMode", "receiveShadows", "sortingOrder",
                            "allowOcclusionWhenDynamic", "motionVectorGenerationMode");
                        copiedMeshes++;
                    }
                }

                object? light = GetComponent(sourceGameObject, _lightType!);
                if (light != null)
                {
                    object targetLight = AddComponent(targetGameObject, _lightType!);
                    CopyOptionalProperties(light, targetLight,
                        "type", "color", "intensity", "range", "spotAngle",
                        "shadows", "shadowStrength", "bounceIntensity", "enabled",
                        "renderMode", "cullingMask");
                    copiedLights++;
                }
            }

            int childCount = ReadInt(sourceTransform, "childCount");
            MethodInfo? getChild = sourceTransform.GetType().GetMethods(PublicInstance)
                .FirstOrDefault(method => method.Name == "GetChild"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == typeof(int));

            if (getChild == null)
                return;

            for (int i = 0; i < childCount; i++)
            {
                object? child = getChild.Invoke(sourceTransform, new object[] { i });
                if (child != null)
                    CopyHierarchy(child, targetTransform, false, visited,
                        ref copiedMeshes, ref copiedLights);
            }
        }

        private static void CopyTransform(object sourceTransform, object targetTransform)
        {
            foreach (string property in new[] { "localPosition", "localRotation", "localScale" })
            {
                object? value = ReadMember(sourceTransform, property);
                if (value != null)
                    SetMember(targetTransform, property, value);
            }
        }

        private static void ParentAndPlace(
            object transform,
            object parent,
            float x,
            float y,
            float z,
            float yaw)
        {
            MethodInfo setParent = transform.GetType().GetMethods(PublicInstance)
                .First(method => method.Name == "SetParent"
                    && method.GetParameters().Length == 2
                    && method.GetParameters()[0].ParameterType.IsAssignableFrom(parent.GetType()));
            setParent.Invoke(transform, new[] { parent, (object)false });
            SetMember(transform, "localPosition", Activator.CreateInstance(_vector3Type!,
                new object[] { x, y, z }));
            object rotation = _quaternionType!.GetMethods(PublicStatic)
                .First(method => method.Name == "Euler"
                    && method.GetParameters().Length == 3
                    && method.GetParameters().All(parameter => parameter.ParameterType == typeof(float)))
                .Invoke(null, new object[] { 0f, yaw, 0f })!;
            SetMember(transform, "localRotation", rotation);
        }

        private static object CreateGameObject(string name)
        {
            return Activator.CreateInstance(_gameObjectType!, new object[] { name })
                ?? throw new InvalidOperationException("GameObject constructor returned null.");
        }

        private static object AddComponent(object gameObject, Type componentType)
        {
            MethodInfo add = gameObject.GetType().GetMethods(PublicInstance)
                .First(method => method.Name == "AddComponent"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == typeof(Type));
            return add.Invoke(gameObject, new object[] { componentType })
                ?? throw new InvalidOperationException("AddComponent returned null for " + componentType.FullName);
        }

        private static object? GetComponent(object gameObject, Type componentType)
        {
            MethodInfo? get = gameObject.GetType().GetMethods(PublicInstance)
                .FirstOrDefault(method => method.Name == "GetComponent"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType == typeof(Type));
            return get?.Invoke(gameObject, new object[] { componentType });
        }

        private static void CopyOptionalProperties(object source, object target, params string[] names)
        {
            foreach (string name in names)
            {
                object? value = ReadMember(source, name);
                if (value == null)
                    continue;
                try { SetMember(target, name, value); }
                catch { }
            }
        }

        private static bool SetMember(object target, string name, object? value)
        {
            PropertyInfo? property = target.GetType().GetProperty(name, AllInstance);
            if (property?.CanWrite == true)
            {
                property.SetValue(target, value, null);
                return true;
            }

            FieldInfo? field = target.GetType().GetField(name, AllInstance);
            if (field != null && !field.IsInitOnly)
            {
                field.SetValue(target, value);
                return true;
            }
            return false;
        }

        private static object? ReadMember(object target, string name)
        {
            Type type = target.GetType();
            PropertyInfo? property = type.GetProperty(name, AllInstance);
            if (property?.CanRead == true)
            {
                try { return property.GetValue(target, null); }
                catch { }
            }
            FieldInfo? field = type.GetField(name, AllInstance);
            if (field != null)
            {
                try { return field.GetValue(target); }
                catch { }
            }
            return null;
        }

        private static int ReadInt(object target, string name)
        {
            try { return Convert.ToInt32(ReadMember(target, name), System.Globalization.CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static int GetInstanceId(object target)
        {
            try
            {
                MethodInfo? method = target.GetType().GetMethods(PublicInstance)
                    .FirstOrDefault(candidate => candidate.Name == "GetInstanceID"
                        && candidate.GetParameters().Length == 0);
                return method == null ? 0 : Convert.ToInt32(method.Invoke(target, null));
            }
            catch { return 0; }
        }

        private static void DestroyObject(object gameObject)
        {
            if (_unityObjectType == null)
                return;
            MethodInfo? destroy = _unityObjectType.GetMethods(PublicStatic)
                .FirstOrDefault(method => method.Name == "Destroy"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType.IsAssignableFrom(gameObject.GetType()));
            destroy?.Invoke(null, new[] { gameObject });
        }

        private static void EnsureTypes()
        {
            _gameObjectType ??= FindType("UnityEngine.GameObject")
                ?? FindType("Il2CppUnityEngine.GameObject")
                ?? throw new TypeLoadException("Unity GameObject type not found.");
            _transformType ??= FindType("UnityEngine.Transform")
                ?? FindType("Il2CppUnityEngine.Transform")
                ?? throw new TypeLoadException("Unity Transform type not found.");
            _meshFilterType ??= FindType("UnityEngine.MeshFilter")
                ?? FindType("Il2CppUnityEngine.MeshFilter")
                ?? throw new TypeLoadException("Unity MeshFilter type not found.");
            _meshRendererType ??= FindType("UnityEngine.MeshRenderer")
                ?? FindType("Il2CppUnityEngine.MeshRenderer")
                ?? throw new TypeLoadException("Unity MeshRenderer type not found.");
            _lightType ??= FindType("UnityEngine.Light")
                ?? FindType("Il2CppUnityEngine.Light")
                ?? throw new TypeLoadException("Unity Light type not found.");
            _quaternionType ??= FindType("UnityEngine.Quaternion")
                ?? FindType("Il2CppUnityEngine.Quaternion")
                ?? throw new TypeLoadException("Unity Quaternion type not found.");
            _vector3Type ??= FindType("UnityEngine.Vector3")
                ?? FindType("Il2CppUnityEngine.Vector3")
                ?? throw new TypeLoadException("Unity Vector3 type not found.");
            _unityObjectType ??= FindType("UnityEngine.Object")
                ?? FindType("Il2CppUnityEngine.Object");
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
