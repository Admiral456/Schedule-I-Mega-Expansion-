using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using ScheduleIMegaExpansion.Diagnostics;

[assembly: MelonInfo(typeof(ScheduleIMegaExpansion.MegaExpansionMod), "Schedule I Mega Expansion", "0.1.0", "Admiral456")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleIMegaExpansion
{
    /// <summary>
    /// Runtime diagnostic bootstrap. S1API is discovered dynamically so this
    /// project can compile without shipping or referencing game assemblies.
    /// </summary>
    public sealed class MegaExpansionMod : MelonMod
    {
        private EventInfo? _preLoadEvent;
        private EventInfo? _loadCompleteEvent;
        private Delegate? _preLoadHandler;
        private Delegate? _loadCompleteHandler;
        private bool _subscribed;
        private bool _exportedForCurrentLoad;
        private bool _loggedMissingApi;
        private bool _loggedLifecycleError;
        private int _subscriptionPollFrames;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[Schedule I Mega Expansion] Read-only world survey initialized.");
            TrySubscribeToS1ApiLifecycle();
        }

        public override void OnUpdate()
        {
            if (_subscribed)
                return;

            _subscriptionPollFrames++;
            if (_subscriptionPollFrames < 120)
                return;

            _subscriptionPollFrames = 0;
            TrySubscribeToS1ApiLifecycle();
        }

        private void TrySubscribeToS1ApiLifecycle()
        {
            Type? lifecycleType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly =>
                {
                    try { return assembly.GetType("S1API.Lifecycle.GameLifecycle", false); }
                    catch { return null; }
                })
                .FirstOrDefault(type => type != null);

            if (lifecycleType == null)
            {
                if (!_loggedMissingApi)
                {
                    _loggedMissingApi = true;
                    MelonLogger.Warning("[Schedule I Mega Expansion] Waiting for S1API lifecycle API.");
                }
                return;
            }

            try
            {
                EventInfo? preLoad = lifecycleType.GetEvent(
                    "OnPreLoad",
                    BindingFlags.Public | BindingFlags.Static);
                EventInfo? loadComplete = lifecycleType.GetEvent(
                    "OnLoadComplete",
                    BindingFlags.Public | BindingFlags.Static);

                MethodInfo preMethod = typeof(MegaExpansionMod).GetMethod(
                    nameof(OnPreLoad),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;
                MethodInfo loadMethod = typeof(MegaExpansionMod).GetMethod(
                    nameof(OnLoadComplete),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

                if (loadComplete?.EventHandlerType == null)
                    throw new MissingMemberException("S1API GameLifecycle.OnLoadComplete event was not found.");

                _loadCompleteHandler = Delegate.CreateDelegate(loadComplete.EventHandlerType, this, loadMethod);
                loadComplete.AddEventHandler(null, _loadCompleteHandler);
                _loadCompleteEvent = loadComplete;

                if (preLoad?.EventHandlerType != null)
                {
                    _preLoadHandler = Delegate.CreateDelegate(preLoad.EventHandlerType, this, preMethod);
                    preLoad.AddEventHandler(null, _preLoadHandler);
                    _preLoadEvent = preLoad;
                }

                _subscribed = true;
                MelonLogger.Msg("[Schedule I Mega Expansion] Connected to native S1API save lifecycle.");
            }
            catch (Exception ex)
            {
                if (!_loggedLifecycleError)
                {
                    _loggedLifecycleError = true;
                    MelonLogger.Warning("[Schedule I Mega Expansion] Could not connect to S1API lifecycle: " + ex.Message);
                }
            }
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
                string outputRoot = System.IO.Path.Combine(
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
            try
            {
                if (_preLoadEvent != null && _preLoadHandler != null)
                    _preLoadEvent.RemoveEventHandler(null, _preLoadHandler);
                if (_loadCompleteEvent != null && _loadCompleteHandler != null)
                    _loadCompleteEvent.RemoveEventHandler(null, _loadCompleteHandler);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Lifecycle unsubscription warning: " + ex.Message);
            }
        }
    }
}
