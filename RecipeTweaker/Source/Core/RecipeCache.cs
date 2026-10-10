using System;
using System.Collections.Generic;
using Verse;

namespace RecipeTweaker
{
    internal static class RecipeCache
    {
        private static Dictionary<string, List<RecipeDef>> _grouped;
        private static List<ModRecipeGroup> _filtered;
        private static string _lastSearch;

        public static bool IsBuilt => _grouped != null;

        public static void EnsureBuilt()
        {
            if (_grouped != null) return;
            var all = DefDatabase<RecipeDef>.AllDefsListForReading;
            _grouped = new Dictionary<string, List<RecipeDef>>(64, StringComparer.Ordinal);
            for (int i = 0; i < all.Count; i++)
            {
                var r = all[i];
                string m = r.modContentPack?.Name ?? "Unknown";
                if (!_grouped.TryGetValue(m, out var lst))
                {
                    lst = new List<RecipeDef>(32);
                    _grouped[m] = lst;
                }
                lst.Add(r);
            }
            foreach (var lst in _grouped.Values)
                lst.Sort((a, b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase));
            _filtered = null;
            _lastSearch = null;
        }

        public static List<ModRecipeGroup> GetFiltered(string search)
        {
            EnsureBuilt();
            if (_filtered != null && _lastSearch == search) return _filtered;
            _lastSearch = search;
            bool has = !string.IsNullOrEmpty(search);
            _filtered = new List<ModRecipeGroup>(_grouped.Count);
            foreach (var kv in _grouped)
            {
                if (!has)
                {
                    _filtered.Add(new ModRecipeGroup(kv.Key, kv.Value));
                    continue;
                }
                var matched = new List<RecipeDef>();
                for (int i = 0; i < kv.Value.Count; i++)
                    if (kv.Value[i].LabelCap.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                        matched.Add(kv.Value[i]);
                if (matched.Count > 0) _filtered.Add(new ModRecipeGroup(kv.Key, matched));
            }
            return _filtered;
        }
    }
}
