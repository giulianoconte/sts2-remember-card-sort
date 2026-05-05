using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace RememberCardSort;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "RememberCardSort";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Logger.Info($"{ModId}: Initializing...");

        // Construct + register the config first. The constructor loads the
        // saved values from disk (or writes defaults). Registration enables
        // the auto-save-on-quit hook in BaseLib.
        var config = new RememberCardSortConfig();
        ModConfigRegistry.Register(ModId, config);

        Harmony harmony = new(ModId);
        foreach (var type in typeof(MainFile).Assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
            try { harmony.CreateClassProcessor(type).Patch(); }
            catch (Exception e) { Logger.Warn($"{ModId}: Patch {type.Name} skipped — {e.Message}"); }
        }

        Logger.Info($"{ModId}: Initialized (saved sort: '{RememberCardSortConfig.DeckSortPriority}').");
    }
}
