using System;
using XRL.World.Effects;

#nullable disable
namespace XRL.World.Parts
{

    [Serializable]
    public class Brothers_DeathTimer : IPart
    {
        public int Timer = 5;


        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) ||  ID == SingletonEvent<EndTurnEvent>.ID;
        }


        public override bool HandleEvent(EndTurnEvent E)
        {
            this.OnEndTurn();
            return base.HandleEvent(E);
        }

        public void OnEndTurn()
        {
            this.Timer--;
            if (this.Timer > 0)
                return;
            this.ParentObject.Die();
        }

        public override bool SameAs(IPart p) => false;
    }
}
