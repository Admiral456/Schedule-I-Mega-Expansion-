using System;
using System.IO;
using MelonLoader;

namespace ScheduleIMegaExpansion.Runtime
{
    /// <summary>
    /// F6 captures an exact in-game anchor from the active camera.
    /// F7 creates the first physical dockyard prototype at that anchor.
    /// </summary>
    internal static class HarborlineHotkeys
    {
        private static bool _loggedInstructions;
        private static bool _inputWarningLogged;
        private static Type? _inputType;
        private static Type? _keyCodeType;

        public static void ResetForNewSave()
        {
            _loggedInstructions = false;
            _inputWarningLogged = false;
            HarborlinePhysicalBuilder.ResetForNewSave();
        }

        public static void Update(string gameRoot, Action<string> log, Action<string> warn)
        {
            if (!_loggedInstructions)
            {
                _loggedInstructions = true;
                log("Harborline prototype controls: F6 = capture your current in-game anchor; F7 = spawn the physical dockyard preview. Capture while standing at the intended connection point inside Docks.");
                log("The preview is a single-player geometry prototype. It does not yet register native businesses, properties, doors or NPCs.");
            }

            try
            {
                if (KeyDown("F6"))
                {
                    HarborlinePhysicalBuilder.CaptureAnchor(
                        Path.Combine(gameRoot, "UserData", "ScheduleIMegaExpansion", "Harborline"),
                        log,
                        warn);
                }

                if (KeyDown("F7"))
                {
                    HarborlinePhysicalBuilder.BuildPhysicalSlice(
                        Path.Combine(gameRoot, "UserData", "ScheduleIMegaExpansion", "Harborline"),
                        log,
                        warn);
                }
            }
            catch (Exception ex)
            {
                if (!_inputWarningLogged)
                {
                    _inputWarningLogged = true;
                    warn("Harborline hotkey polling is unavailable in this runtime: " + ex.Message);
                }
            }
        }

        private static bool KeyDown(string keyName)
        {
            EnsureInputTypes();
            object key = Enum.Parse(_keyCodeType!, keyName);
            System.Reflection.MethodInfo method = _inputType!.GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(item => item.Name == "GetKeyDown"
                    && item.GetParameters().Length == 1
                    && item.GetParameters()[0].ParameterType == _keyCodeType);
            return Convert.ToBoolean(method.Invoke(null, new[] { key }), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void EnsureInputTypes()
        {
            _inputType ??= FindType("UnityEngine.Input")
                ?? throw new TypeLoadException("UnityEngine.Input was not found.");
            _keyCodeType ??= FindType("UnityEngine.KeyCode")
                ?? throw new TypeLoadException("UnityEngine.KeyCode was not found.");
        }

        private static Type? FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
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