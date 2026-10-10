using System.Collections.Generic;
using Verse;

namespace RecipeTweaker
{
    public class SavedIngredient : IExposable
    {
        public string thingDefName;
        public string filterLabel;
        public int count;

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingDefName, "thingDefName");
            Scribe_Values.Look(ref filterLabel, "filterLabel");
            Scribe_Values.Look(ref count, "count", 1);
        }
    }

    public class RecipeTweak : IExposable
    {
        public string recipeDefName;
        public string newLabel;
        public float newWorkAmount = -1f;
        public bool disabled;
        public bool hasCustomIngredients;
        public List<SavedIngredient> customIngredients = new List<SavedIngredient>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref recipeDefName, "recipeDefName");
            Scribe_Values.Look(ref newLabel, "newLabel");
            Scribe_Values.Look(ref newWorkAmount, "newWorkAmount", -1f);
            Scribe_Values.Look(ref disabled, "disabled", false);
            Scribe_Values.Look(ref hasCustomIngredients, "hasCustomIngredients", false);
            Scribe_Collections.Look(ref customIngredients, "customIngredients", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && customIngredients == null)
                customIngredients = new List<SavedIngredient>();
        }

        public bool IsVanilla => newLabel == null && newWorkAmount < 0f && !disabled && !hasCustomIngredients;
    }
}
