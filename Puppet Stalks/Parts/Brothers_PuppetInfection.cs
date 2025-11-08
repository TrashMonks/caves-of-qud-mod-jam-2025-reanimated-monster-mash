using System;
using System.Collections.Generic;
using Wintellect.PowerCollections;
using XRL.Language;
using XRL.Rules;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;

#nullable disable
namespace XRL.World.Parts
{

    [Serializable]
    public class Brothers_PuppetInfection : IPart
    {
        public string ColorString = "&amp;w";


        public override bool AllowStaticRegistration() => false;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("Equipped");
            Registrar.Register("Unequipped");
            base.Register(Object, Registrar);
        }

        public bool Puff()
        {
            Cell currentCell = this.ParentObject.GetCurrentCell();
            if (currentCell == null)
                return false;
            List<Cell> localAdjacentCells = currentCell.GetLocalAdjacentCells();
            bool flag1 = false;
            if (localAdjacentCells != null)
            {
                foreach (Cell cell in localAdjacentCells)
                {
                    bool flag2 = false;
                    bool flag3 = false;
                    foreach (GameObject loopObject in cell.LoopObjects())
                    {
                        if (!flag3 && loopObject.Brain != null)
                            flag3 = true;
                        if (!flag2 && loopObject.HasPart<GasFungalSpores>())
                            flag2 = true;
                        if (flag3 & flag2)
                            break;
                    }
                    if (flag3 && !flag2)
                    {
                        flag1 = true;
                        break;
                    }
                }
            }
            if (flag1)
            {
                GameObject gameObject1 = this.ParentObject.Equipped ?? this.ParentObject;
                if (gameObject1.CurrentCell != null)
                    gameObject1.ParticleBlip("&W*");
                for (int index = 0; index < localAdjacentCells.Count; ++index)
                {
                    if (!localAdjacentCells[index].HasObjectWithPart("GasFungalSpores"))
                    {
                        GameObject gameObject2 = localAdjacentCells[index].AddObject("Brothers_FungalSporeGasPuppet");
                        gameObject2.GetPart<Gas>().Creator = gameObject1;
                    }
                }
            }
            return flag1;
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "BeforeApplyDamage")
            {
                if (25.in100() && this.Puff())
                {
                    GameObject equipped = this.ParentObject.Equipped;
                    if (equipped != null)
                    {
                        if (equipped.IsPlayer())
                        {
                            BodyPart bodyPart = this.ParentObject.EquippedOn();
                            IComponent<GameObject>.AddPlayerMessage($"&yYour {bodyPart.GetOrdinalName()} {(bodyPart.Plural ? "spew" : "spews")} a cloud of spores.");
                        }
                        else if (IComponent<GameObject>.Visible(equipped))
                        {
                            BodyPart bodyPart = this.ParentObject.EquippedOn();
                            IComponent<GameObject>.AddPlayerMessage($"{Grammar.MakePossessive(equipped.The + equipped.ShortDisplayName)}&y {bodyPart.GetOrdinalName()} {(bodyPart.Plural ? "spew" : "spews")} a cloud of spores.");
                        }
                    }
                }
            }
            else if (E.ID == "Equipped")
                E.GetGameObjectParameter("EquippingObject").RegisterPartEvent((IPart)this, "BeforeApplyDamage");
            else if (E.ID == "Unequipped")
                E.GetGameObjectParameter("UnequippingObject").UnregisterPartEvent((IPart)this, "BeforeApplyDamage");
            return base.FireEvent(E);
        }
    }
}