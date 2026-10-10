using System.Collections.Generic;
using Verse;

namespace RecipeTweaker
{
    public class RecipeTweakerSettings : ModSettings
    {
        public List<RecipeTweak> tweaks = new List<RecipeTweak>();

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref tweaks, "tweaks", LookMode.Deep);
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit && tweaks == null)
                tweaks = new List<RecipeTweak>();
        }

        public RecipeTweak GetOrCreate(string defName)
        {
            if (tweaks == null) tweaks = new List<RecipeTweak>();
            for (int i = 0; i < tweaks.Count; i++)
            {
                if (tweaks[i].recipeDefName == defName) return tweaks[i];
            }
            var t = new RecipeTweak { recipeDefName = defName };
            tweaks.Add(t);
            return t;
        }

        public void PruneVanilla()
        {
            tweaks?.RemoveAll(t => t.IsVanilla);
        }
    }
}
