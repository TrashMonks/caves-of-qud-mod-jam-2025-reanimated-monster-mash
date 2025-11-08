using ConsoleLib.Console;
using System;
using System.Collections.Generic;
using XRL.Rules;

#nullable disable
namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class Brothers_ZSporePuffer : BaseMutation
    {
        public int Chance = 100;
        public int EnergyCost = 1000;
        public int nCooldown;
        public string PuffObject = "Brothers_FungalSporeGasPuppet";
        public string ColorString = "&G";

        public override string GetLevelText(int Level) => "You puff with the best of them.\n";

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == SingletonEvent<BeginTakeActionEvent>.ID
                || ID == GetAdjacentNavigationWeightEvent.ID;
        }

        public override bool HandleEvent(BeginTakeActionEvent E)
        {
            if (!this.ParentObject.IsPlayer())
                this.UseEnergy(this.EnergyCost, "Fungus Puff");

            --this.nCooldown;

            if (this.nCooldown <= 0 && this.Chance > 0 && (this.Chance >= 100 || Stat.Random(1, 100) < this.Chance))
            {
                bool flag = false;
                List<Cell> localAdjacentCells = this.ParentObject.Physics.CurrentCell.GetLocalAdjacentCells();
                if (localAdjacentCells != null)
                {
                    foreach (Cell cell in localAdjacentCells)
                    {
                        if (cell.HasObjectWithPart("Brain"))
                        {
                            foreach (GameObject obj in cell.GetObjectsWithPart("Brain"))
                            {
                                if (this.ParentObject.Brain == null || !this.ParentObject.Brain.IsAlliedTowards(obj))
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (flag)
                {
                    this.ParentObject.ParticleBlip("&W*");
                    for (int index = 0; index < localAdjacentCells.Count; ++index)
                    {
                        Gas part = localAdjacentCells[index].AddObject(this.PuffObject).GetPart<Gas>();
                        part.ColorString = this.ColorString;
                        part.Creator = this.ParentObject;
                    }
                    this.nCooldown = 20 + Stat.Random(1, 6);
                }
            }

            return (this.EnergyCost == 0 || this.ParentObject.IsPlayer()) && base.HandleEvent(E);
        }

        public override bool HandleEvent(GetAdjacentNavigationWeightEvent E)
        {
            if (!this.ParentObject.IsAlliedTowards(E.Actor))
                E.MinWeight(97);
            return base.HandleEvent(E);
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("ApplySpores");
            Registrar.Register("VillageInit");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "ApplySpores")
                return false;

            if (E.ID == "VillageInit" && this.ParentObject.GetBlueprint().DescendsFrom("Brothers_Puppets_Puffer"))
            {
                int? fg = ColorUtility.FindLastForeground(this.ColorString);
                char ch = (char)(fg ?? 77);
                if (!this.ParentObject.Render.ColorString.Contains(ch))
                    this.ParentObject.Render.DetailColor = ch.ToString();
            }

            return base.FireEvent(E);
        }

        public override bool ChangeLevel(int NewLevel) => base.ChangeLevel(NewLevel);

        public override bool Mutate(GameObject GO, int Level)
        {
            this.ColorString = GO.Render.ColorString;
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO) => base.Unmutate(GO);
    }
}
