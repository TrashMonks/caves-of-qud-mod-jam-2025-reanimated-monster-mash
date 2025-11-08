using System;
using System.Collections.Generic;

namespace XRL.World.Parts
{
    // A colony of table lice.
    [Serializable]
    public class HDBrownie_Colony : IPart
    {
        public GameObject Progenitor;

        public HDBrownie_Colony() { }

        public override void Initialize()
        {
            var blueprint = GameObjectFactory.Factory.GetBlueprint("Creature");

            foreach (var statName in new List<string> { "Energy", "Speed" })
            {
                var stat = new Statistic(blueprint.Stats[statName]);
                stat.Owner = ParentObject;
                ParentObject.Statistics[statName] = stat;
            }

            // A colony is a little bit like a creature. It has opinions.
            ParentObject.Energy = ParentObject.Statistics.GetValue("Energy");
            ParentObject.RemovePart<Brain>();
            var brain = (Brain)Progenitor.Brain.DeepCopy(Progenitor);
            brain.Mobile = false;
            ParentObject.AddPart(brain);

            // Render the colony non-portable.
            ParentObject.Physics.Takeable = false;

            // It's made of bugs.
            ParentObject.Physics.Organic = true;
            ParentObject.RemovePart<Metal>();

            base.Initialize();
        }


        public HDBrownie_Colony(GameObject progenitor)
        {
            Progenitor = progenitor;
        }

        public override bool Render(RenderEvent @event)
        {
            @event.ColorString = $"&{Progenitor.GetForegroundColor()}";
            return base.Render(@event);
        }

        public override bool WantEvent(int id, int cascade)
        {
            return
                base.WantEvent(id, cascade)
                || id == AfterContentsTakenEvent.ID
                || id == GetDisplayNameEvent.ID
                || id == GetShortDescriptionEvent.ID
                || id == TakenEvent.ID
                || id == TookDamageEvent.ID
            ;
        }

        public override bool HandleEvent(AfterContentsTakenEvent @event)
        {
            Bite(@event.Actor);
            return base.HandleEvent(@event);
        }

        public override bool HandleEvent(GetDisplayNameEvent @event)
        {
            @event.AddAdjective("lice");
            return base.HandleEvent(@event);
        }

        public override bool HandleEvent(GetShortDescriptionEvent @event)
        {
            @event.Postfix.Append($"\nThis object appears to be made of {Progenitor.GetPluralName().Replace(" louses", " lice")}.");
            return base.HandleEvent(@event);
        }

        public override bool HandleEvent(TookDamageEvent @event)
        {
            var cell = ParentObject.CurrentCell.GetRandomLocalAdjacentCell(c => c.IsEmpty());
            if (cell != null)
            {
                var clone = Progenitor.DeepCopy();
                clone.PartyLeader = ParentObject;
                clone.RemovePart<GivesRep>();
                cell?.AddObject(clone);
            }
            return base.HandleEvent(@event);
        }

        public void Bite(GameObject @object)
        {
            Progenitor.MeleeAttackWithWeapon(Target: @object, Weapon: Progenitor.GetPrimaryWeapon());
        }
    }
}
