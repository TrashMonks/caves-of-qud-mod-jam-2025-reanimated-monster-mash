using System;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts
{

    [Serializable]
    public class AricMJM_ModImprovedMenacingStare : IModification
    {
        public string MutationDisplayName ="Menacing Stare.";

        public string ClassName = "Persuasion_MenacingStare";

        public string TrackingProperty;

        public int moddedAmount;

        public Guid mutationMod;

        public AricMJM_ModImprovedMenacingStare()
        {
        }

 

        public override int GetModificationSlotUsage()
        {
            return 0;
        }

/*        public override void Configure()
        {
            T val = new T();
            SkillDisplayName = "Menacing Stare";
            ClassName = val.Name;
            TrackingProperty = "Equipped" + ClassName;
            WorksOnEquipper = true;
            NameForStatus = ClassName + "Amp";
        }
*/
        public override bool WantEvent(int ID, int cascade)
        {
            if (!base.WantEvent(ID, cascade) && ID != EquippedEvent.ID && ID != ImplantedEvent.ID && ID != GetShortDescriptionEvent.ID && ID != UnequippedEvent.ID)
            {
                return ID == UnimplantedEvent.ID;
            }
            return true;
        }

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            E.Postfix.AppendRules("Grants you the Menacing Stare skill.");
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EquippedEvent E)
        {
            if (ParentObject.IsEquippedProperly())
            {
                E.Actor.AddSkill("Persuasion_MenacingStare");
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(UnequippedEvent E)
        {
            E.Actor.RemoveSkill("Persuasion_MenacingStare");
            return base.HandleEvent(E);
        }

    }
}