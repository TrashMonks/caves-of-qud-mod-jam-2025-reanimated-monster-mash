using System;

#nullable disable
namespace XRL.World.Parts
{
    [Serializable]
    public class Brothers_ImmuneToSpores : IPart
    {

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == EquippedEvent.ID || ID == UnequippedEvent.ID || ID == BeforeApplyDamageEvent.ID;
        }



        public override bool HandleEvent(BeforeApplyDamageEvent E)
        {
            if (E.Object != this.ParentObject.Equipped || !E.Damage.HasAttribute("Gas"))
                return base.HandleEvent(E);
            NotifyTargetImmuneEvent.Send(E.Weapon, E.Object, E.Actor, E.Damage, (IComponent<GameObject>)this);
            E.Damage.Amount = 0;
            return false;
        }

        public override bool HandleEvent(EquippedEvent E)
        {
            E.Actor.RegisterPartEvent(this, "CanApplySpores");
            E.Actor.RegisterPartEvent(this, "ApplySpores");
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(UnequippedEvent E)
        {
            E.Actor.UnregisterPartEvent(this, "CanApplySpores");
            E.Actor.UnregisterPartEvent(this, "ApplySpores");
            return base.HandleEvent(E);
        }

        public override bool FireEvent(Event E)
        {
            return !(E.ID == "CanApplySpores") && !(E.ID == "ApplySpores") && base.FireEvent(E);
        }
    }
}
