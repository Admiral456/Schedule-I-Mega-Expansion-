using System;
using System.Linq;
using System.Reflection;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// Uses S1MAPI's documented procedural building pipeline when it is available.
    /// The geometry remains a mod-created building, not a registered vanilla Property,
    /// Business or Realtor listing. No third-party mod assets or code are used.
    /// </summary>
    internal static class HarborlineProceduralBuildingFactory
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static bool TryBuildWarehouse(
            object parentTransform,
            float localX,
            float localZ,
            Action<string> log,
            Action<string> warn)
        {
            object? builder = null;
            object? builderRoot = null;

            try
            {
                Type? builderType = FindType("S1MAPI.Building.BuildingBuilder");
                Type? configType = FindType("S1MAPI.Building.Config.BuildingConfig");
                if (builderType == null || configType == null)
                {
                    warn("S1MAPI BuildingBuilder/BuildingConfig was not found; the warehouse will use the safe primitive fallback.");
                    return false;
                }

                builder = Activator.CreateInstance(builderType, new object[] { "Harborline Portside Supply" });
                if (builder == null)
                    throw new InvalidOperationException("S1MAPI BuildingBuilder constructor returned null.");

                builderRoot = ReadMember(builder, "Root");
                object config = Activator.CreateInstance(configType)
                    ?? throw new InvalidOperationException("S1MAPI BuildingConfig could not be constructed.");

                SetFloat(config, "Width", 13.0f);
                SetFloat(config, "Height", 5.5f);
                SetFloat(config, "Depth", 12.0f);
                SetFloat(config, "WallThickness", 0.22f);
                SetFloat(config, "FloorThickness", 0.14f);
                SetFloat(config, "CeilingThickness", 0.14f);

                InvokeRequired(builder, "WithConfig", new object?[] { config });
                InvokeRequired(builder, "AddFloor", Array.Empty<object?>());
                InvokeRequired(builder, "AddCeiling", Array.Empty<object?>());

                bool hasSouthDoorOpening = TryInvokeNamed(
                    builder,
                    "AddWalls",
                    new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["southDoor"] = true,
                        ["eastWindow"] = true,
                        ["westWindow"] = true
                    });

                if (!hasSouthDoorOpening)
                {
                    // Still build a complete weatherproof shell, but do not add a door
                    // at a guessed position when the installed S1MAPI signature differs.
                    InvokeRequired(builder, "AddWalls", Array.Empty<object?>());
                    warn("S1MAPI AddWalls did not expose the documented southDoor/eastWindow/westWindow parameters; the storefront opening and door were skipped.");
                }

                TryInvokeOptional(builder, "AddRoofTrim", Array.Empty<object?>());
                TryInvokeOptional(builder, "AddLights", Array.Empty<object?>());

                bool doorAdded = false;
                if (hasSouthDoorOpening)
                {
                    doorAdded = TryInvokeSlidingDoors(builder);
                    if (!doorAdded)
                        warn("S1MAPI's documented sliding-door method was not available for this build; the doorway remains open.");
                }

                object? built = InvokeRequired(builder, "Build", Array.Empty<object?>());
                object? gameObject = built ?? builderRoot ?? ReadMember(builder, "Root");
                if (gameObject == null)
                    throw new InvalidOperationException("S1MAPI Build() returned no building root.");

                object? transform = ReadMember(gameObject, "transform");
                if (transform == null)
                    throw new MissingMemberException("S1MAPI building root has no Transform.");

                ParentAndPlace(transform, parentTransform, localX, 0f, localZ);

                bool interiorNavigationBuilt = TryBuildInteriorNavigation(builder, warn);
                log("S1MAPI warehouse created: procedural floor, roof, walls and lighting; door=" +
                    (doorAdded ? "networked sliding door requested" : "not available") +
                    "; interior A* navigation=" + (interiorNavigationBuilt ? "built" : "not confirmed") + ".");
                log("The building is physical geometry only; native Property/Business/Realtor registration is a separate system.");
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    object? root = builderRoot ?? (builder == null ? null : ReadMember(builder, "Root"));
                    if (root != null)
                        DestroyObject(root);
                }
                catch (Exception cleanupError)
                {
                    warn("S1MAPI warehouse rollback warning: " + cleanupError.Message);
                }

                warn("S1MAPI warehouse generation failed; falling back to primitive geometry: " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static bool TryInvokeSlidingDoors(object builder)
        {
            Type? vectorType = FindType("UnityEngine.Vector3") ?? FindType("Il2CppUnityEngine.Vector3");
            Type? quaternionType = FindType("UnityEngine.Quaternion") ?? FindType("Il2CppUnityEngine.Quaternion");
            if (vectorType == null || quaternionType == null)
                return false;

            object position = Activator.CreateInstance(vectorType, new object[] { 0f, 0f, -6.0f })
                ?? throw new InvalidOperationException("Could not construct door Vector3.");
            object rotation = CreateYawQuaternion(quaternionType, 180f);

            MethodInfo? method = builder.GetType().GetMethods(PublicInstance)
                .Where(candidate => candidate.Name == "AddSlidingDoors")
                .FirstOrDefault(candidate =>
                {
                    ParameterInfo[] p = candidate.GetParameters();
                    return p.Length >= 3
                        && p[0].ParameterType.IsInstanceOfType(position)
                        && p[1].ParameterType.IsInstanceOfType(rotation)
                        && p[2].ParameterType == typeof(string);
                });
            if (method == null)
                return false;

            object?[] args = BuildArguments(method.GetParameters(), new System.Collections.Generic.Dictionary<string, object?>
            {
                [method.GetParameters()[0].Name ?? "position"] = position,
                [method.GetParameters()[1].Name ?? "rotation"] = rotation,
                [method.GetParameters()[2].Name ?? "openingHours"] = "24/7"
            });
            method.Invoke(builder, args);
            return true;
        }

        private static bool TryBuildInteriorNavigation(object builder, Action<string> warn)
        {
            try
            {
                MethodInfo? create = builder.GetType().GetMethods(PublicInstance)
                    .FirstOrDefault(method => method.Name == "CreateNavigationBuilder"
                        && method.GetParameters().Length == 0);
                if (create == null)
                    return false;

                object? navigation = create.Invoke(builder, null);
                if (navigation == null)
                    return false;

                MethodInfo? build = navigation.GetType().GetMethods(PublicInstance)
                    .FirstOrDefault(method => method.Name == "Build" && method.GetParameters().Length == 0);
                if (build == null)
                    return false;

                build.Invoke(navigation, null);
                return true;
            }
            catch (Exception ex)
            {
                warn("S1MAPI interior navigation could not be built: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static object? InvokeRequired(object target, string name, object?[] values)
        {
            MethodInfo? method = target.GetType().GetMethods(PublicInstance)
                .Where(candidate => candidate.Name == name)
                .FirstOrDefault(candidate => CanSupplyArguments(candidate.GetParameters(), values.Length));
            if (method == null)
                throw new MissingMethodException(target.GetType().FullName, name);

            return method.Invoke(target, BuildPositionalArguments(method.GetParameters(), values));
        }

        private static void TryInvokeOptional(object target, string name, object?[] values)
        {
            try { InvokeRequired(target, name, values); }
            catch (MissingMethodException) { }
        }

        private static bool TryInvokeNamed(
            object target,
            string name,
            System.Collections.Generic.Dictionary<string, object?> overrides)
        {
            MethodInfo? method = target.GetType().GetMethods(PublicInstance)
                .Where(candidate => candidate.Name == name)
                .FirstOrDefault(candidate =>
                {
                    ParameterInfo[] p = candidate.GetParameters();
                    return overrides.Keys.All(key => p.Any(parameter =>
                        string.Equals(parameter.Name, key, StringComparison.OrdinalIgnoreCase)
                        && parameter.ParameterType.IsInstanceOfType(overrides[key])));
                });
            if (method == null)
                return false;

            method.Invoke(target, BuildArguments(method.GetParameters(), overrides));
            return true;
        }

        private static bool CanSupplyArguments(ParameterInfo[] parameters, int supplied)
        {
            if (parameters.Length < supplied)
                return false;
            for (int i = supplied; i < parameters.Length; i++)
            {
                if (!parameters[i].IsOptional && !parameters[i].HasDefaultValue)
                    return false;
            }
            return true;
        }

        private static object?[] BuildPositionalArguments(ParameterInfo[] parameters, object?[] supplied)
        {
            var args = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                args[i] = i < supplied.Length ? supplied[i] : DefaultValue(parameters[i]);
            return args;
        }

        private static object?[] BuildArguments(
            ParameterInfo[] parameters,
            System.Collections.Generic.Dictionary<string, object?> overrides)
        {
            var args = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                if (parameter.Name != null && overrides.TryGetValue(parameter.Name, out object? value))
                    args[i] = value;
                else
                    args[i] = DefaultValue(parameter);
            }
            return args;
        }

        private static object? DefaultValue(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
                return parameter.DefaultValue;
            Type type = parameter.ParameterType;
            if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
                return null;
            return Activator.CreateInstance(type);
        }

        private static void ParentAndPlace(object transform, object parentTransform, float x, float y, float z)
        {
            MethodInfo? setParent = transform.GetType().GetMethods(PublicInstance)
                .FirstOrDefault(method => method.Name == "SetParent"
                    && method.GetParameters().Length == 2
                    && method.GetParameters()[0].ParameterType.IsAssignableFrom(parentTransform.GetType())
                    && method.GetParameters()[1].ParameterType == typeof(bool));
            if (setParent == null)
                throw new MissingMethodException("Transform.SetParent(Transform, bool) was not found.");

            setParent.Invoke(transform, new object[] { parentTransform, false });

            Type? vectorType = FindType("UnityEngine.Vector3") ?? FindType("Il2CppUnityEngine.Vector3");
            Type? quaternionType = FindType("UnityEngine.Quaternion") ?? FindType("Il2CppUnityEngine.Quaternion");
            if (vectorType == null || quaternionType == null)
                throw new TypeLoadException("Unity Vector3/Quaternion types were not found.");

            SetMember(transform, "localPosition", Activator.CreateInstance(vectorType, new object[] { x, y, z }));
            SetMember(transform, "localRotation", CreateYawQuaternion(quaternionType, 0f));
        }

        private static object CreateYawQuaternion(Type quaternionType, float yaw)
        {
            MethodInfo? euler = quaternionType.GetMethods(PublicStatic)
                .FirstOrDefault(method => method.Name == "Euler"
                    && method.GetParameters().Length == 3
                    && method.GetParameters().All(parameter => parameter.ParameterType == typeof(float)));
            if (euler == null)
                throw new MissingMethodException(quaternionType.FullName, "Euler(float,float,float)");

            return euler.Invoke(null, new object[] { 0f, yaw, 0f })
                ?? throw new InvalidOperationException("Quaternion.Euler returned null.");
        }

        private static void SetFloat(object target, string name, float value)
        {
            if (!SetMember(target, name, value))
                throw new MissingMemberException(target.GetType().FullName, name);
        }

        private static bool SetMember(object target, string name, object? value)
        {
            Type type = target.GetType();
            PropertyInfo? property = type.GetProperty(name, AllInstance);
            if (property?.CanWrite == true)
            {
                property.SetValue(target, value, null);
                return true;
            }

            FieldInfo? field = type.GetField(name, AllInstance);
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
                return property.GetValue(target, null);
            FieldInfo? field = type.GetField(name, AllInstance);
            return field?.GetValue(target);
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

        private static void DestroyObject(object gameObject)
        {
            Type? objectType = FindType("UnityEngine.Object") ?? FindType("Il2CppUnityEngine.Object");
            MethodInfo? destroy = objectType?.GetMethods(PublicStatic)
                .FirstOrDefault(method => method.Name == "Destroy"
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType.IsAssignableFrom(gameObject.GetType()));
            destroy?.Invoke(null, new[] { gameObject });
        }
    }
}
