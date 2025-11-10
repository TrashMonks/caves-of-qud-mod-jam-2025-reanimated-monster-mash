using System;
using System.Collections.Generic;
using System.Text;

using XRL.World.Anatomy;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_SetDefaultBehaviorBlueprintWhenDefaultBehavior : IScribedPart
    {
        [SerializeField]
        private bool Assigned = false;

        public override bool AllowStaticRegistration() => true;

        public override bool WantEvent(int ID, int Cascade) => base.WantEvent(ID, Cascade)
            || ID == EquippedEvent.ID;

        public override bool HandleEvent(EquippedEvent E)
        {
            if (!Assigned
                && E.Item is GameObject item
                && item == ParentObject
                && E.Part is BodyPart bodyPart
                && bodyPart.DefaultBehavior == item
                && bodyPart.DefaultBehaviorBlueprint != item.Blueprint)
            {
                bodyPart.DefaultBehaviorBlueprint = item.Blueprint;
                Assigned = bodyPart.DefaultBehaviorBlueprint == item.Blueprint;
            }
            return base.HandleEvent(E);
        }
    }
}
