using System;
using System.Collections.Generic;
using System.Text;

using XRL.World;
using XRL.Liquids;

namespace XRL.World.Parts
{
    public  class UD_FleshGolems_CorpseQuestHelperPart : IScribedPart
    {
        public bool Primed;

        public bool IsCorpse => ParentObject != null
            && ParentObject.GetBlueprint().InheritsFrom("Corpse");

        public bool IsBrainBrine => ParentObject != null
            && ParentObject.TryGetPart(out LiquidVolume liquidVolume)
            && liquidVolume.IsPureLiquid(false)
            && liquidVolume.Primary == LiquidBrainBrine.ID;

        public bool IsMedicalBed => ParentObject != null
            && ParentObject.HasPart<Bed>() 
            && ParentObject.GetBlueprint().InheritsFrom("Medical Bed");

        public bool IsElectrothing => ParentObject != null
            && ParentObject.GetBlueprint().InheritsFrom("Electrothing");

        public UD_FleshGolems_CorpseQuestHelperPart()
        {
            Primed = false;
        }

        public static bool ObjectIsCorpse(GameObject GO)
        {
            return GO.TryGetPart(out UD_FleshGolems_CorpseQuestHelperPart corpseQuestHelperPart)
                && corpseQuestHelperPart.IsBrainBrine;
        }
        public static bool ObjectIsBrainBrine(GameObject GO)
        {
            return GO.TryGetPart(out UD_FleshGolems_CorpseQuestHelperPart corpseQuestHelperPart)
                && corpseQuestHelperPart.IsBrainBrine;
        }
        public static bool ObjectIsMedicalBed(GameObject GO)
        {
            return GO.TryGetPart(out UD_FleshGolems_CorpseQuestHelperPart corpseQuestHelperPart)
                && corpseQuestHelperPart.IsMedicalBed;
        }
        public static bool ObjectIsBrainBrineOrMedicalBed(GameObject GO)
        {
            return ObjectIsBrainBrine(GO) || ObjectIsMedicalBed(GO);
        }

        public override bool WantEvent(int ID, int Cascade) => base.WantEvent(ID, Cascade)
            || ID == EnteredCellEvent.ID
            || ID == BeforeApplyDamageEvent.ID;

        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (E.Object == ParentObject)
            {
                if ((IsCorpse && !E.Cell.GetObjects(ObjectIsBrainBrineOrMedicalBed).IsNullOrEmpty())
                    || (IsBrainBrine && E.Cell.GetObjects(ObjectIsCorpse).Count > 2 && !E.Cell.GetObjects(ObjectIsMedicalBed).IsNullOrEmpty())
                    || (IsMedicalBed && E.Cell.GetObjects(ObjectIsCorpse).Count > 2 && !E.Cell.GetObjects(ObjectIsBrainBrine).IsNullOrEmpty()))
                Primed = true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeApplyDamageEvent E)
        {
            if (E.Damage.IsElectricDamage())
            {
                if (Primed || (IsElectrothing && ParentObject.CurrentCell.AnyAdjacentCell(c => !c.GetObjects(ObjectIsMedicalBed).IsNullOrEmpty())))
                {
                    E.Damage.Amount = Math.Min(ParentObject.GetStat("Hitpoints").Value - 1, E.Damage.Amount);
                }
            }
            return base.HandleEvent(E);
        }
    }
}
