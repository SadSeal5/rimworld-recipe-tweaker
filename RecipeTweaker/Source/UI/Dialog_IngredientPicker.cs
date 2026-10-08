using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RecipeTweaker {
    public class Dialog_IngredientPicker : Window {
        public override Vector2 InitialSize => new Vector2(420f, 560f);

        private Action<ThingDef> _onSelected;
        private string _search = string.Empty;
        private Vector2 _scroll = Vector2.zero;
        private static List<ThingDef> _allItems;

        public Dialog_IngredientPicker(Action<ThingDef> onSelected) {
            _onSelected = onSelected;
            doCloseX = true;
            draggable = true;
            closeOnClickedOutside = true;

            if (_allItems == null) {
                _allItems = new List<ThingDef>();
                foreach (var td in DefDatabase<ThingDef>.AllDefsListForReading) {
                    if (td.category == ThingCategory.Item && !td.isUnfinishedThing)
                        _allItems.Add(td);
                }
                _allItems.Sort((a, b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase));
            }
        }

        public override void DoWindowContents(Rect inRect) {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "Malzeme Sec");
            Text.Font = GameFont.Small;

            _search = Widgets.TextField(new Rect(inRect.x, inRect.y + 36f, inRect.width, 28f), _search);

            float listY = inRect.y + 70f;
            float listH = inRect.height - 70f;
            var outRect = new Rect(inRect.x, listY, inRect.width, listH);

            var matches = new List<ThingDef>();
            for (int i = 0; i < _allItems.Count; i++) {
                if (string.IsNullOrEmpty(_search) || _allItems[i].LabelCap.ToString().IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                    matches.Add(_allItems[i]);
            }

            var viewRect = new Rect(0f, 0f, inRect.width - 16f, matches.Count * 28f);
            Widgets.BeginScrollView(outRect, ref _scroll, viewRect);

            for (int i = 0; i < matches.Count; i++) {
                var r = new Rect(0f, i * 28f, viewRect.width, 26f);
                if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                Widgets.Label(new Rect(8f, i * 28f + 2f, viewRect.width - 16f, 26f), matches[i].LabelCap);
                if (Widgets.ButtonInvisible(r)) {
                    _onSelected?.Invoke(matches[i]);
                    Close();
                }
            }

            Widgets.EndScrollView();
        }
    }
}