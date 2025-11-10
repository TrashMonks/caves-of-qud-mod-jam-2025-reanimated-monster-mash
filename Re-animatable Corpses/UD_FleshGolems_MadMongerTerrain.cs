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
    public class UD_FleshGolems_MadMongerTerrain : IPart
    {
        public string SecretID = SecretMapNote.ID;

        public bool Revealed;

        public override bool SameAs(IPart p)
        {
            return false;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Object.SetIntProperty("ForceMutableSave", 1);
            Registrar.Register("UD_FleshGolems_MadMongerReveal");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "UD_FleshGolems_MadMongerReveal")
            {
                ParentObject.Render.Tile = "Terrain/sw_joppa.bmp";
                ParentObject.Render.ColorString = "&W";
                ParentObject.Render.DisplayName = SecretMapNote.Text.Capitalize();
                ParentObject.Render.DetailColor = "r";
                ParentObject.Render.RenderString = "#";
                ParentObject.HasProperName = true;
                ParentObject.GetPart<Description>().Short = "Near the jungle-strangled ruins of Bethesda Susa sits a make-shift lab tucked away in a small clearing.";
                ParentObject.GetPart<TerrainTravel>()?.ClearEncounters();
                ParentObject.SetStringProperty("OverlayColor", "&W");
                SecretMapNote?.Reveal();
            }
            return base.FireEvent(E);
        }
    }
}