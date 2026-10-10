using Verse;

namespace RecipeTweaker
{
    public class IngredientTweak : IExposable
    {
        public int ingredientIndex;
        public int newCount;

        public void ExposeData()
        {
            Scribe_Values.Look(ref ingredientIndex, "ingredientIndex", 0);
            Scribe_Values.Look(ref newCount, "newCount", 1);
        }
    }
}
