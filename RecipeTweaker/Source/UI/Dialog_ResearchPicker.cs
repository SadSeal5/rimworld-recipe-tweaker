using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RecipeTweaker
{
    public class Dialog_ResearchPicker : Window
    {
        public override Vector2 InitialSize => new Vector2(460f, 600f);

        private readonly Action<List<string>> _onConfirm;
        private readonly HashSet<string> _selected;
        private string _search = string.Empty;
        private Vector2 _scroll = Vector2.zero;
        private static List<ResearchProjectDef> _allProjects;

        public Dialog_ResearchPicker(IEnumerable<string> initialSelected, Action<List<string>> onConfirm)
        {
            _onConfirm = onConfirm;
            _selected = initialSelected != null ? new HashSet<string>(initialSelected) : new HashSet<string>();
            doCloseX = true;
            draggable = true;
            closeOnClickedOutside = true;

            if (_allProjects == null)
            {
                _allProjects = new List<ResearchProjectDef>(DefDatabase<ResearchProjectDef>.AllDefsListForReading);
                _allProjects.Sort((a, b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "Arastirma Projesi Sec");
            Text.Font = GameFont.Small;

            _search = Widgets.TextField(new Rect(inRect.x, inRect.y + 34f, inRect.width, 28f), _search);

            float actionY = inRect.y + 66f;
            if (Widgets.ButtonText(new Rect(inRect.x, actionY, 110f, 26f), "Tumunu Sec"))
            {
                for (int i = 0; i < _allProjects.Count; i++)
                {
                    if (MatchesSearch(_allProjects[i])) _selected.Add(_allProjects[i].defName);
                }
            }
            if (Widgets.ButtonText(new Rect(inRect.x + 116f, actionY, 120f, 26f), "Tumunu Temizle"))
            {
                _selected.Clear();
            }

            float listY = actionY + 32f;
            float listH = inRect.height - (listY - inRect.y) - 40f;
            var outRect = new Rect(inRect.x, listY, inRect.width, listH);

            var matches = new List<ResearchProjectDef>();
            for (int i = 0; i < _allProjects.Count; i++)
            {
                if (MatchesSearch(_allProjects[i])) matches.Add(_allProjects[i]);
            }

            var viewRect = new Rect(0f, 0f, inRect.width - 16f, matches.Count * 28f);
            Widgets.BeginScrollView(outRect, ref _scroll, viewRect);

            for (int i = 0; i < matches.Count; i++)
            {
                var rp = matches[i];
                var rowR = new Rect(0f, i * 28f, viewRect.width, 26f);
                if (i % 2 == 1) Widgets.DrawBoxSolid(rowR, new Color(0.1f, 0.1f, 0.1f, 0.35f));
                if (Mouse.IsOver(rowR)) Widgets.DrawHighlight(rowR);

                bool isSel = _selected.Contains(rp.defName);
                bool newSel = isSel;
                Widgets.Checkbox(rowR.x + 4f, rowR.y + 2f, ref newSel);
                if (newSel != isSel)
                {
                    if (newSel) _selected.Add(rp.defName);
                    else _selected.Remove(rp.defName);
                }

                var labelR = new Rect(rowR.x + 32f, rowR.y + 2f, rowR.width - 36f, 24f);
                string text = rp.LabelCap;
                if (rp.techLevel != TechLevel.Undefined)
                {
                    text += " (" + rp.techLevel.ToString() + ")";
                }
                Widgets.Label(labelR, text);

                if (Widgets.ButtonInvisible(labelR))
                {
                    if (_selected.Contains(rp.defName)) _selected.Remove(rp.defName);
                    else _selected.Add(rp.defName);
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

        private bool MatchesSearch(ResearchProjectDef rp)
        {
            if (string.IsNullOrEmpty(_search)) return true;
            return rp.LabelCap.ToString().IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   rp.defName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
