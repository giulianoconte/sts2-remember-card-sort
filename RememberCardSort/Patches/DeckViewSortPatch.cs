using System.Collections.Generic;
using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace RememberCardSort.Patches;

// Persists the deck-view sort across runs and highlights the active sort
// button green when it differs from the vanilla default (Obtained
// ascending = "obtained downward").
//
// _Ready postfix:    parse the saved priority list, write it onto the screen
//                    instance, redraw the cards, then refresh button colors.
// On*Sort postfixes: serialize the new priority list, save (debounced), then
//                    refresh button colors.
public static class DeckViewSortPatch
{
    // Active-sort highlight color. Same green slay-the-stats uses for
    // non-default filter chips (CompendiumFilterPatch.cs:23) so any user
    // running both mods sees a consistent "this differs from default" cue.
    private static readonly Color ActiveSortColor = new(0.4f, 0.85f, 0.5f, 1f);

    private static readonly AccessTools.FieldRef<NDeckViewScreen, List<SortingOrders>> SortingPriorityRef =
        AccessTools.FieldRefAccess<NDeckViewScreen, List<SortingOrders>>("_sortingPriority");

    [HarmonyPatch(typeof(NDeckViewScreen), "_Ready")]
    public static class ReadyPatch
    {
        public static void Postfix(NDeckViewScreen __instance)
        {
            try { Restore(__instance); }
            catch (Exception e) { MainFile.Logger.Error($"Restore failed: {e}"); }
        }
    }

    [HarmonyPatch(typeof(NDeckViewScreen), "OnObtainedSort")]
    public static class ObtainedSortPatch
    {
        public static void Postfix(NDeckViewScreen __instance) => OnSortChanged(__instance);
    }

    [HarmonyPatch(typeof(NDeckViewScreen), "OnCardTypeSort")]
    public static class CardTypeSortPatch
    {
        public static void Postfix(NDeckViewScreen __instance) => OnSortChanged(__instance);
    }

    [HarmonyPatch(typeof(NDeckViewScreen), "OnCostSort")]
    public static class CostSortPatch
    {
        public static void Postfix(NDeckViewScreen __instance) => OnSortChanged(__instance);
    }

    [HarmonyPatch(typeof(NDeckViewScreen), "OnAlphabetSort")]
    public static class AlphabetSortPatch
    {
        public static void Postfix(NDeckViewScreen __instance) => OnSortChanged(__instance);
    }

    private static void Restore(NDeckViewScreen screen)
    {
        if (!RememberCardSortConfig.TryParse(RememberCardSortConfig.DeckSortPriority, out var saved))
        {
            // Empty config (fresh install) or corrupt — leave vanilla state
            // alone, but still drive the highlight pass so we paint the
            // initial colors correctly.
            RefreshHighlights(screen);
            return;
        }

        var obtainedSorter = GetSorter(screen, "_obtainedSorter");
        var typeSorter     = GetSorter(screen, "_typeSorter");
        var costSorter     = GetSorter(screen, "_costSorter");
        var alphabetSorter = GetSorter(screen, "_alphabetSorter");
        if (obtainedSorter == null || typeSorter == null || costSorter == null || alphabetSorter == null)
        {
            MainFile.Logger.Warn("Restore aborted: a sort button is missing on NDeckViewScreen.");
            return;
        }

        var priority = SortingPriorityRef(screen);
        priority.Clear();
        priority.AddRange(saved);

        // Direction per button is fully determined by which variant of the
        // pair appears in the priority list. Use the public setter so its
        // OnToggle handler runs and updates the icon's FlipV — the scene's
        // arrow texture is "sort_descending", drawn unflipped when
        // IsDescending=true (arrow down) and flipped when IsDescending=false
        // (arrow up), and the setter is the only thing that keeps that
        // visual in sync with the state.
        obtainedSorter.IsDescending = priority.Contains(SortingOrders.Descending);
        typeSorter.IsDescending     = priority.Contains(SortingOrders.TypeDescending);
        costSorter.IsDescending     = priority.Contains(SortingOrders.CostDescending);
        alphabetSorter.IsDescending = priority.Contains(SortingOrders.AlphabetDescending);

        // _Ready already called DisplayCards once with the now-overwritten
        // vanilla priority. Call it again so the saved order takes effect.
        AccessTools.Method(typeof(NDeckViewScreen), "DisplayCards").Invoke(screen, null);

        RefreshHighlights(screen);
    }

    private static void OnSortChanged(NDeckViewScreen screen)
    {
        try
        {
            var priority = SortingPriorityRef(screen);
            RememberCardSortConfig.DeckSortPriority = RememberCardSortConfig.Format(priority);
            ModConfig.SaveDebounced<RememberCardSortConfig>();
            RefreshHighlights(screen);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"OnSortChanged failed: {e}");
        }
    }

    private static void RefreshHighlights(NDeckViewScreen screen)
    {
        var priority = SortingPriorityRef(screen);
        if (priority.Count == 0) return;
        var top = priority[0];
        var enabled = RememberCardSortConfig.HighlightActiveSort;

        SetLabelColor(GetSorter(screen, "_obtainedSorter"),
            enabled && top is SortingOrders.Descending);
        SetLabelColor(GetSorter(screen, "_typeSorter"),
            enabled && top is SortingOrders.TypeAscending or SortingOrders.TypeDescending);
        SetLabelColor(GetSorter(screen, "_costSorter"),
            enabled && top is SortingOrders.CostAscending or SortingOrders.CostDescending);
        SetLabelColor(GetSorter(screen, "_alphabetSorter"),
            enabled && top is SortingOrders.AlphabetAscending or SortingOrders.AlphabetDescending);
    }

    private static void SetLabelColor(NCardViewSortButton? button, bool isActiveNonDefault)
    {
        if (button == null) return;
        var label = ((Node)button).GetNodeOrNull<MegaLabel>("%Label");
        if (label == null) return;

        if (isActiveNonDefault)
            label.AddThemeColorOverride("font_color", ActiveSortColor);
        else
            label.RemoveThemeColorOverride("font_color");
    }

    private static NCardViewSortButton? GetSorter(NDeckViewScreen screen, string fieldName) =>
        AccessTools.Field(typeof(NDeckViewScreen), fieldName)?.GetValue(screen) as NCardViewSortButton;
}
