using System.Collections.Generic;
using Verse;

namespace RecipeTweaker
{
    public class CustomRecipeData : IExposable
    {
        public string defName;
        public string label;
        public string productDefName;
        public int productCount = 1;
        public float workAmount = 450f;
        public List<string> workbenchDefNames = new List<string>();
        public List<string> researchPrerequisiteDefNames = new List<string>();
        public List<SavedIngredient> ingredients = new List<SavedIngredient>();
        public int minSkillLevel = 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref productDefName, "productDefName");
            Scribe_Values.Look(ref productCount, "productCount", 1);
            Scribe_Values.Look(ref workAmount, "workAmount", 450f);
            Scribe_Values.Look(ref minSkillLevel, "minSkillLevel", 0);
            Scribe_Collections.Look(ref workbenchDefNames, "workbenchDefNames", LookMode.Value);
            Scribe_Collections.Look(ref researchPrerequisiteDefNames, "researchPrerequisiteDefNames", LookMode.Value);
            Scribe_Collections.Look(ref ingredients, "ingredients", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (workbenchDefNames == null) workbenchDefNames = new List<string>();
                if (researchPrerequisiteDefNames == null) researchPrerequisiteDefNames = new List<string>();
                if (ingredients == null) ingredients = new List<SavedIngredient>();
            }
        }
    }
}
