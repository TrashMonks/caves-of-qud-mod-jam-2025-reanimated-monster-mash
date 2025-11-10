using System;

using Qud.API;

using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.World.WorldBuilders;

using static XRL.World.WorldBuilders.UD_FleshGolems_MadMonger_WorldBuilder;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_MadMongerSurface : IPart
    {
        public string SecretID = SecretMapNote.ID;

        public string RevealKey = "UD_FleshGolems_MadMonger_LocationKnown";

        public override bool SameAs(IPart p)
        {
            return false;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("EnteredCell");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "EnteredCell" && !The.Game.HasIntGameState(RevealKey))
            {
                The.Game.SetIntGameState(RevealKey, 1);
                ZoneManager.instance.GetZone("JoppaWorld").BroadcastEvent("UD_FleshGolems_MadMongerReveal");
                if (SecretMapNote != null && !SecretMapNote.Revealed)
                {
                    JournalAPI.RevealMapNote(SecretMapNote);
                }
            }
            return base.FireEvent(E);
        }
    }
}