using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RecipeTweaker {
    public class VanillaSnapshot {
        public string Label;
        public float WorkAmount;
        public float WorkToMake = -1f;
        public List<IngredientCount> Ingredients;
        public List<ThingDef> RecipeUsers;
    }

    [StaticConstructorOnStartup]
    public static class RecipePatchApplier {
        public static Dictionary<string, VanillaSnapshot> VanillaSnapshots = new Dictionary<string, VanillaSnapshot>();

        static RecipePatchApplier() {
            ApplyAll();
        }

        public static void EnsureVanillaSaved(RecipeDef r) {
            if (r == null || VanillaSnapshots.ContainsKey(r.defName)) return;
            var snap = new VanillaSnapshot {
                Label = r.label,
                WorkAmount = r.workAmount,
                Ingredients = r.ingredients != null ? new List<IngredientCount>(r.ingredients) : new List<IngredientCount>(),
                RecipeUsers = r.recipeUsers != null ? new List<ThingDef>(r.recipeUsers) : new List<ThingDef>()
            };
            if (r.products != null && r.products.Count > 0 && r.products[0].thingDef != null) {
                snap.WorkToMake = r.products[0].thingDef.GetStatValueAbstract(StatDefOf.WorkToMake);
            }
            VanillaSnapshots[r.defName] = snap;
        }

        public static void ApplyAll() {
            var settings = RecipeTweakerMod.Settings;
            if (settings?.tweaks == null || settings.tweaks.Count == 0) return;
            for (int i = 0; i < settings.tweaks.Count; i++) {
                var tw = settings.tweaks[i];
                var r = DefDatabase<RecipeDef>.GetNamed(tw.recipeDefName, false);
                if (r == null) continue;
                EnsureVanillaSaved(r);
                ApplyTweak(r, tw);
            }
        }

        public static void ApplyTweak(RecipeDef recipe, RecipeTweak tweak) {
            EnsureVanillaSaved(recipe);

            if (!string.IsNullOrEmpty(tweak.newLabel)) recipe.label = tweak.newLabel;

            // Hem tarif workAmount hem de esyanin WorkToMake degeri guncellenir (CRAFTING FIX)
            if (tweak.newWorkAmount >= 0f) {
                recipe.workAmount = tweak.newWorkAmount;
                if (recipe.products != null) {
                    for (int p = 0; p < recipe.products.Count; p++) {
                        var prod = recipe.products[p].thingDef;
                        if (prod != null) {
                            prod.SetStatBaseValue(StatDefOf.WorkToMake, tweak.newWorkAmount);
                        }
                    }
                }
            }

            if (tweak.hasCustomIngredients && tweak.customIngredients != null) {
                recipe.ingredients = new List<IngredientCount>();
                if (recipe.fixedIngredientFilter != null)
                    recipe.fixedIngredientFilter.SetDisallowAll();

                for (int i = 0; i < tweak.customIngredients.Count; i++) {
                    var s = tweak.customIngredients[i];
                    var td = DefDatabase<ThingDef>.GetNamed(s.thingDefName, false);
                    if (td != null) {
                        var ing = new IngredientCount();
                        ing.filter.SetAllow(td, true);
                        ing.SetBaseCount(s.count);
                        recipe.ingredients.Add(ing);
                        recipe.fixedIngredientFilter?.SetAllow(td, true);
                    }
                }
            }

            if (tweak.disabled) recipe.recipeUsers?.Clear();
        }

        public static void ResetToVanilla(RecipeDef recipe) {
            if (recipe == null) return;
            if (VanillaSnapshots.TryGetValue(recipe.defName, out var snap)) {
                recipe.label = snap.Label;
                recipe.workAmount = snap.WorkAmount;
                recipe.ingredients = new List<IngredientCount>(snap.Ingredients);
                recipe.recipeUsers = new List<ThingDef>(snap.RecipeUsers);

                if (snap.WorkToMake >= 0f && recipe.products != null) {
                    for (int p = 0; p < recipe.products.Count; p++) {
                        recipe.products[p].thingDef?.SetStatBaseValue(StatDefOf.WorkToMake, snap.WorkToMake);
                    }
                }
            }
        }
    }
}