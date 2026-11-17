using Capstone.Core.Model;
using SharpQuest.Contracts;

namespace Capstone.Core.Search;

/// <summary>Searches a store with the course's <see cref="SearchMatcher"/> rules.</summary>
public static class ItemSearch
{
    /// <summary>
    /// Items whose display name matches, in store order. A Pokémon also matches on its types,
    /// so "fire" finds Charmander even when he has a nickname.
    /// </summary>
    public static IReadOnlyList<IItem> Find(IItemStore store, string query, SearchMatcher? matcher = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        matcher ??= SearchMatcher.Create();
        return store.All().Where(item => matcher.Matches(SearchText(item), query)).ToList();
    }

    private static string SearchText(IItem item) => item switch
    {
        Pokemon p => $"{p.DisplayName} {p.PrimaryType} {p.SecondaryType}",
        _ => item.DisplayName,
    };
}
