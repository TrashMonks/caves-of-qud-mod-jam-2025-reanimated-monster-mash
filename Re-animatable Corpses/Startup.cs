using System.Collections.Generic;
using System.Linq;
using System;

using Qud.UI;

using XRL;
using XRL.UI;
using XRL.World;

using static UD_FleshGolems.Const;

namespace UD_FleshGolems
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    public static class Startup
    {
        [ModSensitiveStaticCache]
        public static bool CachedCorpses = false;

        [GameBasedStaticCache(CreateInstance = false)]
        public static string _PlayerBlueprint = null;

        public static string PlayerBlueprint => _PlayerBlueprint ??= Utils.GetPlayerBlueprint();

        [GameBasedStaticCache(CreateInstance = false)]
        public static string _PlayerID = null;

        public static string PlayerID
        {
            get => _PlayerID = The.Player?.ID ?? _PlayerID;
            set => _PlayerID = value;
        }
    }
}