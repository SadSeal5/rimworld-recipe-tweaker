using System.IO;
using System.Text;
using Verse;
using RimWorld;

namespace RecipeTweaker {
    public static class XmlPatchExporter {
        private static string Dir => Path.Combine(GenFilePaths.ConfigFolderPath, "RecipeTweaker");
        private static string FilePath => Path.Combine(Dir, "GeneratedPatches.xml");

        public static void ExportAll() {
            var tweaks = RecipeTweakerMod.Settings?.tweaks;
            if (tweaks == null || tweaks.Count == 0) {
                Messages.Message("[RecipeTweaker] Disari aktarilacak degisiklik yok.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Directory.CreateDirectory(Dir);
            var sb = new StringBuilder(1024);
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Patch>");

            for (int i = 0; i < tweaks.Count; i++) {
                var tw = tweaks[i];
                var r = DefDatabase<RecipeDef>.GetNamed(tw.recipeDefName, false);
                if (r == null) continue;
                string xpath = "Defs/RecipeDef[defName=\"" + tw.recipeDefName + "\"]";

                if (!string.IsNullOrEmpty(tw.newLabel))
                    Repl(sb, xpath + "/label", "<label>" + tw.newLabel + "</label>");
                if (tw.newWorkAmount >= 0f)
                    Repl(sb, xpath + "/workAmount", "<workAmount>" + tw.newWorkAmount.ToString("F0") + "</workAmount>");

                if (tw.hasCustomIngredients && tw.customIngredients != null) {
                    var ingXml = new StringBuilder();
                    ingXml.AppendLine("<ingredients>");
                    for (int j = 0; j < tw.customIngredients.Count; j++) {
                        var item = tw.customIngredients[j];
                        ingXml.AppendLine("      <li>");
                        ingXml.AppendLine("        <filter><thingDefs><li>" + item.thingDefName + "</li></thingDefs></filter>");
                        ingXml.AppendLine("        <count>" + item.count + "</count>");
                        ingXml.AppendLine("      </li>");
                    }
                    ingXml.Append("    </ingredients>");
                    Repl(sb, xpath + "/ingredients", ingXml.ToString());
                }
            }
            sb.AppendLine("</Patch>");
            File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            Messages.Message("[RecipeTweaker] XML aktarildi: " + FilePath, MessageTypeDefOf.PositiveEvent, false);
        }

        private static void Repl(StringBuilder sb, string xpath, string val) {
            sb.AppendLine("  <Operation Class=\"PatchOperationReplace\">\n    <xpath>" + xpath + "</xpath>\n    <value>" + val + "</value>\n  </Operation>");
        }
    }
}