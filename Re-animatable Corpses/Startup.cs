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
    [HasCallAfterGameLoaded]
    public static class Startup
    {
        [GameBasedStaticCache( CreateInstance = false )]
        [ModSensitiveStaticCache]
        public static string _PlayerBlueprint = null;

        public static string PlayerBlueprint => _PlayerBlueprint ??= Utils.GetPlayerBlueprint();

        // Start-up calls in order that they happen.

        [ModSensitiveCacheInit]
        public static void ModSensitiveCacheInit()
        {
            // Called at game startup and whenever mod configuration changes
        }

        [GameBasedCacheInit]
        public static void GameBasedCacheInit()
        {
            // Called once when world is first generated.

            // The.Game registered events should go here.

            UnityEngine.Debug.Log( nameof(Startup) + "." + nameof(GameBasedCacheInit) + ", " + nameof(PlayerBlueprint) + ": " + PlayerBlueprint ?? NULL);
        }

        // [PlayerMutator]

        // The.Player.FireEvent("GameRestored");
        // AfterGameLoadedEvent.Send(Return);  // Return is the game.

        [CallAfterGameLoaded]
        public static void OnLoadGameCallback()
        {
            // Gets called every time the game is loaded but not during generation

            UnityEngine.Debug.Log(nameof(Startup) + "." + nameof(GameBasedCacheInit) + ", " + nameof(PlayerBlueprint) + ": " + PlayerBlueprint ?? NULL);
        }

        //
        // End Startup calls
        // 
    }

    // [ModSensitiveCacheInit]

    // [GameBasedCacheInit]

    [PlayerMutator]
    public class LearnAllTheBytes : IPlayerMutator
    {
        public void mutate(GameObject player)
        {
            // Called once when the player is generated (a fair bit after they're created.
        }
    }

    // [CallAfterGameLoaded]
}