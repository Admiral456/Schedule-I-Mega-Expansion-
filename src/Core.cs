using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using ScheduleIMegaExpansion.Diagnostics;
using ScheduleIMegaExpansion.Runtime;

[assembly: MelonInfo(typeof(ScheduleIMegaExpansion.MegaExpansionMod), "Schedule I Mega Expansion", "0.2.0", "Admiral456")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleIMegaExpansion
{
    /// <summary>
    /// Runtime bootstrap for the Harborline prototype. S1API is discovered
    /// dynamically so this project does not ship game/API assemblies.
    /// </summary>
    public sealed class MegaExpansionMod : MelonMod
    {
        private EventInfo? _preLoadEvent;
        private EventInfo? _loadCompleteEvent;
        private EventInfo? _preSceneChangeEvent;
        private Delegate? _preLoadHandler;
        private Delegate? _loadCompleteHandler;
        private Delegate? _preSceneChangeHandler;
        private bool _subscribed;
        private bool _exportedForCurrentLoad;
        private bool _loggedMissingApi;
        private bool _loggedLifecycleError;
        private int _subscriptionPollFrames;
        private bool _saveLoaded;
        private int _generationPollFrames;
        private bool _harborlineGenerated;
        private int _gameplayPollFrames;
        private bool _gameplayInitialized;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("[Schedule I Mega Expansion] Harborline runtime initialized; awaiting native save load.");
            TrySubscribeToS1ApiLifecycle();
        }

        public override void OnUpdate()
        {
            if (_saveLoaded && !_harborlineGenerated)
            {
                _generationPollFrames++;
                if (_generationPollFrames >= 120)
                {
                    _generationPollFrames = 0;
                    string gameRoot = AppDomain.CurrentDomain.BaseDirectory;
                    _harborlineGenerated = HarborlinePhysicalBuilder.TryAutoGenerate(
                        gameRoot,
                        message => MelonLogger.Msg("[Schedule I Mega Expansion] " + message),
                        message => MelonLogger.Warning("[Schedule I Mega Expansion] " + message));
                }
            }

            if (_saveLoaded && _harborlineGenerated && !_gameplayInitialized)
            {
                _gameplayPollFrames++;
                if (_gameplayPollFrames >= 60)
                {
                    _gameplayPollFrames = 0;
                    _gameplayInitialized = HarborlineGameplayBootstrap.TryActivate(
                        message => MelonLogger.Msg("[Schedule I Mega Expansion] " + message),
                        message => MelonLogger.Warning("[Schedule I Mega Expansion] " + message));
                }
            }

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
                EventInfo? preSceneChange = lifecycleType.GetEvent(
                    "OnPreSceneChange",
                    BindingFlags.Public | BindingFlags.Static);

                MethodInfo preMethod = typeof(MegaExpansionMod).GetMethod(
                    nameof(OnPreLoad),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;
                MethodInfo loadMethod = typeof(MegaExpansionMod).GetMethod(
                    nameof(OnLoadComplete),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;
                MethodInfo sceneChangeMethod = typeof(MegaExpansionMod).GetMethod(
                    nameof(OnPreSceneChange),
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

                if (preSceneChange?.EventHandlerType != null)
                {
                    _preSceneChangeHandler = Delegate.CreateDelegate(preSceneChange.EventHandlerType, this, sceneChangeMethod);
                    preSceneChange.AddEventHandler(null, _preSceneChangeHandler);
                    _preSceneChangeEvent = preSceneChange;
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
            _saveLoaded = false;
            _harborlineGenerated = false;
            _generationPollFrames = 0;
            _gameplayPollFrames = 0;
            _gameplayInitialized = false;
            HarborlinePhysicalBuilder.ResetForNewSave();
            HarborlineGameplayBootstrap.ResetForNewSave();
        }

        private void OnPreSceneChange()
        {
            _saveLoaded = false;
            _exportedForCurrentLoad = false;
            _harborlineGenerated = false;
            _generationPollFrames = 0;
            _gameplayPollFrames = 0;
            _gameplayInitialized = false;
            HarborlinePhysicalBuilder.ResetForNewSave();
            HarborlineGameplayBootstrap.ResetForNewSave();
        }

        private void OnLoadComplete()
        {
            if (_exportedForCurrentLoad)
                return;

            _exportedForCurrentLoad = true;
            _saveLoaded = true;

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
                if (_preSceneChangeEvent != null && _preSceneChangeHandler != null)
                    _preSceneChangeEvent.RemoveEventHandler(null, _preSceneChangeHandler);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Schedule I Mega Expansion] Lifecycle unsubscription warning: " + ex.Message);
            }
        }
    }
}
