using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ScheduleIMegaExpansion.Diagnostics
{
    /// <summary>
    /// Writes metadata-only reflection information for native map and property types.
    /// It inspects names and signatures only; no game methods or property setters are invoked.
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

                int written = 0;
                foreach (Type type in types
                    .Where(IsRelevantMapOrPropertyType)
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .Take(300))
                {
                    inspected += WriteTypeSurface(lines, type);
                    written++;
                }

                lines.Add("RelevantTypesWritten=" + written);
                lines.Add("");
            }

            System.IO.File.WriteAllLines(
                System.IO.Path.Combine(outputDirectory, "native-map-property-api.txt"),
                lines);

            return inspected;
        }

        private static bool IsTargetAssembly(Assembly assembly)
        {
            string name = assembly.GetName().Name ?? string.Empty;
            return name.IndexOf("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("S1API", StringComparison.OrdinalIgnoreCase) >= 0;
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

        private static int WriteTypeSurface(List<string> lines, Type type)
        {
            int count = 0;
            lines.Add("[TYPE] " + (type.FullName ?? type.Name));

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
                lines.Add("MEMBER_SCAN_ERROR " + ex.GetType().Name + ": " + ex.Message);
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
                "properties", "businesses", "poi"
            };

            return keywords.Any(keyword =>
                name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
