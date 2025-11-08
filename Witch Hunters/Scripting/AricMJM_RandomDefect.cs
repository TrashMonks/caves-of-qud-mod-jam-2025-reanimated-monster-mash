using System;
using System.Collections.Generic;
using XRL.Rules;

namespace XRL.World.Parts
{

    [Serializable]
    public class AricMJM_RandomDefect : IPart
    {
        public string PhysicalDefects;

        public string MentalDefects;

        public string StateKey;

        public override bool SameAs(IPart p)
        {
            return true;
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (!base.WantEvent(ID, cascade))
            {
                return ID == ObjectCreatedEvent.ID;
            }
            return true;
        }

        public override bool HandleEvent(ObjectCreatedEvent E)
        {
            Mutations mutations = ParentObject.RequirePart<Mutations>();
            Random random = Stat.Rnd;
            if (!StateKey.IsNullOrEmpty())
            {
                if (!The.Game.IntGameState.TryGetValue(StateKey, out var value))
                {
                    int num2 = (The.Game.IntGameState[StateKey] = random.Next());
                    value = num2;
                }
                random = new Random(value);
            }
            int num3 = random.Roll(MentalDefects);
            int num4 = random.Roll(PhysicalDefects);
            List<MutationEntry> mutationsOfCategory = MutationFactory.GetMutationsOfCategory("MentalDefects");
            int num5 = 0;
            int num6 = 0;
            while (num5 < num3 && num6 < 100)
            {
                MutationEntry randomElement = mutationsOfCategory.GetRandomElement(random);
                if (!mutations.HasMutation(randomElement.Class) && mutations.AddMutation(randomElement) >= 0)
                {
                    num5++;
                }
                num6++;
            }
            mutationsOfCategory = MutationFactory.GetMutationsOfCategory("PhysicalDefects");
            int num7 = 0;
            int num8 = 0;
            while (num7 < num4 && num8 < 100)
            {
                MutationEntry randomElement2 = mutationsOfCategory.GetRandomElement(random);
                if (!mutations.HasMutation(randomElement2.Class) && mutations.AddMutation(randomElement2) >= 0)
                {
                    num7++;
                }
                num8++;
            }
            ParentObject.RemovePart(this);
            return base.HandleEvent(E);
        }
    }
}