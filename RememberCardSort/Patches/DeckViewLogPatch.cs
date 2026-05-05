using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace RememberCardSort.Patches;

// TODO: REMOVE — verification-only patch.
//
// Dumps the deck view's sort state (per-button IsDescending, the
// _sortIcon's FlipV + texture path, and the _sortingPriority list) every
// time the screen opens. Confirmed (godot2.log run, 2026-05-05) that the
// vanilla default is `_sortingPriority[0] == SortingOrders.Ascending`,
// IsDescending=false on all four buttons, and the icon texture is
// res://images/atlases/ui_atlas.sprites/sort_descending.tres unflipped —
// i.e. the user's "obtained downward".
//
// Kept around temporarily alongside DeckViewSortPatch so we can compare
// post-restore state against vanilla. Delete this file once the
// persistence behavior is verified end-to-end.
[HarmonyPatch(typeof(NDeckViewScreen), "_Ready")]
public static class DeckViewLogPatch
{
    private static readonly AccessTools.FieldRef<NDeckViewScreen, List<SortingOrders>> SortingPriorityRef =
        AccessTools.FieldRefAccess<NDeckViewScreen, List<SortingOrders>>("_sortingPriority");

    private static readonly string[] SorterFields =
    {
        "_obtainedSorter",
        "_typeSorter",
        "_costSorter",
        "_alphabetSorter",
    };

    public static void Postfix(NDeckViewScreen __instance)
    {
        try { Dump(__instance); }
        catch (Exception e) { MainFile.Logger.Error($"DeckViewLogPatch dump failed: {e}"); }
    }

    private static void Dump(NDeckViewScreen screen)
    {
        var log = MainFile.Logger;
        log.Info("=== RememberCardSort: NDeckViewScreen._Ready dump ===");

        var priority = SortingPriorityRef(screen);
        log.Info($"  _sortingPriority [{priority.Count}]: {string.Join(", ", priority)}");

        foreach (var fieldName in SorterFields)
        {
            var field = AccessTools.Field(typeof(NDeckViewScreen), fieldName);
            var btn = field?.GetValue(screen) as NCardViewSortButton;
            if (btn == null)
            {
                log.Warn($"  {fieldName}: <null>");
                continue;
            }

            var iconNode = ((Node)btn).GetNodeOrNull<TextureRect>("%Image");
            var flipV = iconNode != null && iconNode.FlipV;
            var texPath = iconNode?.Texture?.ResourcePath ?? "<no texture>";

            log.Info($"  {fieldName}: IsDescending={btn.IsDescending}  _sortIcon.FlipV={flipV}  texture={texPath}");
        }

        log.Info("=== /dump (look at the displayed arrow direction in-game to map FlipV → up/down) ===");
    }
}
