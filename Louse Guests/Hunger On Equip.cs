using System;

namespace XRL.World.Parts
{
    [Serializable]
    public class HDBrownie_HungerOnEquip : IPart
    {
        public int EveryNTurns = 10;
        public int Tick = 0;

        public override bool WantEvent(int id, int cascade)
        {
            return base.WantEvent(id, cascade)
                || id == BeginTakeActionEvent.ID;
            ;
        }

        public override bool HandleEvent(BeginTakeActionEvent @event)
        {
            if (@event.Object != ParentObject.Equipped) { goto Done; }
            Tick = (Tick + 1) % EveryNTurns;
            if (Tick == 0 && @event.Object.TryGetPart(out Stomach stomach))
            {
                stomach.CookingCounter += 1;
            }
            Done:
            return base.HandleEvent(@event);
        }
    }
}
