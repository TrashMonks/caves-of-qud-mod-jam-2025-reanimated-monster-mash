namespace XRL.World.Parts
{
    public class UD_FleshGolems_DifficultyEvaluationAdjustment : IScribedPart
    {
        public int AdjustRating;
        public int MinRating;
        public int MaxRating;

        private bool WantAny
            => AdjustRating != 0
            || MinRating > int.MinValue
            || MaxRating < int.MaxValue;

        public UD_FleshGolems_DifficultyEvaluationAdjustment()
        {
            AdjustRating = 0;
            MinRating = int.MinValue;
            MaxRating = int.MaxValue;
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || (ID == GetDifficultyEvaluationEvent.ID && WantAny);

        public override bool HandleEvent(GetDifficultyEvaluationEvent E)
        {
            if (AdjustRating != 0)
                E.Rating += AdjustRating;

            if (MinRating > int.MinValue)
                E.MinimumRating(MinRating);

            if (MaxRating < int.MaxValue)
                E.MaximumRating(MaxRating);

            return base.HandleEvent(E);
        }
    }
}
