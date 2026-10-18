using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RecipeTweaker
{
    public class WorkingIngredient
    {
        public string ThingDefName;
        public string Label;
        public int Count;
    }

    public class Dialog_RecipeTweaker : Window
    {
        public override Vector2 InitialSize => new Vector2(980f, 680f);
        private const float LEFT_W = 290f, ROW_H = 26f, HDR_H = 24f, SEARCH_H = 28f, GAP = 6f, BTN_H = 32f, LBL_W = 104f, SPINNER_W = 26f, COUNT_W = 44f;
        private static readonly Color ColHeader = new Color(0.13f, 0.13f, 0.13f, 1f), ColRowAlt = new Color(0.1f, 0.1f, 0.1f, 0.35f), ColSelected = new Color(0.3f, 0.2f, 0.04f, 1f), ColGray = new Color(0.6f, 0.6f, 0.6f, 1f);

        private Vector2 _listScroll;
        private float _totalListH;
        private string _searchText = string.Empty, _prevSearch;
        private List<ModRecipeGroup> _currentFiltered;
        private RecipeDef _selected;

        private string _editLabel, _editWork;
        private bool _editDisabled;
        private List<WorkingIngredient> _workingIngredients = new List<WorkingIngredient>();

        public Dialog_RecipeTweaker()
        {
            doCloseX = true;
            resizeable = true;
            draggable = true;
            RecipeCache.EnsureBuilt();
            RefreshFilter();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (_searchText != _prevSearch) { RefreshFilter(); _prevSearch = _searchText; }

            if (Widgets.ButtonText(new Rect(inRect.x, inRect.y, 160f, 30f), "+ Yeni Tarif Ekle"))
            {
                Find.WindowStack.Add(new Dialog_CreateCustomRecipe(() =>
                {
                    RefreshFilter();
                }));
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x + 175f, inRect.y + 2f, 450f, 30f), "Recipe & Crafting Tweaker");
            Text.Font = GameFont.Small;

            float cy = inRect.y + 36f, ch = inRect.height - 36f;
            var leftR = new Rect(inRect.x, cy, LEFT_W, ch);
            var divX = inRect.x + LEFT_W + GAP;
            var rightR = new Rect(divX + 1f + GAP, cy, inRect.width - LEFT_W - GAP * 2f - 1f, ch);

            Widgets.DrawLineVertical(divX, cy, ch);
            DrawLeft(leftR);
            DrawRight(rightR);
        }

        private void DrawLeft(Rect rect)
        {
            _searchText = Widgets.TextField(new Rect(rect.x, rect.y, rect.width, SEARCH_H), _searchText);
            var outer = new Rect(rect.x, rect.y + SEARCH_H + GAP, rect.width, rect.height - SEARCH_H - GAP);
            var view = new Rect(0f, 0f, outer.width - 16f, _totalListH);
            Widgets.BeginScrollView(outer, ref _listScroll, view);
            float y = 0f, visTop = _listScroll.y, visBot = _listScroll.y + outer.height;

            if (_currentFiltered != null)
            {
                for (int g = 0; g < _currentFiltered.Count; g++)
                {
                    var entry = _currentFiltered[g];
                    if (y + HDR_H > visTop && y < visBot)
                    {
                        Widgets.DrawBoxSolid(new Rect(0f, y, view.width, HDR_H), ColHeader);
                        GUI.color = ColGray; Text.Font = GameFont.Tiny;
                        Widgets.Label(new Rect(5f, y + 4f, view.width - 5f, HDR_H), entry.ModName.ToUpperInvariant());
                        Text.Font = GameFont.Small; GUI.color = Color.white;
                    }
                    y += HDR_H;

                    for (int i = 0; i < entry.Recipes.Count; i++)
                    {
                        var r = entry.Recipes[i];
                        if (y + ROW_H > visTop && y < visBot)
                        {
                            var rowR = new Rect(0f, y, view.width, ROW_H);
                            if (r == _selected) Widgets.DrawBoxSolid(rowR, ColSelected);
                            else if (i % 2 == 1) Widgets.DrawBoxSolid(rowR, ColRowAlt);
                            if (Mouse.IsOver(rowR)) Widgets.DrawHighlight(rowR);

                            bool isCust = CustomRecipeManager.IsCustomRecipe(r.defName);
                            bool hasTweak = HasTweak(r.defName);
                            var iconR = new Rect(4f, y + 5f, 16f, 16f);
                            if (r.UIIcon != null) GUI.DrawTexture(iconR, r.UIIcon);

                            var txtR = new Rect(24f, y + 3f, view.width - 28f, ROW_H - 3f);
                            if (isCust) GUI.color = Color.cyan;
                            else if (hasTweak) GUI.color = Color.yellow;
                            Widgets.Label(txtR, r.LabelCap);
                            if (isCust || hasTweak) GUI.color = Color.white;

                            if (Widgets.ButtonInvisible(rowR)) SelectRecipe(r);
                        }
                        y += ROW_H;
                    }
                }
            }
            Widgets.EndScrollView();
        }

        private bool HasTweak(string defName)
        {
            var tweaks = RecipeTweakerMod.Settings?.tweaks;
            if (tweaks == null) return false;
            for (int i = 0; i < tweaks.Count; i++)
                if (tweaks[i].recipeDefName == defName && !tweaks[i].IsVanilla) return true;
            return false;
        }

        private void DrawRight(Rect rect)
        {
            if (_selected == null)
            {
                GUI.color = ColGray;
                Widgets.Label(new Rect(rect.x + 12f, rect.y + 20f, 300f, 24f), "Sol listeden bir tarif secin.");
                GUI.color = Color.white;
                return;
            }

            float y = rect.y, w = rect.width;
            bool isCustom = CustomRecipeManager.IsCustomRecipe(_selected.defName);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, y, w - (isCustom ? 120f : 0f), 28f), _selected.LabelCap);
            if (isCustom)
            {
                GUI.color = Color.cyan;
                Widgets.Label(new Rect(rect.x + w - 110f, y + 2f, 110f, 26f), "[Özel Tarif]");
                GUI.color = Color.white;
            }
            Text.Font = GameFont.Small;
            y += 32f;

            GUI.color = ColGray;
            Widgets.Label(new Rect(rect.x, y, w, 20f), "defName: " + _selected.defName + " | Mod: " + (_selected.modContentPack?.Name ?? "Vanilla"));
            GUI.color = Color.white;
            y += 24f;
            Widgets.DrawLineHorizontal(rect.x, y, w);
            y += GAP * 2;

            Widgets.Label(new Rect(rect.x, y + 2f, LBL_W, 24f), "Gosterim Adi:");
            _editLabel = Widgets.TextField(new Rect(rect.x + LBL_W, y, w - LBL_W, 24f), _editLabel);
            y += 24f + GAP;

            Widgets.Label(new Rect(rect.x, y + 2f, LBL_W, 24f), "Uretim Suresi (Work):");
            _editWork = Widgets.TextField(new Rect(rect.x + LBL_W, y, 100f, 24f), _editWork);
            y += 24f + GAP;

            Widgets.CheckboxLabeled(new Rect(rect.x, y, 200f, 24f), "Tarifi Devre Disi Birak", ref _editDisabled);
            y += 24f + GAP * 2;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, y, 200f, 26f), "Malzemeler");
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(new Rect(rect.x + w - 140f, y, 140f, 26f), "+ Malzeme Ekle"))
            {
                Find.WindowStack.Add(new Dialog_IngredientPicker(td =>
                {
                    _workingIngredients.Add(new WorkingIngredient
                    {
                        ThingDefName = td.defName,
                        Label = td.LabelCap,
                        Count = 1
                    });
                }));
            }
            y += 30f;

            float listH = rect.height - (y - rect.y) - (BTN_H + GAP * 2);
            var ingR = new Rect(rect.x, y, w, listH);
            Widgets.DrawMenuSection(ingR);

            float iy = ingR.y + 4f;
            for (int i = 0; i < _workingIngredients.Count; i++)
            {
                var ing = _workingIngredients[i];
                var rowR = new Rect(ingR.x + 4f, iy, ingR.width - 8f, 28f);
                if (i % 2 == 1) Widgets.DrawBoxSolid(rowR, ColRowAlt);

                Widgets.Label(new Rect(rowR.x + 4f, iy + 4f, rowR.width - 240f, 22f), ing.Label);

                if (Widgets.ButtonText(new Rect(rowR.x + rowR.width - 210f, iy + 2f, SPINNER_W, 24f), "-"))
                    if (ing.Count > 1) ing.Count--;

                string cntStr = Widgets.TextField(new Rect(rowR.x + rowR.width - 180f, iy + 2f, COUNT_W, 24f), ing.Count.ToString());
                if (int.TryParse(cntStr, out int nc)) ing.Count = Mathf.Max(1, nc);

                if (Widgets.ButtonText(new Rect(rowR.x + rowR.width - 132f, iy + 2f, SPINNER_W, 24f), "+"))
                    ing.Count++;

                if (Widgets.ButtonText(new Rect(rowR.x + rowR.width - 70f, iy + 2f, 65f, 24f), "Sil"))
                {
                    _workingIngredients.RemoveAt(i);
                    i--;
                }
                iy += 30f;
            }

            float by = rect.y + rect.height - BTN_H;
            if (Widgets.ButtonText(new Rect(rect.x, by, 120f, BTN_H), "Kaydet")) SaveChanges();
            if (Widgets.ButtonText(new Rect(rect.x + 130f, by, 140f, BTN_H), "Orijinale Sifirla")) ResetToFactory();

            if (isCustom)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                if (Widgets.ButtonText(new Rect(rect.x + rect.width - 120f, by, 120f, BTN_H), "Tarifi Sil"))
                {
                    var customData = CustomRecipeManager.GetCustomRecipe(_selected.defName);
                    if (customData != null)
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "'" + _selected.LabelCap + "' ozel tarifini tamamen silmek istediginize emin misiniz?",
                            () =>
                            {
                                CustomRecipeManager.RemoveCustomRecipe(customData);
                                _selected = null;
                                RefreshFilter();
                                Messages.Message("Ozel tarif silindi.", MessageTypeDefOf.PositiveEvent, false);
                            },
                            destructive: true
                        ));
                    }
                }
                GUI.color = Color.white;
            }
        }

        private void RefreshFilter()
        {
            _currentFiltered = RecipeCache.GetFiltered(_searchText);
            _totalListH = 0f;
            if (_currentFiltered != null)
            {
                for (int i = 0; i < _currentFiltered.Count; i++)
                    _totalListH += HDR_H + _currentFiltered[i].Recipes.Count * ROW_H;
            }
        }

        private void SelectRecipe(RecipeDef r)
        {
            _selected = r;
            RecipePatchApplier.EnsureVanillaSaved(r);
            var tw = RecipeTweakerMod.Settings?.GetOrCreate(r.defName) ?? new RecipeTweak { recipeDefName = r.defName };

            _editLabel = tw.newLabel ?? r.LabelCap.ToString();

            float currentWork = r.workAmount;
            if (currentWork < 0f && r.products != null && r.products.Count > 0 && r.products[0].thingDef != null)
            {
                currentWork = r.products[0].thingDef.GetStatValueAbstract(StatDefOf.WorkToMake);
            }
            if (currentWork < 0f) currentWork = 450f;

            _editWork = (tw.newWorkAmount >= 0f ? tw.newWorkAmount : currentWork).ToString("F0");
            _editDisabled = tw.disabled;

            _workingIngredients.Clear();
            if (tw.hasCustomIngredients && tw.customIngredients != null)
            {
                for (int i = 0; i < tw.customIngredients.Count; i++)
                {
                    var s = tw.customIngredients[i];
                    _workingIngredients.Add(new WorkingIngredient
                    {
                        ThingDefName = s.thingDefName,
                        Label = s.filterLabel,
                        Count = s.count
                    });
                }
            }
            else if (r.ingredients != null)
            {
                for (int i = 0; i < r.ingredients.Count; i++)
                {
                    var ing = r.ingredients[i];
                    string defName = ing.filter.AnyAllowedDef?.defName ?? "Steel";
                    _workingIngredients.Add(new WorkingIngredient
                    {
                        ThingDefName = defName,
                        Label = ing.filter.Summary,
                        Count = (int)ing.GetBaseCount()
                    });
                }
            }
        }

        private void SaveChanges()
        {
            if (_selected == null) return;
            var tw = RecipeTweakerMod.Settings.GetOrCreate(_selected.defName);
            string van = _selected.LabelCap.ToString();
            tw.newLabel = (!string.IsNullOrEmpty(_editLabel) && _editLabel != van) ? _editLabel : null;
            tw.newWorkAmount = float.TryParse(_editWork, out var wp) ? wp : _selected.workAmount;
            tw.disabled = _editDisabled;

            tw.hasCustomIngredients = true;
            tw.customIngredients.Clear();
            for (int i = 0; i < _workingIngredients.Count; i++)
            {
                tw.customIngredients.Add(new SavedIngredient
                {
                    thingDefName = _workingIngredients[i].ThingDefName,
                    filterLabel = _workingIngredients[i].Label,
                    count = _workingIngredients[i].Count
                });
            }

            var customData = CustomRecipeManager.GetCustomRecipe(_selected.defName);
            if (customData != null)
            {
                if (!string.IsNullOrEmpty(_editLabel)) customData.label = _editLabel;
                if (float.TryParse(_editWork, out var cwp)) customData.workAmount = cwp;
                customData.ingredients.Clear();
                for (int i = 0; i < _workingIngredients.Count; i++)
                {
                    customData.ingredients.Add(new SavedIngredient
                    {
                        thingDefName = _workingIngredients[i].ThingDefName,
                        filterLabel = _workingIngredients[i].Label,
                        count = _workingIngredients[i].Count
                    });
                }
                CustomRecipeManager.BuildAndRegister(customData, true);
            }

            RecipePatchApplier.ApplyTweak(_selected, tw);
            RecipeTweakerMod.Settings.PruneVanilla();
            RecipeTweakerMod.Settings.Write();
            Messages.Message("Tarif guncellendi: " + _selected.LabelCap, MessageTypeDefOf.PositiveEvent, false);
        }

        private void ResetToFactory()
        {
            if (_selected == null) return;
            if (CustomRecipeManager.IsCustomRecipe(_selected.defName))
            {
                Messages.Message("Ozel tariflerin orijinal hali yoktur. Silmek icin 'Tarifi Sil' butonunu kullanin.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            RecipePatchApplier.ResetToVanilla(_selected);
            RecipeTweakerMod.Settings.tweaks.RemoveAll(t => t.recipeDefName == _selected.defName);
            RecipeTweakerMod.Settings.Write();
            SelectRecipe(_selected);
            Messages.Message("Tarif orijinal haline donduruldu: " + _selected.LabelCap, MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
