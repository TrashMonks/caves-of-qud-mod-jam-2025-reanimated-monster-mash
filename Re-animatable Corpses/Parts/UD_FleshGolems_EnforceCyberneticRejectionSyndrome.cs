using System;
using System.Collections.Generic;
using System.Text;

using XRL.World.Anatomy;
using XRL.World.Effects;

using static UD_FleshGolems.Const;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_EnforceCyberneticRejectionSyndrome : IScribedPart
    {
        public bool AppliedInitial;

        public double CostStackMultiplier;

        public UD_FleshGolems_EnforceCyberneticRejectionSyndrome()
        {
            AppliedInitial = false;
            CostStackMultiplier = 1.0;
        }

        public static int GetCyberneticRejectionSyndromeChance(GameObject Implantee, GameObject InstalledCybernetic, int Cost, string Slots)
        {
            if (!Implantee.IsMutant())
                return 0;

            int cyberneticModifier = InstalledCybernetic.GetIntProperty("CyberneticRejectionSyndromeModifier");
            int implanteeModifier = Implantee.GetIntProperty("CyberneticRejectionSyndromeModifier");
            int chance = 5 + Cost + cyberneticModifier + implanteeModifier;
            if (Implantee.TryGetPart<Mutations>(out var Part) && Part.MutationList != null)
            {
                foreach (BaseMutation mutation in Part.MutationList)
                    if (!mutation.IsPhysical() && Slots != "Head")
                        chance += 1; // mental mutations only give their full level if the cybernetic is install in the head.
                    else
                        chance += mutation.Level;
            }
            return chance;
        }

        public static bool ProcessCybernetic(
            GameObject Implantee,
            GameObject InstalledCybernetic,
            double CostStackMultiplier = 1.0
            )
        {
            if (!InstalledCybernetic.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItemPart))
                return false;

            int chance = GetCyberneticRejectionSyndromeChance(
                Implantee: Implantee,
                InstalledCybernetic: InstalledCybernetic,
                Cost: cyberneticsBaseItemPart.Cost,
                Slots: cyberneticsBaseItemPart.Slots);

            if (chance < 1)
                return false;

            string cRS_Key = CyberneticsBaseItem.GetCyberneticRejectionSyndromeKey(Implantee);

            if (!InstalledCybernetic.HasIntProperty(cRS_Key))
                InstalledCybernetic.SetIntProperty(cRS_Key, chance.in100() ? 1 : 0);

            if (InstalledCybernetic.GetIntProperty(cRS_Key) < 1)
                return false;

            int cost = (int)Math.Ceiling(cyberneticsBaseItemPart.Cost * CostStackMultiplier);
            return Implantee.ForceApplyEffect(new CyberneticRejectionSyndrome(cost));
        }
        public bool ProcessCybernetic(GameObject InstalledCybernetic)
            => ProcessCybernetic(ParentObject, InstalledCybernetic, CostStackMultiplier);

        public static bool UnprocessCybernetic(
            GameObject Implantee,
            GameObject InstalledCybernetic,
            double CostStackMultiplier = 1.0
            )
        {
            string cRS_Key = CyberneticsBaseItem.GetCyberneticRejectionSyndromeKey(Implantee);
            if (InstalledCybernetic.GetIntProperty(cRS_Key) < 1)
                return false;

            if (!Implantee.TryGetEffect(out CyberneticRejectionSyndrome cRS))
                return false;

            if (!InstalledCybernetic.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItemPart))
                return false;

            cRS.Reduce((int)Math.Ceiling(cyberneticsBaseItemPart.Cost * CostStackMultiplier));

            return true;
        }
        public bool UnprocessCybernetic(GameObject InstalledCybernetic)
            => UnprocessCybernetic(ParentObject, InstalledCybernetic, CostStackMultiplier);

        public override bool AllowStaticRegistration() => true;

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == ImplantedEvent.ID
            || ID == UnimplantedEvent.ID
            || ID == EnteredCellEvent.ID
            || ID == GetDebugInternalsEvent.ID
            ;

        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (!AppliedInitial
                && ParentObject != null
                && E.Object == ParentObject)
            {
                AppliedInitial = true;

                foreach (GameObject installedCybernetic in E.Object.GetInstalledCybernetics())
                    ProcessCybernetic(installedCybernetic);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(ImplantedEvent E)
        {
            if (E.Implantee == ParentObject
                && E.Item != null)
                ProcessCybernetic(E.Item);

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(UnimplantedEvent E)
        {
            if (E.Implantee == ParentObject
                && E.Item != null)
                UnprocessCybernetic(E.Item);

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(AppliedInitial), AppliedInitial);
            E.AddEntry(this, nameof(CostStackMultiplier), CostStackMultiplier);
            return base.HandleEvent(E);
        }
    }
}
