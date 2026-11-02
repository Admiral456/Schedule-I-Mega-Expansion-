using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ScheduleIMegaExpansion.Diagnostics
{
    /// <summary>
    /// Writes metadata-only reflection information for native world, map,
    /// building, property, NPC, door, navigation and law-enforcement types.
    /// It inspects type/member signatures only; it never invokes game methods.
    /// </summary>
    internal static class NativeApiSurfaceScanner
    {
        private const BindingFlags AllDeclared =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly;

        private static readonly string[] RelevantTypeTokens =
        {
            "Map", "Region", "World", "Property", "Business", "Building",
            "NPCEnterableBuilding", "POI", "Door", "Police", "Law",
            "Dispatch", "Patrol", "Checkpoint", "Navigation", "NavMesh",
            "Path", "Avatar", "Schedule", "Customer", "Dealer", "NPC",
            "Shop", "Store", "DeliveryLocation", "Parking", "Prefab",
            "BuildingBuilder", "PrefabPlacer", "NavigationBuilder", "MeshRef",
            "TerrainClearer", "TerrainFlattener", "BuildingPartRegistry",
            "NetworkObject", "NetworkManager", "SpawnablePrefabs"
        };

        private static readonly string[] InterestingMemberTokens =
        {
            "register", "unregister", "add", "remove", "create", "initialize",
            "map", "region", "icon", "bounds", "point", "property", "business",
            "purchase", "owned", "ownership", "prefab", "load", "building",
            "properties", "businesses", "poi", "door", "open", "close",
            "interact", "navigation", "navmesh", "path", "agent", "schedule",
            "customer", "dealer", "spawn", "appearance", "avatar", "dispatch",
            "patrol", "checkpoint", "area", "position", "transform", "world",
            "coordinate", "origin", "texture", "sprite", "icon", "enter",
            "exit", "interior", "exterior", "purchase", "buy", "sell"
        };

        public static int Export(string outputDirectory, Action<string> warn)
        {
            var lines = new List<string>
            {
                "Schedule I Mega Expansion - native game API surface",
                "Mode=METADATA_ONLY",
                "Inspected data: type names, inheritance, interfaces and member signatures only.",
                "No reflected methods, property getters/setters, constructors or game actions are invoked.",
                "Assembly scan is limited to Assembly-CSharp and S1API assemblies.",
                ""
            };

            int inspected = 0;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(IsTargetAssembly)
                .OrderBy(item => item.GetName().Name, StringComparer.Ordinal))
            {
                lines.Add("ASSEMBLY " + assembly.GetName().FullName);
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(type => type != null).Cast<Type>().ToArray();
                    warn("Some types could not be loaded from " + assembly.GetName().Name);
                }
                catch (Exception ex)
                {
                    lines.Add("TYPE_SCAN_ERROR " + ex.GetType().Name + ": " + ex.Message);
                    continue;
                }

                Type[] relevant = types
                    .Where(IsRelevantNativeType)
                    .OrderByDescending(GetTypePriority)
                    .ThenBy(type => type.FullName, StringComparer.Ordinal)
                    .Take(750)
                    .ToArray();

                foreach (Type type in relevant)
                    inspected += WriteTypeSurface(lines, type);

                lines.Add("RelevantTypesWritten=" + relevant.Length);
                lines.Add("");
            }

            System.IO.File.WriteAllLines(
                System.IO.Path.Combine(outputDirectory, "native-game-api-surface.txt"),
                lines);

            return inspected;
        }

        private static bool IsTargetAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name ?? string.Empty;
            return name.IndexOf("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("S1API", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("S1MAPI", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("FishNet", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsRelevantNativeType(Type type)
        {
            string name = type.FullName ?? type.Name;
            return RelevantTypeTokens.Any(token =>
                name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static int GetTypePriority(Type type)
        {
            string name = type.FullName ?? type.Name;
            int priority = 0;
            string[] highPriority =
            {
                "Property", "Business", "Region", "Map", "Building",
                "Door", "Police", "Dispatch", "Navigation", "NavMesh",
                "NPC", "Avatar", "Schedule", "Customer", "Dealer",
                "Prefab", "BuildingBuilder", "PrefabPlacer", "NetworkObject"
            };

            foreach (string token in highPriority)
            {
                if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    priority += 10;
            }

            return priority;
        }

        private static int WriteTypeSurface(List<string> lines, Type type)
        {
            int count = 0;
            lines.Add("[TYPE] " + (type.FullName ?? type.Name));

            try
            {
                Type? baseType = type.BaseType;
                if (baseType != null)
                    lines.Add("BASE " + (baseType.FullName ?? baseType.Name));

                foreach (Type contract in type.GetInterfaces()
                    .OrderBy(item => item.FullName, StringComparer.Ordinal))
                {
                    lines.Add("IMPLEMENTS " + (contract.FullName ?? contract.Name));
                }

                foreach (FieldInfo field in type.GetFields(AllDeclared)
                    .Where(member => IsInterestingMember(member.Name))
                    .OrderBy(member => member.Name, StringComparer.Ordinal)
                    .Take(100))
                {
                    lines.Add("FIELD " + (field.IsStatic ? "static " : "instance ")
                        + (field.FieldType.FullName ?? field.FieldType.Name) + " " + field.Name);
                    count++;
                }

                foreach (PropertyInfo property in type.GetProperties(AllDeclared)
                    .Where(member => IsInterestingMember(member.Name))
                    .OrderBy(member => member.Name, StringComparer.Ordinal)
                    .Take(100))
                {
                    MethodInfo? getter = property.GetGetMethod(true);
                    MethodInfo? setter = property.GetSetMethod(true);
                    string access = getter != null && getter.IsStatic ? "static " : "instance ";
                    lines.Add("PROPERTY " + access
                        + (property.PropertyType.FullName ?? property.PropertyType.Name) + " "
                        + property.Name + " getter=" + (getter != null)
                        + " setter=" + (setter != null));
                    count++;
                }

                foreach (ConstructorInfo constructor in type.GetConstructors(AllDeclared)
                    .Take(20))
                {
                    string parameters = FormatParameters(constructor.GetParameters());
                    lines.Add("CONSTRUCTOR " + type.Name + "(" + parameters + ")");
                    count++;
                }

                foreach (MethodInfo method in type.GetMethods(AllDeclared)
                    .Where(member => IsInterestingMember(member.Name))
                    .OrderBy(member => member.Name, StringComparer.Ordinal)
                    .Take(150))
                {
                    lines.Add("METHOD " + (method.IsStatic ? "static " : "instance ")
                        + (method.ReturnType.FullName ?? method.ReturnType.Name)
                        + " " + method.Name + "("
                        + FormatParameters(method.GetParameters()) + ")");
                    count++;
                }
            }
            catch (Exception ex)
            {
                lines.Add("MEMBER_SCAN_ERROR " + ex.GetType().Name + ": " + ex.Message);
            }

            lines.Add("");
            return count;
        }

        private static string FormatParameters(ParameterInfo[] parameters)
        {
            return string.Join(
                ", ",
                parameters.Select(parameter =>
                    (parameter.ParameterType.FullName ?? parameter.ParameterType.Name)
                    + " " + parameter.Name));
        }

        private static bool IsInterestingMember(string name)
        {
            return InterestingMemberTokens.Any(token =>
                name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
