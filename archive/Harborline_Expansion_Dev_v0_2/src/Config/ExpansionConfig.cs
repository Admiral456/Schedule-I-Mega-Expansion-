using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace HarborlineExpansion.Config
{
    public sealed class ExpansionConfig
    {
        public bool Enabled { get; private set; }
        public Vector3 Origin { get; private set; }

        private static string ConfigPath =>
            Path.Combine(Environment.CurrentDirectory, "UserData", "HarborlineExpansion.cfg");

        public static ExpansionConfig Load()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);

            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath,
                    "# Harborline Expansion prototype settings\n" +
                    "# Keep Enabled=false until you have verified safe free map coordinates.\n" +
                    "Enabled=false\n" +
                    "OriginX=350\n" +
                    "OriginY=0\n" +
                    "OriginZ=350\n");
            }

            var values = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(ConfigPath))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || !line.Contains("="))
                    continue;

                string[] pair = line.Split(new[] { '=' }, 2);
                values[pair[0].Trim()] = pair[1].Trim();
            }

            bool enabled = values.TryGetValue("Enabled", out string? en) &&
                           bool.TryParse(en, out bool parsedEnabled) && parsedEnabled;

            float x = ReadFloat(values, "OriginX", 350f);
            float y = ReadFloat(values, "OriginY", 0f);
            float z = ReadFloat(values, "OriginZ", 350f);

            return new ExpansionConfig
            {
                Enabled = enabled,
                Origin = new Vector3(x, y, z)
            };
        }

        private static float ReadFloat(
            System.Collections.Generic.Dictionary<string, string> values,
            string key,
            float fallback)
        {
            if (values.TryGetValue(key, out string? raw) &&
                float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) &&
                !float.IsNaN(value) && !float.IsInfinity(value))
                return value;

            return fallback;
        }
    }
}