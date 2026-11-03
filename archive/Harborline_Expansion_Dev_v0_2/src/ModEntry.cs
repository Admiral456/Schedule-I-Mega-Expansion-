using System;
using MelonLoader;
using HarborlineExpansion.Config;
using HarborlineExpansion.World;

[assembly: MelonInfo(typeof(HarborlineExpansion.ModEntry), "Harborline Expansion", "0.1.0", "Community / ChatGPT")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace HarborlineExpansion
{
    public sealed class ModEntry : MelonMod
    {
        private bool _attempted;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Harborline Expansion 0.1.0 loaded. Visual prototype is disabled by default.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (_attempted || !string.Equals(sceneName, "Main", StringComparison.OrdinalIgnoreCase))
                return;

            _attempted = true;

            try
            {
                ExpansionConfig config = ExpansionConfig.Load();
                if (!config.Enabled)
                {
                    LoggerInstance.Msg("Harborline prototype is disabled. Set Enabled=true in UserData/HarborlineExpansion.cfg to test.");
                    return;
                }

                HarborDistrictPrototype.Spawn(config, message => LoggerInstance.Msg(message));
            }
            catch (Exception ex)
            {
                LoggerInstance.Error("Harborline failed to create its prototype geometry.");
                LoggerInstance.Error(ex.ToString());
            }
        }
    }
}