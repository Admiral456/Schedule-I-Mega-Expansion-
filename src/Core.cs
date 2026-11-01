using System;
using System.IO;
using MelonLoader;
using S1API.Lifecycle;
using ScheduleIMegaExpansion.Diagnostics;

[assembly: MelonInfo(typeof(ScheduleIMegaExpansion.Core), "Schedule I Mega Expansion", "0.1.0", "Admiral456")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleIMegaExpansion
{
    /// <summary>
    /// First runtime milestone. This mod only reads the native world registries
    /// and exports diagnostic files. It does not mutate game or save state.
    /// </summary>
    public sealed class Core : MelonMod
    {
        private bool _exportedForCurrentLoad;

        public override void OnInitializeMelon()
        {
            GameLifecycle.OnPreLoad += OnPreLoad;
            GameLifecycle.OnLoadComplete += OnLoadComplete;
            MelonLogger.Msg("[Schedule I Mega Expansion] Read-only world survey mode initialized.");
        }

        private void OnPreLoad()
        {
            _exportedForCurrentLoad = false;
        }

        private void OnLoadComplete()
        {
            if (_exportedForCurrentLoad)
                return;

            _exportedForCurrentLoad = true;

            try
            {
                string gameRoot = AppDomain.CurrentDomain.BaseDirectory;
                string outputRoot = Path.Combine(
                    gameRoot,
                    "UserData",
                    "ScheduleIMegaExpansion",
                    "Diagnostics");

                string reportPath = WorldSnapshotExporter.Export(
                    outputRoot,
                    message => MelonLogger.Msg("[Schedule I Mega Expansion] " + message),
                    message => MelonLogger.Warning("[Schedule I Mega Expansion] " + message));

                MelonLogger.Msg("[Schedule I Mega Expansion] Survey saved: " + reportPath);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Schedule I Mega Expansion] Survey export failed: " + ex);
            }
        }

        public override void OnApplicationQuit()
        {
            GameLifecycle.OnPreLoad -= OnPreLoad;
            GameLifecycle.OnLoadComplete -= OnLoadComplete;
        }
    }
}
