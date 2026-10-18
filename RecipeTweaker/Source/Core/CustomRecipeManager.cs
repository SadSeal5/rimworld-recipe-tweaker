using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace RecipeTweaker
{
    public static class CustomRecipeManager
    {
        private static readonly FieldInfo allRecipesCachedField = typeof(ThingDef).GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool IsCustomRecipe(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return false;
            var list = RecipeTweakerMod.Settings?.customRecipes;
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].defName == defName) return true;
            }
            return false;
        }

        public static CustomRecipeData GetCustomRecipe(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return null;
            var list = RecipeTweakerMod.Settings?.customRecipes;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].defName == defName) return list[i];
            }
            return null;
        }

        public static void InitializeAll()
        {
            var list = RecipeTweakerMod.Settings?.customRecipes;
            if (list == null || list.Count == 0) return;
            for (int i = 0; i < list.Count; i++)
            {
                BuildAndRegister(list[i], false);
            }
        }

        public static RecipeDef BuildAndRegister(CustomRecipeData data, bool addToSettings = true)
        {
            if (data == null || string.IsNullOrEmpty(data.defName)) return null;

            var r = DefDatabase<RecipeDef>.GetNamed(data.defName, false) ?? new RecipeDef();
            r.defName = data.defName;
            r.label = data.label;
            r.jobString = "Üretiyor: " + data.label;
            r.workAmount = data.workAmount;
            r.workSpeedStat = StatDefOf.GeneralLaborSpeed;
            r.effectWorking = DefDatabase<EffecterDef>.GetNamedSilentFail("Cook");
            r.soundWorking = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_CookMeal");

            // Product
            var prodDef = DefDatabase<ThingDef>.GetNamed(data.productDefName, false);
            if (prodDef != null)
            {
                r.products = new List<ThingDefCountClass> { new ThingDefCountClass(prodDef, data.productCount) };
            }

            // Ingredients & Filters
            r.ingredients = new List<IngredientCount>();
            r.fixedIngredientFilter = new ThingFilter();
            r.defaultIngredientFilter = new ThingFilter();

            if (data.ingredients != null)
            {
                for (int i = 0; i < data.ingredients.Count; i++)
                {
                    var s = data.ingredients[i];
                    var td = DefDatabase<ThingDef>.GetNamed(s.thingDefName, false);
                    if (td != null)
                    {
                        var ing = new IngredientCount();
                        ing.filter.SetAllow(td, true);
                        ing.SetBaseCount(s.count);
                        r.ingredients.Add(ing);
                        r.fixedIngredientFilter.SetAllow(td, true);
                        r.defaultIngredientFilter.SetAllow(td, true);
                    }
                }
            }

            // Workbenches
            if (r.recipeUsers == null) r.recipeUsers = new List<ThingDef>();
            if (data.workbenchDefNames != null)
            {
                for (int i = 0; i < data.workbenchDefNames.Count; i++)
                {
                    string wbName = data.workbenchDefNames[i];
                    var wb = DefDatabase<ThingDef>.GetNamed(wbName, false);
                    if (wb != null)
                    {
                        if (!r.recipeUsers.Contains(wb)) r.recipeUsers.Add(wb);
                        if (wb.recipes == null) wb.recipes = new List<RecipeDef>();
                        if (!wb.recipes.Contains(r)) wb.recipes.Add(r);
                        var cached = allRecipesCachedField?.GetValue(wb) as List<RecipeDef>;
                        if (cached != null && !cached.Contains(r)) cached.Add(r);
                    }
                }
            }

            // Research Prerequisites
            r.researchPrerequisites = new List<ResearchProjectDef>();
            if (data.researchPrerequisiteDefNames != null)
            {
                for (int i = 0; i < data.researchPrerequisiteDefNames.Count; i++)
                {
                    string resName = data.researchPrerequisiteDefNames[i];
                    var res = DefDatabase<ResearchProjectDef>.GetNamed(resName, false);
                    if (res != null) r.researchPrerequisites.Add(res);
                }
            }
            if (r.researchPrerequisites.Count == 1) r.researchPrerequisite = r.researchPrerequisites[0];
            else r.researchPrerequisite = null;

            // Skill requirement
            if (data.minSkillLevel > 0)
            {
                r.skillRequirements = new List<SkillRequirement>
                {
                    new SkillRequirement { skill = SkillDefOf.Crafting, minLevel = data.minSkillLevel }
                };
            }
            else
            {
                r.skillRequirements = null;
            }

            // Register in DefDatabase
            if (DefDatabase<RecipeDef>.GetNamed(data.defName, false) == null)
            {
                DefDatabase<RecipeDef>.Add(r);
            }

            // Add to settings
            if (addToSettings)
            {
                var settings = RecipeTweakerMod.Settings;
                if (settings != null)
                {
                    if (settings.customRecipes == null) settings.customRecipes = new List<CustomRecipeData>();
                    settings.customRecipes.RemoveAll(x => x.defName == data.defName);
                    settings.customRecipes.Add(data);
                    settings.Write();
                }
            }

            RecipeCache.Rebuild();
            return r;
        }

        public static void RemoveCustomRecipe(CustomRecipeData data)
        {
            if (data == null) return;
            var r = DefDatabase<RecipeDef>.GetNamed(data.defName, false);
            if (r != null)
            {
                if (r.recipeUsers != null)
                {
                    for (int i = 0; i < r.recipeUsers.Count; i++)
                    {
                        var wb = r.recipeUsers[i];
                        wb.recipes?.Remove(r);
                        var cached = allRecipesCachedField?.GetValue(wb) as List<RecipeDef>;
                        cached?.Remove(r);
                        allRecipesCachedField?.SetValue(wb, null);
                    }
                }

                var allThings = DefDatabase<ThingDef>.AllDefsListForReading;
                if (allThings != null)
                {
                    for (int i = 0; i < allThings.Count; i++)
                    {
                        var wb = allThings[i];
                        wb.recipes?.Remove(r);
                        var cached = allRecipesCachedField?.GetValue(wb) as List<RecipeDef>;
                        cached?.Remove(r);
                        allRecipesCachedField?.SetValue(wb, null);
                    }
                }

                var removeMethod = typeof(DefDatabase<RecipeDef>).GetMethod("Remove", BindingFlags.NonPublic | BindingFlags.Static);
                removeMethod?.Invoke(null, new object[] { r });
            }

            var settings = RecipeTweakerMod.Settings;
            if (settings?.customRecipes != null)
            {
                settings.customRecipes.RemoveAll(x => x.defName == data.defName);
                settings.Write();
            }

            RecipeCache.Rebuild();
        }
    }
}
