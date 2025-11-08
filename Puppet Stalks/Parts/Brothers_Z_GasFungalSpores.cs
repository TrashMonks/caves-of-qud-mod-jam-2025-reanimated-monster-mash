using System;
using XRL.Rules;
using XRL.World.Effects;

#nullable disable
namespace XRL.World.Parts
{

    [Serializable]
    public class Brothers_Z_GasFungalSpores : IObjectGasBehavior
    {


        public string Infection = "LuminousInfection";
        public string GasType = "FungalSpores";
        public int GasLevel = 1;

        public override bool SameAs(IPart p)
        {
            Brothers_Z_GasFungalSpores brothers_z_gasFungalSpores = p as Brothers_Z_GasFungalSpores;
            return !(brothers_z_gasFungalSpores.Infection != this.Infection) && !(brothers_z_gasFungalSpores.GasType != this.GasType) && brothers_z_gasFungalSpores.GasLevel == this.GasLevel && base.SameAs(p);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) || ID == GetAdjacentNavigationWeightEvent.ID || ID == GetNavigationWeightEvent.ID;
        }

        public override bool HandleEvent(GetNavigationWeightEvent E)
        {
            if (!E.IgnoreGases && E.PhaseMatches(this.ParentObject))
            {
                if (E.Smart)
                {
                    E.Uncacheable = true;
                    if (CheckGasCanAffectEvent.Check(E.Actor, this.ParentObject) && (E.Actor == null || E.Actor != this.ParentObject.GetPart<Gas>()?.Creator && E.Actor.FireEvent("CanApplySpores") && !E.Actor.HasEffect<FungalSporeInfection>() && E.Actor.PhaseMatches(this.ParentObject)))
                        E.MinWeight(this.GasDensityStepped() / 2 + 20, 80 /*0x50*/);
                }
                else
                    E.MinWeight(10);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetAdjacentNavigationWeightEvent E)
        {
            if (!E.IgnoreGases && E.PhaseMatches(this.ParentObject))
            {
                if (E.Smart)
                {
                    E.Uncacheable = true;
                    if (CheckGasCanAffectEvent.Check(E.Actor, this.ParentObject) && (E.Actor == null || E.Actor != this.ParentObject.GetPart<Gas>()?.Creator && E.Actor.FireEvent("CanApplySpores") && !E.Actor.HasEffect<FungalSporeInfection>() && E.Actor.PhaseMatches(this.ParentObject)))
                        E.MinWeight(this.GasDensityStepped() / 10 + 4, 16 /*0x10*/);
                }
                else
                    E.MinWeight(2);
            }
            return base.HandleEvent(E);
        }

        public override bool ApplyGas(GameObject Object)
        {
            if (Object == this.ParentObject)
                return false;

            // added corpse handling here
            if (Object.HasTag("Corpse"))
            {
                // display message
                IComponent<GameObject>.AddPlayerMessage($"Swarming ascomata burst out from {Object.the}{Object.DisplayNameOnly}, reshaped into a shambling dybbuk.");

                // remove corpse and spawn zombie
                
                //Get zombie blueprint name
                string zombieName = Brothers_ZombieNameBuilder.GetZombieName(Object);

                GameObject zombie = GameObject.Create(zombieName);
                Cell cell = Object.GetCurrentCell();
                if (cell != null)
                {
                    cell.AddObject(zombie);
                }
                Object.Destroy();
                return true;
            }

            GameObject gameObject = (GameObject)null;
            Gas part = this.ParentObject.GetPart<Gas>();
            if (part != null)
                gameObject = part.Creator;
            if (Object == gameObject || !CheckGasCanAffectEvent.Check(Object, this.ParentObject, part) || !Object.FireEvent("CanApplySpores") || !Object.FireEvent("ApplySpores") || Object.HasTagOrProperty("ImmuneToFungus") || !Object.PhaseMatches(this.ParentObject))
                return false;
            Object.RemoveEffect<SporeCloudPoison>();
            if (!Object.HasStat("Toughness") || Object.HasEffect<FungalSporeInfection>())
                return false;
            bool flag1 = false;
            SporeCloudPoison E1 = Object.GetEffect<SporeCloudPoison>();
            if (this.Infection == "PaxInfection")
            {
                this.GasLevel = 8;
            }
            else
            {
                int Duration = Stat.Random(2, 5);
                int num = E1 != null ? 1 : 0;
                if (E1 == null)
                {
                    E1 = new SporeCloudPoison(Duration, part?.Creator);
                }
                else
                {
                    if (E1.Duration < Duration)
                        E1.Duration = Duration;
                    if (!GameObject.Validate(ref E1.Owner) && part != null && part.Creator != null)
                        E1.Owner = part.Creator;
                }
                E1.Damage = Math.Min(1, this.GasLevel);
                if (num != 0 || Object.ApplyEffect((Effect)E1))
                    flag1 = true;
            }
            int Difficulty = 10 + this.GasLevel / 3;
            if (!Object.MakeSave("Toughness", Difficulty, Vs: "Fungal Disease Contact Gas", Source: this.ParentObject))
            {
                if (Object.IsPlayer())
                    IComponent<GameObject>.AddPlayerMessage("Your skin itches.");
                Event E2 = new Event("BeforeApplyFungalInfection");
                bool flag2 = false;
                if (!Object.FireEvent(E2) || E2.HasParameter("Cancelled"))
                    flag2 = true;
                FungalSporeInfection E3;
                if (this.Infection == "PaxInfection")
                {
                    E3 = new FungalSporeInfection(3, this.Infection);
                }
                else
                {
                    E3 = new FungalSporeInfection(flag2 ? Stat.Random(8, 10) : Stat.Random(20, 30) * 120, this.Infection);
                    E3.Fake = flag2;
                }
                if (Object.ApplyEffect((Effect)E3))
                    flag1 = true;
            }
            return flag1;
        }
    }
}