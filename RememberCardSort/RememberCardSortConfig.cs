using System.Collections.Generic;
using BaseLib.Config;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace RememberCardSort;

internal class RememberCardSortConfig : SimpleModConfig
{
    /// <summary>
    /// Comma-joined SortingOrders names representing the deck-view sort priority list.
    /// Empty string => use vanilla default (Ascending, TypeAscending, CostAscending,
    /// AlphabetAscending). Hidden from BaseLib's auto-UI; this is driven by clicks
    /// on the in-run deck view sort buttons, not a settings page.
    /// </summary>
    [ConfigHideInUI] public static string DeckSortPriority { get; set; } = "";

    // Each "category" has exactly two SortingOrders variants, one ascending and one
    // descending. The priority list must contain exactly one variant per category,
    // in some order, so direction-per-button is fully determined by which variant
    // appears.
    private static readonly SortingOrders[][] Categories =
    {
        new[] { SortingOrders.Ascending,         SortingOrders.Descending },
        new[] { SortingOrders.TypeAscending,     SortingOrders.TypeDescending },
        new[] { SortingOrders.CostAscending,     SortingOrders.CostDescending },
        new[] { SortingOrders.AlphabetAscending, SortingOrders.AlphabetDescending },
    };

    public static List<SortingOrders> VanillaDefault() => new()
    {
        SortingOrders.Ascending,
        SortingOrders.TypeAscending,
        SortingOrders.CostAscending,
        SortingOrders.AlphabetAscending,
    };

    /// <summary>
    /// Format a priority list to the persisted string form.
    /// </summary>
    public static string Format(List<SortingOrders> priority) =>
        string.Join(",", priority);

    /// <summary>
    /// Parse the persisted string into a priority list. Returns false if the string
    /// is empty, malformed, has the wrong number of entries, or doesn't include
    /// exactly one variant per category — in which case the caller should fall
    /// back to the vanilla default.
    /// </summary>
    public static bool TryParse(string raw, out List<SortingOrders> priority)
    {
        priority = new List<SortingOrders>(4);
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var tokens = raw.Split(',');
        if (tokens.Length != 4) return false;

        foreach (var token in tokens)
        {
            if (!Enum.TryParse<SortingOrders>(token.Trim(), out var value)) return false;
            priority.Add(value);
        }

        // Exactly one per category, no duplicates.
        foreach (var category in Categories)
        {
            int hits = 0;
            foreach (var v in priority)
                if (v == category[0] || v == category[1]) hits++;
            if (hits != 1) return false;
        }

        return true;
    }

    /// <summary>
    /// True iff <paramref name="top"/> is the vanilla-default top sort
    /// (Obtained ascending = "obtained downward" in the user's terms). All
    /// other top sorts trigger the green active-sort highlight.
    /// </summary>
    public static bool IsDefaultTop(SortingOrders top) => top == SortingOrders.Ascending;
}
