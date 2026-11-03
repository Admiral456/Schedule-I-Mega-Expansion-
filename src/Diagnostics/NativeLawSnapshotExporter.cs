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
    /// Read-only snapshot of Schedule I's existing law-enforcement world state.
    /// It does not enable checkpoints, spawn officers, start patrols or change wanted levels.
    /// </summary>
    internal static class NativeLawSnapshotExporter
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        public static int Export(string outputDirectory, List<string> summary, Action<string> warn)
        {
            int exported = 0;
            exported += ExportStaticValues(
                outputDirectory,
                summary,
                warn,
                "S1API.Law.LawController",
                new[] { "Intensity", "InternalIntensity", "MaxIntensity", "MinIntensity", "IsUsingOverrideSettings" },
                "native-law-controller.csv");

            exported += ExportStaticValues(
                outputDirectory,
                summary,
                warn,
                "S1API.Law.LawManager",
                new[] { "ActiveOfficerCount", "DispatchOfficerCount", "DispatchVehicleUseThreshold" },
                "native-law-manager.csv");

            exported += ExportEnum(
                outputDirectory,
                summary,
                warn,
                "S1API.Law.CheckpointLocation",
                "native-law-checkpoint-locations.csv");

            exported += ExportCollection(
                outputDirectory,
                summary,
                warn,
                "S1API.Law.CheckpointManager",
                "GetAllCheckpointInfo",
                "native-law-checkpoints.csv",
                "checkpoint");

            exported += ExportCollection(
                outputDirectory,
                summary,
                warn,
                "S1API.Law.LawManager",
                "GetAllFootPatrolRoutes",
                "native-law-foot-patrol-routes.csv",
                "footPatrolRoute");

            exported += ExportCollection(
                outputDirectory,
                summary,
                warn,
                "S1API.Law.LawManager",
                "GetAllVehiclePatrolRoutes",
                "native-law-vehicle-patrol-routes.csv",
                "vehiclePatrolRoute");

            summary.Add("NativeLawSnapshotMode=READ_ONLY");
            summary.Add("NativeLawSnapshotNote=Routes/checkpoints are enumerated from the current scene. This exporter does not create a Harborline route or enable a checkpoint.");
            return exported;
        }

        private static int ExportStaticValues(
            string directory,
            List<string> summary,
            Action<string> warn,
            string typeName,
            string[] memberNames,
            string fileName)
        {
            try
            {
                Type type = FindType(typeName)
                    ?? throw new TypeLoadException(typeName + " was not loaded.");
                var rows = new List<string[]>();
                foreach (string name in memberNames)
                {
                    object? value = ReadStaticMember(type, name);
                    rows.Add(new[] { name, Format(value) });
                }

                WriteCsv(Path.Combine(directory, fileName), new[] { "member", "value" }, rows);
                summary.Add(typeName + "Snapshot=success");
                return rows.Count;
            }
            catch (Exception ex)
            {
                AddError(typeName, ex, summary, warn);
                return 0;
            }
        }

        private static int ExportEnum(
            string directory,
            List<string> summary,
            Action<string> warn,
            string typeName,
            string fileName)
        {
            try
            {
                Type type = FindType(typeName)
                    ?? throw new TypeLoadException(typeName + " was not loaded.");
                if (!type.IsEnum)
                    throw new InvalidOperationException(typeName + " is not an enum.");

                var rows = Enum.GetValues(type).Cast<object>()
                    .Select(value => new[]
                    {
                        value.ToString() ?? string.Empty,
                        Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                    });
                List<string[]> materialized = rows.ToList();
                WriteCsv(Path.Combine(directory, fileName), new[] { "checkpointLocation", "enumValue" }, materialized);
                summary.Add("NativeCheckpointLocations=" + materialized.Count.ToString(CultureInfo.InvariantCulture));
                return materialized.Count;
            }
            catch (Exception ex)
            {
                AddError(typeName, ex, summary, warn);
                return 0;
            }
        }

        private static int ExportCollection(
            string directory,
            List<string> summary,
            Action<string> warn,
            string managerTypeName,
            string methodName,
            string fileName,
            string rowKind)
        {
            try
            {
                Type manager = FindType(managerTypeName)
                    ?? throw new TypeLoadException(managerTypeName + " was not loaded.");
                MethodInfo method = manager.GetMethod(methodName, PublicStatic, null, Type.EmptyTypes, null)
                    ?? throw new MissingMethodException(managerTypeName + "." + methodName + "()");
                object? raw = method.Invoke(null, null);
                if (!(raw is IEnumerable enumerable))
                    throw new InvalidOperationException(managerTypeName + "." + methodName + " did not return an enumerable collection.");

                var rows = new List<string[]>();
                int index = 0;
                foreach (object? item in enumerable)
                {
                    if (item == null)
                        continue;
                    index++;
                    rows.Add(new[]
                    {
                        index.ToString(CultureInfo.InvariantCulture),
                        item.GetType().FullName ?? item.GetType().Name,
                        GetDisplayName(item),
                        SerializePublicValues(item)
                    });
                }

                WriteCsv(Path.Combine(directory, fileName),
                    new[] { "index", "runtimeType", rowKind + "Name", "publicValues" }, rows);
                summary.Add(managerTypeName + "." + methodName + "=" + rows.Count.ToString(CultureInfo.InvariantCulture));
                return rows.Count;
            }
            catch (Exception ex)
            {
                AddError(managerTypeName + "." + methodName, ex, summary, warn);
                return 0;
            }
        }

        private static object? ReadStaticMember(Type type, string name)
        {
            PropertyInfo? property = type.GetProperty(name, PublicStatic);
            if (property?.CanRead == true)
                return property.GetValue(null);
            FieldInfo? field = type.GetField(name, PublicStatic);
            return field?.GetValue(null);
        }

        private static string GetDisplayName(object instance)
        {
            foreach (string name in new[] { "Name", "RouteName", "Location", "CheckpointLocation", "Id", "ID" })
            {
                object? value = ReadInstanceMember(instance, name);
                if (value != null && !string.IsNullOrWhiteSpace(Format(value)))
                    return Format(value);
            }
            return instance.ToString() ?? instance.GetType().Name;
        }

        private static string SerializePublicValues(object instance)
        {
            var values = new List<string>();
            foreach (PropertyInfo property in instance.GetType().GetProperties(PublicInstance)
                .Where(property => property.GetIndexParameters().Length == 0)
                .OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                try
                {
                    if (property.CanRead)
                        values.Add(property.Name + "=" + Format(property.GetValue(instance)));
                }
                catch (Exception ex)
                {
                    values.Add(property.Name + "=<error:" + ex.GetType().Name + ">");
                }
            }

            foreach (FieldInfo field in instance.GetType().GetFields(PublicInstance).OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                try { values.Add(field.Name + "=" + Format(field.GetValue(instance))); }
                catch (Exception ex) { values.Add(field.Name + "=<error:" + ex.GetType().Name + ">"); }
            }

            return string.Join("; ", values);
        }

        private static object? ReadInstanceMember(object instance, string name)
        {
            PropertyInfo? property = instance.GetType().GetProperty(name, PublicInstance);
            if (property?.CanRead == true && property.GetIndexParameters().Length == 0)
                return property.GetValue(instance);
            FieldInfo? field = instance.GetType().GetField(name, PublicInstance);
            return field?.GetValue(instance);
        }

        private static string Format(object? value)
        {
            if (value == null)
                return string.Empty;
            if (value is string text)
                return text.Replace("\r", " ").Replace("\n", " ");
            if (value is IEnumerable enumerable && !(value is string))
            {
                var parts = new List<string>();
                foreach (object? item in enumerable)
                    parts.Add(item?.ToString() ?? string.Empty);
                return string.Join("|", parts);
            }
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? string.Empty;
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
                catch
                {
                    // Ignore an assembly whose IL2CPP metadata cannot be enumerated.
                }
            }
            return null;
        }

        private static void WriteCsv(string path, string[] headers, IEnumerable<string[]> rows)
        {
            using var writer = new StreamWriter(path, false);
            writer.WriteLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (string[] row in rows)
                writer.WriteLine(string.Join(",", row.Select(EscapeCsv)));
        }

        private static string EscapeCsv(string value)
        {
            string safe = value ?? string.Empty;
            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }

        private static void AddError(string source, Exception ex, List<string> summary, Action<string> warn)
        {
            string message = source + " export failed: " + ex.GetType().Name + ": " + ex.Message;
            summary.Add("Error=" + message);
            warn(message);
        }
    }
}