using System.IO;
using UnityEngine;
using Verse;
using RimWorld;

namespace RecipeTweaker
{
    public class RecipeTweakerMod : Mod
    {
        public static RecipeTweakerSettings Settings { get; private set; }

        public RecipeTweakerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RecipeTweakerSettings>();
        }

        public override string SettingsCategory() => "RecipeTweaker_ModTitle".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("RecipeTweaker_SettingsTitle".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.Label("RecipeTweaker_SettingsDesc".Translate());
            listing.Gap(12f);

            int count = Settings?.tweaks?.Count ?? 0;
            string countText = "RecipeTweaker_ActiveCount".Translate(count);
            listing.Label(countText);
            listing.Gap(14f);

            // Primary action: Open recipe editor dialog
            Rect btnRow1 = listing.GetRect(38f);
            if (Widgets.ButtonText(btnRow1, "RecipeTweaker_OpenEditor".Translate(count)))
            {
                Find.WindowStack.Add(new Dialog_RecipeTweaker());
            }

            listing.Gap(10f);

            // Secondary action row: Export XML & Reset to Vanilla
            Rect btnRow2 = listing.GetRect(34f);
            float halfW = (btnRow2.width - 10f) / 2f;
            Rect exportBtn = new Rect(btnRow2.x, btnRow2.y, halfW, 34f);
            Rect resetBtn = new Rect(btnRow2.x + halfW + 10f, btnRow2.y, halfW, 34f);

            if (Widgets.ButtonText(exportBtn, "RecipeTweaker_ExportXml".Translate()))
            {
                XmlPatchExporter.ExportAll();
            }

            if (Widgets.ButtonText(resetBtn, "RecipeTweaker_ResetAll".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RecipeTweaker_ResetAllConfirm".Translate(),
                    () =>
                    {
                        if (Settings?.tweaks != null)
                        {
                            for (int i = 0; i < Settings.tweaks.Count; i++)
                            {
                                var r = DefDatabase<RecipeDef>.GetNamed(Settings.tweaks[i].recipeDefName, false);
                                if (r != null) RecipePatchApplier.ResetToVanilla(r);
                            }
                            Settings.tweaks.Clear();
                            Settings.Write();
                            Messages.Message("RecipeTweaker_ResetAllSuccess".Translate(), MessageTypeDefOf.PositiveEvent, false);
                        }
                    },
                    destructive: true
                ));
            }

            listing.Gap(20f);
            GUI.color = Color.gray;
            listing.Label("RecipeTweaker_HelpText".Translate());
            GUI.color = Color.white;

            listing.End();
        }
    }
}
