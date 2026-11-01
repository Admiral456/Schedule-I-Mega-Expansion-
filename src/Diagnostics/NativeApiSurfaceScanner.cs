using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using S1API.Map;
using S1API.Property;

namespace ScheduleIMegaExpansion.Diagnostics
{
    /// <summary>
    /// Writes metadata-only reflection information for native map and property types.
    /// No game methods are invoked and no game state is changed.
    /// </summary>
    internal static class NativeApiSurfaceScanner
    {
        private const BindingFlags AllDeclared =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.DeclaredOnly;

        public static int Export(string outputDirectory, Action<string> warn)
        {
            var lines = new List<string>
            {
                "Schedule I Mega Expansion - native API surface",
                "Mode=METADATA_ONLY",
                "No reflected methods or property setters are invoked.",
                ""
            };

            int inspected = 0;

            try
            {
                List<PropertyWrapper> properties = PropertyManager.GetAllProperties();
                if (properties.Count > 0)
                {
                    object? native = ReadWrapperField(properties[0], "InnerProperty");
                    if (native != null)
                    {
                        inspected += WriteTypeSurface(lines, "NativeProperty", native.GetType());
                    }
                }
            }
            catch (Exception ex)
            {
                lines.Add("NativePropertyInspection=ERROR: " + ex.GetType().Name + ": " + ex.Message);
                warn("Could not inspect the native Property type: " + ex.Message);
            }

            try
            {
                List<BusinessWrapper> businesses = BusinessManager.GetAllBusinesses();
                if (businesses.Count > 0)
                {
                    object? native = ReadWrapperField(businesses[0], "InnerBusiness");
                    if (native != null)
                    {
                        inspected += WriteTypeSurface(lines, "NativeBusiness", native.GetType());
                    }
                }
            }
            catch (Exception ex)
            {
                lines.Add("NativeBusinessInspection=ERROR: " + ex.GetType().Name + ": " + ex.Message);
                warn("Could not inspect the native Business type: " + ex.Message);
            }

            try
            {
                Assembly? gameAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly =>
                        string.Equals(assembly.GetName().Name, "Assembly-CSharp", StringComparison.OrdinalIgnoreCase));

                if (gameAssembly == null)
                {
                    lines.Add("GameAssembly=not-loaded");
                }
                else
                {
                    lines.Add("GameAssembly=" + gameAssembly.FullName);
                    Type[] types;
                    try
                    {
                        types = gameAssembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        types = ex.Types.Where(type => type != null).Cast<Type>().ToArray();
                        warn("Some game types could not be loaded during metadata inspection.");
                    }

                    int matched = 0;
                    foreach (Type type in types
                        .Where(IsRelevantMapOrPropertyType)
                        .OrderBy(type => type.FullName, StringComparer.Ordinal)
                        .Take(300))
                    {
                        inspected += WriteTypeSurface(lines, "GameType", type);
                        matched++;
                    }

                    lines.Add("RelevantGameTypesWritten=" + matched);
                }
            }
            catch (Exception ex)
            {
                lines.Add("GameTypeScan=ERROR: " + ex.GetType().Name + ": " + ex.Message);
                warn("Native game type scan was incomplete: " + ex.Message);
            }

            File.WriteAllLines(
                Path.Combine(outputDirectory, "native-map-property-api.txt"),
                lines);

            return inspected;
        }

        private static object? ReadWrapperField(object wrapper, string fieldName)
        {
            FieldInfo? field = wrapper.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(wrapper);
        }

        private static bool IsRelevantMapOrPropertyType(Type type)
        {
            string name = type.FullName ?? type.Name;
            return name.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Region", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Property", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Business", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("NPCEnterableBuilding", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("POI", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int WriteTypeSurface(List<string> lines, string label, Type type)
        {
            int count = 0;
            lines.Add("[" + label + "] " + (type.FullName ?? type.Name));
            lines.Add("Assembly=" + type.Assembly.GetName().Name);

            try
            {
                foreach (FieldInfo field in type.GetFields(AllDeclared)
                    .Where(member => IsInterestingMember(member.Name))
                    .OrderBy(member => member.Name, StringComparer.Ordinal))
                {
                    lines.Add("FIELD " + (field.IsStatic ? "static " : "instance ")
                        + field.FieldType.FullName + " " + field.Name);
                    count++;
                }

                foreach (PropertyInfo property in type.GetProperties(AllDeclared)
                    .Where(member => IsInterestingMember(member.Name))
                    .OrderBy(member => member.Name, StringComparer.Ordinal))
                {
                    MethodInfo? getter = property.GetGetMethod(true);
                    MethodInfo? setter = property.GetSetMethod(true);
                    string access = getter != null && getter.IsStatic ? "static " : "instance ";
                    lines.Add("PROPERTY " + access + property.PropertyType.FullName + " "
                        + property.Name + " getter=" + (getter != null)
                        + " setter=" + (setter != null));
                    count++;
                }

                foreach (MethodInfo method in type.GetMethods(AllDeclared)
                    .Where(member => IsInterestingMember(member.Name))
                    .OrderBy(member => member.Name, StringComparer.Ordinal)
                    .Take(100))
                {
                    string parameters = string.Join(
                        ", ",
                        method.GetParameters().Select(parameter =>
                            (parameter.ParameterType.FullName ?? parameter.ParameterType.Name)
                            + " " + parameter.Name));
                    lines.Add("METHOD " + (method.IsStatic ? "static " : "instance ")
                        + (method.ReturnType.FullName ?? method.ReturnType.Name)
                        + " " + method.Name + "(" + parameters + ")");
                    count++;
                }
            }
            catch (Exception ex)
            {
                lines.Add("MemberInspection=ERROR: " + ex.GetType().Name + ": " + ex.Message);
            }

            lines.Add("");
            return count;
        }

        private static bool IsInterestingMember(string name)
        {
            string[] keywords =
            {
                "register", "unregister", "add", "remove", "create", "initialize",
                "map", "region", "icon", "bounds", "point", "property", "business",
                "purchase", "owned", "ownership", "prefab", "load", "building",
                "properties", "businesses", "poI"
            };

            return keywords.Any(keyword =>
                name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
