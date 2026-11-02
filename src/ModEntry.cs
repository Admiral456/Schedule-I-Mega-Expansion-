using MelonLoader;

[assembly: MelonInfo(typeof(ScheduleIMegaExpansion.ModEntry), "Schedule I Mega Expansion", "0.0.1", "Admiral456")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleIMegaExpansion;

/// <summary>Minimal loader smoke-test entry point. Gameplay systems wait for API spikes.</summary>
public sealed class ModEntry : MelonMod
{
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("Mega Expansion development scaffold loaded.");
        LoggerInstance.Msg("No province geometry or save data is modified by this build.");
    }
}