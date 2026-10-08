using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RecipeTweaker {
    public class WorkingIngredient {
        public string ThingDefName;
        public string Label;
        public int Count;
    }

    public class Dialog_RecipeTweaker : Window {
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

        public Dialog_RecipeTweaker() { doCloseX = true; resizeable = true; draggable = true; RecipeCache.EnsureBuilt(); RefreshFilter(); }

        public override void DoWindowContents(Rect inRect) {
            if (_searchText != _prevSearch) { RefreshFilter(); _prevSearch = _searchText; }
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, 400f, 32f), "Recipe Tweaker (1.6)");
            Text.Font = GameFont.Small;

            float cy = inRect.y + 36f, ch = inRect.height - 36f;
            var leftR = new Rect(inRect.x, cy, LEFT_W, ch);
            var divX = inRect.x + LEFT_W + GAP;
            var rightR = new Rect(divX + 1f + GAP, cy, inRect.width - LEFT_W - GAP * 2f - 1f, ch);

            Widgets.DrawLineVertical(divX, cy, ch);
            DrawLeft(leftR); DrawRight(rightR);
        }

        private void DrawLeft(Rect rect) {
            _searchText = Widgets.TextField(new Rect(rect.x, rect.y, rect.width, SEARCH_H), _searchText);
            var outer = new Rect(rect.x, rect.y + SEARCH_H + GAP, rect.width, rect.height - SEARCH_H - GAP);
            var view = new Rect(0f, 0f, outer.width - 16f, _totalListH);
            Widgets.BeginScrollView(outer, ref _listScroll, view);
            float y = 0f, visTop = _listScroll.y, visBot = _listScroll.y + outer.height;

            if (_currentFiltered != null) {
                for (int g = 0; g < _currentFiltered.Count; g++) {
                    var entry = _currentFiltered[g];
                    if (y + HDR_H > visTop && y < visBot) {
                        Widgets.DrawBoxSolid(new Rect(0f, y, view.width, HDR_H), ColHeader);
                        GUI.color = ColGray; Text.Font = GameFont.Tiny;
                        Widgets.Label(new Rect(5f, y + 4f, view.width - 5f, HDR_H), entry.ModName.ToUpperInvariant());
                        Text.Font = GameFont.Small; GUI.color = Color.white;
                    }
                    y += HDR_H;

                    for (int i = 0; i < entry.Recipes.Count; i++) {
                        if (y + ROW_H > visTop && y < visBot) {
                            var row = new Rect(0f, y, view.width, ROW_H);
                            if (entry.Recipes[i] == _selected) Widgets.DrawBoxSolid(row, ColSelected);
                            else if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);
                            Widgets.Label(new Rect(10f, y + 3f, view.width - 10f, ROW_H), entry.Recipes[i].LabelCap);
                            if (Widgets.ButtonInvisible(row)) SelectRecipe(entry.Recipes[i]);
                        }
                        y += ROW_H;
                    }
                }
            }
            Widgets.EndScrollView();
        }

        private void DrawRight(Rect rect) {
            if (_selected == null) { GUI.color = ColGray; Widgets.Label(rect, "<- Bir tarif secin."); GUI.color = Color.white; return; }
            float x = rect.x, y = rect.y, w = rect.width;
            Text.Font = GameFont.Medium; Widgets.Label(new Rect(x, y, w, 30f), _selected.LabelCap);
            Text.Font = GameFont.Tiny; GUI.color = ColGray; Widgets.Label(new Rect(x, y + 26f, w, 18f), _selected.modContentPack?.Name ?? "Unknown");
            GUI.color = Color.white; Text.Font = GameFont.Small;
            y += 52f; Widgets.DrawLineHorizontal(x, y, w); y += GAP * 2f;

            Widgets.Label(new Rect(x, y, LBL_W, 24f), "Etiket:");
            _editLabel = Widgets.TextField(new Rect(x + LBL_W, y, w - LBL_W, 24f), _editLabel);
            y += 30f;

            // Sure Alani: Crafting + Ameliyat Icin Genisletilmis Slider
            Widgets.Label(new Rect(x, y, LBL_W, 24f), "Sure (tick):");
            float wa = float.TryParse(_editWork, out var p1) ? p1 : 0f;
            float sliderW = w - LBL_W - COUNT_W - GAP - 70f;
            wa = (float)Math.Round(Widgets.HorizontalSlider(new Rect(x + LBL_W, y + 6f, sliderW, 16f), wa, 1f, 100000f, true), 0);
            string waStr = Widgets.TextField(new Rect(x + LBL_W + sliderW + GAP, y, COUNT_W + 20f, 24f), wa.ToString("F0"));
            if (float.TryParse(waStr, out var p2)) wa = p2;
            _editWork = wa.ToString("F0");
            Widgets.Label(new Rect(x + w - 46f, y + 2f, 46f, 24f), "tick");
            y += 34f; Widgets.DrawLineHorizontal(x, y, w); y += GAP;

            // Malzemeler
            GUI.color = ColGray; Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(x, y, 120f, 20f), "MALZEMELER");
            GUI.color = Color.white; Text.Font = GameFont.Small;
            y += 22f;

            int toRemove = -1;
            for (int i = 0; i < _workingIngredients.Count; i++) {
                var item = _workingIngredients[i];
                var row = new Rect(x, y, w, ROW_H);
                if (i % 2 == 0) Widgets.DrawBoxSolid(row, ColRowAlt);

                float controlsW = SPINNER_W * 2f + COUNT_W + 28f + GAP * 3f;
                Widgets.Label(new Rect(x + 4f, y + 2f, w - controlsW - 8f, ROW_H), item.Label);

                float curX = x + w - controlsW;
                if (Widgets.ButtonText(new Rect(curX, y, SPINNER_W, ROW_H), "-"))
                    item.Count = Math.Max(1, item.Count - 1);
                curX += SPINNER_W + GAP;

                string cs = Widgets.TextField(new Rect(curX, y, COUNT_W, ROW_H), item.Count.ToString());
                if (int.TryParse(cs, out var cp) && cp > 0) item.Count = cp;
                curX += COUNT_W + GAP;

                if (Widgets.ButtonText(new Rect(curX, y, SPINNER_W, ROW_H), "+"))
                    item.Count++;
                curX += SPINNER_W + GAP;

                GUI.color = new Color(1f, 0.3f, 0.3f);
                if (Widgets.ButtonText(new Rect(curX, y, 26f, ROW_H), "X"))
                    toRemove = i;
                GUI.color = Color.white;

                y += ROW_H;
            }
            if (toRemove >= 0) _workingIngredients.RemoveAt(toRemove);

            y += GAP;
            if (Widgets.ButtonText(new Rect(x, y, 160f, 26f), "+ Malzeme Ekle")) {
                Find.WindowStack.Add(new Dialog_IngredientPicker(selectedThing => {
                    _workingIngredients.Add(new WorkingIngredient {
                        ThingDefName = selectedThing.defName,
                        Label = selectedThing.LabelCap.ToString(),
                        Count = 1
                    });
                }));
            }
            y += 32f;

            Widgets.DrawLineHorizontal(x, y, w); y += GAP;
            bool dis = _editDisabled;
            Widgets.CheckboxLabeled(new Rect(x, y, w, 24f), "Bu tarifi devre disi birak", ref dis);
            _editDisabled = dis;
            y += 34f;

            // Alt Butonlar: Kaydet | Fabrika Ayarlari | XML Export
            float bw = (w - GAP * 2f) / 3f;
            
            // 1. Kaydet
            if (Widgets.ButtonText(new Rect(x, y, bw, BTN_H), "Kaydet")) 
                SaveChanges();

            // 2. Fabrika Ayarlarina Sifirla
            GUI.color = new Color(1f, 0.8f, 0.4f);
            if (Widgets.ButtonText(new Rect(x + bw + GAP, y, bw, BTN_H), "Fabrika Ayarlari")) {
                ResetToFactory();
            }
            GUI.color = Color.white;

            // 3. XML Export
            if (Widgets.ButtonText(new Rect(x + (bw + GAP) * 2f, y, bw, BTN_H), "XML Export")) 
                XmlPatchExporter.ExportAll();
        }

        private void RefreshFilter() {
            _currentFiltered = RecipeCache.GetFiltered(_searchText);
            _totalListH = 0f;
            if (_currentFiltered == null) return;
            for (int i = 0; i < _currentFiltered.Count; i++) _totalListH += HDR_H + _currentFiltered[i].Recipes.Count * ROW_H;
        }

        private void SelectRecipe(RecipeDef r) {
            _selected = r;
            RecipePatchApplier.EnsureVanillaSaved(r);

            var tw = RecipeTweakerMod.Settings.GetOrCreate(r.defName);
            _editLabel = tw.newLabel ?? r.LabelCap.ToString();

            // Crafting esyalarinda sure WorkToMake statindan okunur
            float currentWork = r.workAmount;
            if (currentWork < 0f && r.products != null && r.products.Count > 0 && r.products[0].thingDef != null) {
                currentWork = r.products[0].thingDef.GetStatValueAbstract(StatDefOf.WorkToMake);
            }
            if (currentWork < 0f) currentWork = 450f;

            _editWork = (tw.newWorkAmount >= 0f ? tw.newWorkAmount : currentWork).ToString("F0");
            _editDisabled = tw.disabled;

            _workingIngredients.Clear();
            if (tw.hasCustomIngredients && tw.customIngredients != null) {
                for (int i = 0; i < tw.customIngredients.Count; i++) {
                    var s = tw.customIngredients[i];
                    _workingIngredients.Add(new WorkingIngredient {
                        ThingDefName = s.thingDefName,
                        Label = s.filterLabel,
                        Count = s.count
                    });
                }
            } else if (r.ingredients != null) {
                for (int i = 0; i < r.ingredients.Count; i++) {
                    var ing = r.ingredients[i];
                    string defName = ing.filter.AnyAllowedDef?.defName ?? "Steel";
                    _workingIngredients.Add(new WorkingIngredient {
                        ThingDefName = defName,
                        Label = ing.filter.Summary,
                        Count = (int)ing.GetBaseCount()
                    });
                }
            }
        }

        private void SaveChanges() {
            if (_selected == null) return;
            var tw = RecipeTweakerMod.Settings.GetOrCreate(_selected.defName);
            string van = _selected.LabelCap.ToString();
            tw.newLabel = (!string.IsNullOrEmpty(_editLabel) && _editLabel != van) ? _editLabel : null;
            tw.newWorkAmount = float.TryParse(_editWork, out var wp) ? wp : _selected.workAmount;
            tw.disabled = _editDisabled;

            tw.hasCustomIngredients = true;
            tw.customIngredients.Clear();
            for (int i = 0; i < _workingIngredients.Count; i++) {
                tw.customIngredients.Add(new SavedIngredient {
                    thingDefName = _workingIngredients[i].ThingDefName,
                    filterLabel = _workingIngredients[i].Label,
                    count = _workingIngredients[i].Count
                });
            }

            RecipePatchApplier.ApplyTweak(_selected, tw);
            RecipeTweakerMod.Settings.PruneVanilla();
            RecipeTweakerMod.Settings.Write();
            Messages.Message("Tarif guncellendi: " + _selected.LabelCap, MessageTypeDefOf.PositiveEvent, false);
        }

        private void ResetToFactory() {
            if (_selected == null) return;
            RecipePatchApplier.ResetToVanilla(_selected);
            RecipeTweakerMod.Settings.tweaks.RemoveAll(t => t.recipeDefName == _selected.defName);
            RecipeTweakerMod.Settings.Write();
            SelectRecipe(_selected);
            Messages.Message("Tarif orijinal haline donduruldu: " + _selected.LabelCap, MessageTypeDefOf.PositiveEvent, false);
        }
    }
}