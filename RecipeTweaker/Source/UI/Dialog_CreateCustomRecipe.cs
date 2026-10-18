using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RecipeTweaker
{
    public class Dialog_CreateCustomRecipe : Window
    {
        public override Vector2 InitialSize => new Vector2(640f, 680f);

        private readonly Action _onCreated;
        private ThingDef _selectedProduct;
        private string _label = string.Empty;
        private int _productCount = 1;
        private string _workAmount = "450";
        private int _minSkill = 0;
        private List<string> _workbenchDefs = new List<string>();
        private List<string> _researchDefs = new List<string>();
        private List<WorkingIngredient> _ingredients = new List<WorkingIngredient>();
        private Vector2 _ingScroll = Vector2.zero;

        public Dialog_CreateCustomRecipe(Action onCreated = null)
        {
            _onCreated = onCreated;
            doCloseX = true;
            draggable = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "Yeni Ozel Tarif Ekle");
            Text.Font = GameFont.Small;

            float y = inRect.y + 36f;
            float w = inRect.width;

            // 1. Product Selection
            Widgets.Label(new Rect(inRect.x, y + 2f, 130f, 26f), "Uretilecek Urun:");
            string prodText = _selectedProduct != null ? _selectedProduct.LabelCap.ToString() : "Urun Sec...";
            var prodBtnR = new Rect(inRect.x + 130f, y, w - 130f, 28f);
            if (Widgets.ButtonText(prodBtnR, prodText))
            {
                Find.WindowStack.Add(new Dialog_IngredientPicker(td =>
                {
                    _selectedProduct = td;
                    if (string.IsNullOrEmpty(_label))
                    {
                        _label = "Uret: " + td.LabelCap;
                    }
                }));
            }
            if (_selectedProduct?.uiIcon != null)
            {
                GUI.DrawTexture(new Rect(prodBtnR.x + 4f, prodBtnR.y + 4f, 20f, 20f), _selectedProduct.uiIcon);
            }
            y += 34f;

            // 2. Label
            Widgets.Label(new Rect(inRect.x, y + 2f, 130f, 24f), "Tarif Adi:");
            _label = Widgets.TextField(new Rect(inRect.x + 130f, y, w - 130f, 24f), _label);
            y += 30f;

            // 3. Product Count & Work Amount & Min Skill row
            Widgets.Label(new Rect(inRect.x, y + 2f, 90f, 24f), "Uretim Adedi:");
            if (Widgets.ButtonText(new Rect(inRect.x + 90f, y, 26f, 24f), "-"))
            {
                if (_productCount > 1) _productCount--;
            }
            string pCntStr = Widgets.TextField(new Rect(inRect.x + 118f, y, 40f, 24f), _productCount.ToString());
            if (int.TryParse(pCntStr, out int pc)) _productCount = Mathf.Max(1, pc);
            if (Widgets.ButtonText(new Rect(inRect.x + 160f, y, 26f, 24f), "+"))
            {
                _productCount++;
            }

            Widgets.Label(new Rect(inRect.x + 205f, y + 2f, 85f, 24f), "Is Suresi:");
            _workAmount = Widgets.TextField(new Rect(inRect.x + 290f, y, 65f, 24f), _workAmount);

            Widgets.Label(new Rect(inRect.x + 375f, y + 2f, 110f, 24f), "Min El Becerisi:");
            if (Widgets.ButtonText(new Rect(inRect.x + 485f, y, 26f, 24f), "-"))
            {
                if (_minSkill > 0) _minSkill--;
            }
            string skStr = Widgets.TextField(new Rect(inRect.x + 513f, y, 35f, 24f), _minSkill.ToString());
            if (int.TryParse(skStr, out int sk)) _minSkill = Mathf.Clamp(sk, 0, 20);
            if (Widgets.ButtonText(new Rect(inRect.x + 550f, y, 26f, 24f), "+"))
            {
                if (_minSkill < 20) _minSkill++;
            }
            y += 32f;

            // 4. Workbenches selection
            string wbBtnText = "Tezgahlar (" + _workbenchDefs.Count + " secili)";
            if (Widgets.ButtonText(new Rect(inRect.x, y, 220f, 28f), wbBtnText))
            {
                Find.WindowStack.Add(new Dialog_WorkbenchPicker(_workbenchDefs, list => _workbenchDefs = list));
            }
            string wbSummary = GetWorkbenchSummary();
            GUI.color = Color.gray;
            Widgets.Label(new Rect(inRect.x + 226f, y + 4f, w - 226f, 24f), wbSummary);
            GUI.color = Color.white;
            y += 32f;

            // 5. Research Prerequisites
            string resBtnText = "Gereken Arastirmalar (" + _researchDefs.Count + " secili)";
            if (Widgets.ButtonText(new Rect(inRect.x, y, 220f, 28f), resBtnText))
            {
                Find.WindowStack.Add(new Dialog_ResearchPicker(_researchDefs, list => _researchDefs = list));
            }
            string resSummary = GetResearchSummary();
            GUI.color = Color.gray;
            Widgets.Label(new Rect(inRect.x + 226f, y + 4f, w - 226f, 24f), resSummary);
            GUI.color = Color.white;
            y += 36f;

            // 6. Ingredients
            Widgets.DrawLineHorizontal(inRect.x, y, w);
            y += 6f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, 160f, 26f), "Malzemeler");
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(new Rect(inRect.x + w - 140f, y, 140f, 26f), "+ Malzeme Ekle"))
            {
                Find.WindowStack.Add(new Dialog_IngredientPicker(td =>
                {
                    _ingredients.Add(new WorkingIngredient
                    {
                        ThingDefName = td.defName,
                        Label = td.LabelCap,
                        Count = 1
                    });
                }));
            }
            y += 30f;

            float listH = inRect.height - (y - inRect.y) - 44f;
            var listOutR = new Rect(inRect.x, y, w, listH);
            Widgets.DrawMenuSection(listOutR);

            var viewRect = new Rect(0f, 0f, w - 16f, _ingredients.Count * 30f);
            Widgets.BeginScrollView(listOutR, ref _ingScroll, viewRect);

            for (int i = 0; i < _ingredients.Count; i++)
            {
                var ing = _ingredients[i];
                var rowR = new Rect(0f, i * 30f, viewRect.width, 28f);
                if (i % 2 == 1) Widgets.DrawBoxSolid(rowR, new Color(0.1f, 0.1f, 0.1f, 0.35f));

                Widgets.Label(new Rect(rowR.x + 6f, rowR.y + 4f, rowR.width - 240f, 22f), ing.Label);

                if (Widgets.ButtonText(new Rect(rowR.x + rowR.width - 200f, rowR.y + 2f, 26f, 24f), "-"))
                {
                    if (ing.Count > 1) ing.Count--;
                }

                string cntStr = Widgets.TextField(new Rect(rowR.x + rowR.width - 170f, rowR.y + 2f, 44f, 24f), ing.Count.ToString());
                if (int.TryParse(cntStr, out int nc)) ing.Count = Mathf.Max(1, nc);

                if (Widgets.ButtonText(new Rect(rowR.x + rowR.width - 122f, rowR.y + 2f, 26f, 24f), "+"))
                {
                    ing.Count++;
                }

                if (Widgets.ButtonText(new Rect(rowR.x + rowR.width - 65f, rowR.y + 2f, 60f, 24f), "Sil"))
                {
                    _ingredients.RemoveAt(i);
                    i--;
                }
            }

            Widgets.EndScrollView();

            // Bottom buttons
            float btnY = inRect.y + inRect.height - 34f;
            if (Widgets.ButtonText(new Rect(inRect.x, btnY, 150f, 32f), "Tarifi Olustur"))
            {
                TryCreateRecipe();
            }
            if (Widgets.ButtonText(new Rect(inRect.x + 160f, btnY, 110f, 32f), "Iptal"))
            {
                Close();
            }
        }

        private string GetWorkbenchSummary()
        {
            if (_workbenchDefs.Count == 0) return "Hicbiri secilmedi";
            var names = new List<string>();
            for (int i = 0; i < _workbenchDefs.Count; i++)
            {
                var td = DefDatabase<ThingDef>.GetNamed(_workbenchDefs[i], false);
                names.Add(td != null ? td.LabelCap.ToString() : _workbenchDefs[i]);
            }
            string full = string.Join(", ", names);
            if (full.Length > 45) return full.Substring(0, 42) + "...";
            return full;
        }

        private string GetResearchSummary()
        {
            if (_researchDefs.Count == 0) return "Yok (Hemen acik)";
            var names = new List<string>();
            for (int i = 0; i < _researchDefs.Count; i++)
            {
                var rp = DefDatabase<ResearchProjectDef>.GetNamed(_researchDefs[i], false);
                names.Add(rp != null ? rp.LabelCap.ToString() : _researchDefs[i]);
            }
            string full = string.Join(", ", names);
            if (full.Length > 45) return full.Substring(0, 42) + "...";
            return full;
        }

        private void TryCreateRecipe()
        {
            if (_selectedProduct == null)
            {
                Messages.Message("Lutfen uretilecek bir urun secin.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (string.IsNullOrWhiteSpace(_label))
            {
                Messages.Message("Lutfen tarif icin bir isim girin.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!float.TryParse(_workAmount, out float work) || work <= 0f)
            {
                Messages.Message("Lutfen gecerli bir is suresi girin (pozitif sayi).", MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (_workbenchDefs.Count == 0)
            {
                Messages.Message("Lutfen en az bir calisma tezgahi secin.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (_ingredients.Count == 0)
            {
                Messages.Message("Lutfen en az bir malzeme ekleyin.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            string defName;
            do
            {
                defName = "CustomRecipe_" + _selectedProduct.defName + "_" + Rand.Range(1000, 9999);
            }
            while (DefDatabase<RecipeDef>.GetNamed(defName, false) != null);

            var data = new CustomRecipeData
            {
                defName = defName,
                label = _label.Trim(),
                productDefName = _selectedProduct.defName,
                productCount = Mathf.Max(1, _productCount),
                workAmount = work,
                minSkillLevel = Mathf.Clamp(_minSkill, 0, 20),
                workbenchDefNames = new List<string>(_workbenchDefs),
                researchPrerequisiteDefNames = new List<string>(_researchDefs),
                ingredients = new List<SavedIngredient>()
            };

            for (int i = 0; i < _ingredients.Count; i++)
            {
                data.ingredients.Add(new SavedIngredient
                {
                    thingDefName = _ingredients[i].ThingDefName,
                    filterLabel = _ingredients[i].Label,
                    count = _ingredients[i].Count
                });
            }

            CustomRecipeManager.BuildAndRegister(data, true);
            Messages.Message("Yeni tarif olusturuldu: " + data.label, MessageTypeDefOf.PositiveEvent, false);
            _onCreated?.Invoke();
            Close();
        }
    }
}
