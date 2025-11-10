using System;
using System.Collections.Generic;
using System.Text;

using UD_FleshGolems;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_CorpseIconColor : IIconColorPart
    {
        public UD_FleshGolems_CorpseIconColor()
        {
            TextForeground = "&r";
            TextForegroundPriority = 110;
            TileForeground = "&r";
            TileForegroundPriority = 110;
        }
        public UD_FleshGolems_CorpseIconColor(GameObjectBlueprint Blueprint)
            : this()
        {
            if (Blueprint != null)
            {
                if (Blueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.TileColor), out string tileColor))
                {
                    TextForeground = tileColor;
                    TextForegroundPriority = 110;
                    TileForeground = tileColor;
                    TileForegroundPriority = 110;
                }
                if (Blueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.DetailColor), out string detailColor))
                {
                    TileDetail = detailColor;
                    TileDetailPriority = 100;
                }
            }
        }
        public UD_FleshGolems_CorpseIconColor(string Blueprint)
            : this(GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint))
        {
        }

        public UD_FleshGolems_CorpseIconColor SetTileColorFromBlueprint(GameObjectBlueprint Blueprint)
        {
            if (Blueprint != null)
            {
                if (Blueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.TileColor), out string tileColor))
                {
                    TextForeground = tileColor;
                    TextForegroundPriority = 110;
                    TileForeground = tileColor;
                    TileForegroundPriority = 110;
                }
            }
            return this;
        }
        public UD_FleshGolems_CorpseIconColor SetTileColorFromBlueprint(string Blueprint)
        {
            return SetTileColorFromBlueprint(GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint));
        }

        public UD_FleshGolems_CorpseIconColor SetDetailColorFromBlueprint(GameObjectBlueprint Blueprint)
        {
            if (Blueprint != null)
            {
                if (Blueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.DetailColor), out string detailColor))
                {
                    TileDetail = detailColor;
                    TileDetailPriority = 100;
                }
            }
            return this;
        }
        public UD_FleshGolems_CorpseIconColor SetDetailColorFromBlueprint(string Blueprint)
        {
            return SetDetailColorFromBlueprint(GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint));
        }

        public UD_FleshGolems_CorpseIconColor SetColorsFromBlueprint(GameObjectBlueprint Blueprint)
        {
            return SetTileColorFromBlueprint(Blueprint).SetDetailColorFromBlueprint(Blueprint);
        }
        public UD_FleshGolems_CorpseIconColor SetColorsFromBlueprint(string Blueprint)
        {
            return SetTileColorFromBlueprint(Blueprint).SetDetailColorFromBlueprint(Blueprint);
        }
    }
}
