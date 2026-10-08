using System.IO;
using UnityEngine;
using Verse;
namespace RecipeTweaker {
    public class RecipeTweakerMod : Mod {
        public static RecipeTweakerSettings Settings { get; private set; }
        public RecipeTweakerMod(ModContentPack content) : base(content) { Settings = GetSettings<RecipeTweakerSettings>(); }
        public override string SettingsCategory() => "Recipe Tweaker";
        public override void DoSettingsWindowContents(Rect inRect) {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Gap(8f);
            listing.Label("Yuklu tum modlarin tarif tanimlarini (RecipeDef) buradan duzenle.");
            listing.Gap(16f);
            if (listing.ButtonText("Tarif Editorunu Ac (" + (Settings.tweaks?.Count ?? 0) + " tweak)"))
                Find.WindowStack.Add(new Dialog_RecipeTweaker());
            listing.End();
        }
    }
}