using System.Collections.Generic;
using Verse;

namespace RecipeTweaker
{
    public class ModRecipeGroup
    {
        public string ModName;
        public List<RecipeDef> Recipes;

        public ModRecipeGroup(string name, List<RecipeDef> recipes)
        {
            ModName = name;
            Recipes = recipes;
        }
    }
}
