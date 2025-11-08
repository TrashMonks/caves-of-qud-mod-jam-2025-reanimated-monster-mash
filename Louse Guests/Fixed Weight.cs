using System;

namespace XRL.World.Parts
{
    [Serializable]
    public class HDBrownie_FixedWeight : IPart
    {
        public int Weight = 5;

        public override bool WantEvent(int id, int cascade)
        {
            return base.WantEvent(id, cascade)
                || id == GetIntrinsicWeightEvent.ID
            ;
        }

        public override bool HandleEvent(GetIntrinsicWeightEvent @event)
        {
            @event.Weight = Weight;
            return base.HandleEvent(@event);
        }
    }
}
