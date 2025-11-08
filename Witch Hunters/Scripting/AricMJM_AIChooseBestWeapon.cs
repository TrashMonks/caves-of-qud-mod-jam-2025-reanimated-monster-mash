/*using System;
using NUnit.Framework;
using Qud.API;

namespace XRL.World.Parts
{
    [Serializable]

    public class AricMJM_AIChooseBestWEapon : AIBehaviorPart
    {
        string MundaneWeapon = "";
        string PsionicWeapon = "";

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade) || ID == AIGetOffensiveAbilityListEvent.ID;


        }

        public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
        {

            int TargetAV = E.Target.Stat("AV");
            int TargetMA = E.Target.Stat("MA");

            bool WantMundane=false;
            bool WantPsionic=false;

            if (TargetAV < TargetMA)
            {
                WantPsionic = true;
            }
            else
            {
                WantMundane = true;
            }

            


            //check what's equipped

            //if mundaneweapon is equipped and wantpsionic is true, equip PsionicWeapon

            // if psionicweapon is equipped and wantmundane is true, equip mundaneweapon

            //else return base




            return base.HandleEvent(E);
        }

    }
}*/