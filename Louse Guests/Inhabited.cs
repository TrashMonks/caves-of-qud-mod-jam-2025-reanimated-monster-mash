using System;
using XRL;
using XRL.World;
using XRL.World.Parts;
using HDBrownie.Effects;

namespace HDBrownie.Effects
{
    [Serializable]
    public class Inhabited : Effect
    {
        public GameObject Inhabitor;
        public bool didReduceVolume = false;

        public Inhabited() { } // needed for deserialization

        public Inhabited(GameObject by)
        {
            Duration = DURATION_INDEFINITE;
            DisplayName = "{{W|inhabited}}";
            Inhabitor = by;
        }

        public override bool Apply(GameObject @object)
        {
            return IsInhabitable(@object) && base.Apply(@object);
        }

        public override void Applied(GameObject @object)
        {
            Inhabitor.Brain.Goals.Clear();
            Inhabitor.MakeInactive();
            Inhabitor.CurrentCell.RemoveObject(Inhabitor, Silent: true);

            if (@object.TryGetPart(out LiquidVolume liquid))
            {
                if (liquid.Volume >= liquid.MaxVolume)
                {
                    liquid.Pour(TargetCell: @object.GetCurrentCell(), PourAmount: 1);
                }
                liquid.MaxVolume -= 1;
            }

            @object.RequirePart<TurnTickWorkaround>();
            base.Applied(@object);
        }

        public override void Remove(GameObject @object)
        {
            Object.GetCurrentCell().AddObject(Inhabitor);

            if (didReduceVolume && @object.TryGetPart(out LiquidVolume liquid))
            {
                liquid.MaxVolume += 1;
            }
        }

        public static bool IsInhabitable(GameObject @object)
        {
            return !(
                @object.HasEffectDescendedFrom<Inhabited>() ||
                @object.HasPartDescendedFrom<HDBrownie_Colony>()
            );
        }

        public override int GetEffectType()
        {
            return TYPE_STRUCTURAL | TYPE_NEGATIVE | TYPE_REMOVABLE;
        }

        public override bool WantTurnTick()
        {
            return true;
        }

        public override void TurnTick(long timeTick, int amount)
        {
            for (var n = 0; n < amount; n++)
            {
                Inhabitor.FireEvent(new Event("HDBrownie_InhabitingTick", "Nest", Object, "TimeTick", timeTick));
            }
            base.TurnTick(timeTick, amount);
        }

        public override bool WantEvent(int id, int cascade)
        {
            return
                base.WantEvent(id, cascade)
                || id == OnDestroyObjectEvent.ID
                || id == UseDramsEvent.ID
            ;
        }

        public override bool HandleEvent(OnDestroyObjectEvent @event)
        {
            if (@event.Obliterate) { goto Done; }

            var firedEvent = new Event("HDBrownie_NestDamaged", "Nest", Object);

            if (Inhabitor.FireEvent(firedEvent))
            {
                Remove(Object);
            }

        Done:
            return base.HandleEvent(@event);
        }

        public override bool HandleEvent(UseDramsEvent @event)
        {
            if (@event.Drinking)
            {
                Inhabitor.FireEvent(new Event("HDBrownie_NestDrank", "Nest", Object, "Drinker", @event.Actor));
            }
            return base.HandleEvent(@event);
        }
    }
}

namespace HDBrownie
{
    // This exists because Inhabited's TurnTick wasn't being called.
    public class TurnTickWorkaround : IPart
    {
        public override bool WantTurnTick()
        {
            return true;
        }

        public override void TurnTick(long timeTick, int amount)
        {
            ParentObject.GetEffectDescendedFrom<Inhabited>()?.TurnTick(timeTick, amount);
            base.TurnTick(timeTick, amount);
        }
    }
}
