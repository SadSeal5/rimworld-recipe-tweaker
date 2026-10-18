using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RecipeTweaker
{
    public class Dialog_WorkbenchPicker : Window
    {
        public override Vector2 InitialSize => new Vector2(460f, 600f);

        private readonly Action<List<string>> _onConfirm;
        private readonly HashSet<string> _selected;
        private string _search = string.Empty;
        private Vector2 _scroll = Vector2.zero;
        private static List<ThingDef> _allWorkbenches;

        public Dialog_WorkbenchPicker(IEnumerable<string> initialSelected, Action<List<string>> onConfirm)
        {
            _onConfirm = onConfirm;
            _selected = initialSelected != null ? new HashSet<string>(initialSelected) : new HashSet<string>();
            doCloseX = true;
            draggable = true;
            closeOnClickedOutside = true;

            if (_allWorkbenches == null)
            {
                _allWorkbenches = new List<ThingDef>();
                var allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
                for (int i = 0; i < allDefs.Count; i++)
                {
                    var td = allDefs[i];
                    if (td.category == ThingCategory.Building && (
                        (td.recipes != null && td.recipes.Count > 0) ||
                        (td.AllRecipes != null && td.AllRecipes.Count > 0) ||
                        td.IsWorkTable))
                    {
                        _allWorkbenches.Add(td);
                    }
                }
                _allWorkbenches.Sort((a, b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "Tezgah Sec");
            Text.Font = GameFont.Small;

            _search = Widgets.TextField(new Rect(inRect.x, inRect.y + 34f, inRect.width, 28f), _search);

            float actionY = inRect.y + 66f;
            if (Widgets.ButtonText(new Rect(inRect.x, actionY, 110f, 26f), "Tumunu Sec"))
            {
                for (int i = 0; i < _allWorkbenches.Count; i++)
                {
                    if (MatchesSearch(_allWorkbenches[i])) _selected.Add(_allWorkbenches[i].defName);
                }
            }
            if (Widgets.ButtonText(new Rect(inRect.x + 116f, actionY, 120f, 26f), "Tumunu Temizle"))
            {
                _selected.Clear();
            }

            float listY = actionY + 32f;
            float listH = inRect.height - (listY - inRect.y) - 40f;
            var outRect = new Rect(inRect.x, listY, inRect.width, listH);

            var matches = new List<ThingDef>();
            for (int i = 0; i < _allWorkbenches.Count; i++)
            {
                if (MatchesSearch(_allWorkbenches[i])) matches.Add(_allWorkbenches[i]);
            }

            var viewRect = new Rect(0f, 0f, inRect.width - 16f, matches.Count * 30f);
            Widgets.BeginScrollView(outRect, ref _scroll, viewRect);

            for (int i = 0; i < matches.Count; i++)
            {
                var td = matches[i];
                var rowR = new Rect(0f, i * 30f, viewRect.width, 28f);
                if (i % 2 == 1) Widgets.DrawBoxSolid(rowR, new Color(0.1f, 0.1f, 0.1f, 0.35f));
                if (Mouse.IsOver(rowR)) Widgets.DrawHighlight(rowR);

                bool isSel = _selected.Contains(td.defName);
                bool newSel = isSel;
                Widgets.Checkbox(rowR.x + 4f, rowR.y + 2f, ref newSel);
                if (newSel != isSel)
                {
                    if (newSel) _selected.Add(td.defName);
                    else _selected.Remove(td.defName);
                }

                if (td.uiIcon != null)
                {
                    GUI.DrawTexture(new Rect(rowR.x + 32f, rowR.y + 4f, 20f, 20f), td.uiIcon);
                }

                var labelR = new Rect(rowR.x + 58f, rowR.y + 2f, rowR.width - 62f, 24f);
                Widgets.Label(labelR, td.LabelCap);

                if (Widgets.ButtonInvisible(labelR))
                {
                    if (_selected.Contains(td.defName)) _selected.Remove(td.defName);
                    else _selected.Add(td.defName);
                }
            }

            Widgets.EndScrollView();

            float btnY = inRect.y + inRect.height - 34f;
            if (Widgets.ButtonText(new Rect(inRect.x, btnY, 130f, 32f), "Tamam (" + _selected.Count + ")"))
            {
                _onConfirm?.Invoke(new List<string>(_selected));
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.x + 140f, btnY, 110f, 32f), "Iptal"))
            {
                Close();
            }
        }

        private bool MatchesSearch(ThingDef td)
        {
            if (string.IsNullOrEmpty(_search)) return true;
            return td.LabelCap.ToString().IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   td.defName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
