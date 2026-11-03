using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ScheduleIMegaExpansion.Diagnostics
{
    /// <summary>
    /// Focused metadata-only scan for the integration points needed by Harborline:
    /// Realtor choices, property ownership, doors, movement, roads and map registration.
    /// It deliberately never invokes constructors, getters, setters or game methods.
    /// </summary>
    internal static class HarborlineIntegrationApiScanner
    {
        private const BindingFlags DeclaredMembers =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static readonly string[] TypeTokens =
        {
            "EstateAgent", "RealEstate", "PropertyListing", "PropertyManager",
            "BusinessManager", "Property", "Business", "NPCEnterableBuilding",
            "DoorController", "StaticDoor", "PivotDoor", "InteractableObject",
            "NPCMovement", "NPCSchedule", "NavMesh", "Navigation", "AstarPath",
            "Pathfinding", "RecastGraph", "PointGraph", "GraphNode", "Seeker",
            "AIPath", "RoadNodes", "NodeLink", "Road", "Region", "Map"
        };

        private static readonly string[] MemberTokens =
        {
            "ShouldChoiceBeShown", "ModifyChoiceText", "CheckChoice", "ChoiceCallback",
            "AgentShow", "AgentText", "AgentCheck", "AgentPick", "SetOwned",
            "SetDestination", "GetNearest", "GetNearestNode", "UpdateGraph",
            "Scan", "AddWorkItem", "Connect", "StayInBuilding", "WalkTo",
            "SetHouse", "House", "Home", "RecalculatePath", "Build",
            "BuyingProperty", "Register", "AddProperty", "AddBusiness",
            "StartFootPatrol", "StartVehiclePatrol", "SetDestination",
            "GetAllProperties", "GetAllBusinesses", "WalkTo", "StayInBuilding",
            "Choice", "Ownership", "Listing", "Purchase", "Buy", "Sell",
            "Agent", "Door", "Open", "Close", "Enter", "Exit", "Nav", "Path",
            "Road", "Region", "Map", "Icon", "Spawn", "Register", "Add"
        };

        public static int Export(string outputDirectory)
        {
            var lines = new List<string>
            {
                "Schedule I Mega Expansion - Harborline integration API surface",
                "Mode=METADATA_ONLY",
                "Assemblies=Assembly-CSharp, S1API, S1MAPI, FishNet",
                "This report enumerates metadata only. It does not invoke reflected methods, constructors, property accessors or game actions.",
                "Use exact installed-game signatures before implementing hooks or registration.",
                ""
            };

            int typeCount = 0;
            int memberCount = 0;

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
                    lines.Add("TYPE_LOAD_WARNING=" + ex.GetType().Name);
                }
                catch (Exception ex)
                {
                    lines.Add("TYPE_SCAN_ERROR=" + ex.GetType().Name + ": " + ex.Message);
                    continue;
                }

                Type[] selected = types
                    .Where(IsCandidateType)
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .Take(600)
                    .ToArray();

                foreach (Type type in selected)
                {
                    typeCount++;
                    lines.Add("[TYPE] " + (type.FullName ?? type.Name));
                    Type? baseType = type.BaseType;
                    if (baseType != null)
                        lines.Add("BASE " + (baseType.FullName ?? baseType.Name));

                    foreach (Type contract in type.GetInterfaces().OrderBy(item => item.FullName, StringComparer.Ordinal))
                        lines.Add("IMPLEMENTS " + (contract.FullName ?? contract.Name));

                    try
                    {
                        foreach (MethodInfo method in type.GetMethods(DeclaredMembers)
                            .Where(method => IsCandidateMember(method.Name))
                            .OrderBy(method => method.Name, StringComparer.Ordinal)
                            .Take(250))
                        {
                            string access = method.IsPublic ? "public" :
                                method.IsFamily ? "protected" :
                                method.IsAssembly ? "internal" :
                                method.IsPrivate ? "private" : "nonpublic";
                            lines.Add("METHOD " + access + " " +
                                (method.IsStatic ? "static " : "instance ") +
                                FormatType(method.ReturnType) + " " + method.Name +
                                "(" + string.Join(", ", method.GetParameters().Select(FormatParameter)) + ")");
                            memberCount++;
                        }

                        foreach (PropertyInfo property in type.GetProperties(DeclaredMembers)
                            .Where(property => IsCandidateMember(property.Name))
                            .OrderBy(property => property.Name, StringComparer.Ordinal)
                            .Take(120))
                        {
                            MethodInfo? getter = property.GetGetMethod(true);
                            MethodInfo? setter = property.GetSetMethod(true);
                            lines.Add("PROPERTY " + FormatType(property.PropertyType) + " " + property.Name +
                                " getter=" + (getter != null) + " setter=" + (setter != null));
                            memberCount++;
                        }

                        foreach (FieldInfo field in type.GetFields(DeclaredMembers)
                            .Where(field => IsCandidateMember(field.Name))
                            .OrderBy(field => field.Name, StringComparer.Ordinal)
                            .Take(120))
                        {
                            lines.Add("FIELD " + (field.IsStatic ? "static " : "instance ") +
                                FormatType(field.FieldType) + " " + field.Name +
                                " readonly=" + field.IsInitOnly);
                            memberCount++;
                        }

                        foreach (EventInfo eventInfo in type.GetEvents(DeclaredMembers)
                            .Where(eventInfo => IsCandidateMember(eventInfo.Name))
                            .OrderBy(eventInfo => eventInfo.Name, StringComparer.Ordinal)
                            .Take(80))
                        {
                            lines.Add("EVENT " + FormatType(eventInfo.EventHandlerType ?? typeof(void)) +
                                " " + eventInfo.Name);
                            memberCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        lines.Add("MEMBER_SCAN_ERROR " + ex.GetType().Name + ": " + ex.Message);
                    }

                    lines.Add("");
                }

                lines.Add("CandidateTypesWritten=" + selected.Length);
                lines.Add("");
            }

            lines.Insert(5, "CandidateTypeCount=" + typeCount);
            lines.Insert(6, "CandidateMemberCount=" + memberCount);
            System.IO.File.WriteAllLines(
                System.IO.Path.Combine(outputDirectory, "native-harborline-integration-api.txt"),
                lines);
            return memberCount;
        }

        private static bool IsTargetAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name ?? string.Empty;
            return name.IndexOf("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("S1API", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("S1MAPI", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("FishNet", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsCandidateType(Type type)
        {
            string name = type.FullName ?? type.Name;
            return TypeTokens.Any(token => name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsCandidateMember(string name)
        {
            return MemberTokens.Any(token => name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string FormatParameter(ParameterInfo parameter)
        {
            string modifier = parameter.IsOut ? "out " :
                parameter.ParameterType.IsByRef ? "ref " : string.Empty;
            return modifier + FormatType(parameter.ParameterType) + " " + parameter.Name +
                (parameter.IsOptional ? " (optional)" : string.Empty);
        }

        private static string FormatType(Type type)
        {
            if (type.IsByRef)
                return FormatType(type.GetElementType() ?? typeof(void)) + "&";
            if (type.IsArray)
                return FormatType(type.GetElementType() ?? typeof(void)) + "[]";
            return type.FullName ?? type.Name;
        }
    }
}
