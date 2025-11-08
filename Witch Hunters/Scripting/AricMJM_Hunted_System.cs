using System;
using XRL;
using XRL.Core;
using XRL.World;
using XRL.World.Parts;


namespace XRL.World.Quests
{

    [Serializable]
    public class AricMJM_Hunted_System : IQuestSystem
    {
        public override void Start()
        {

            if (The.Game.GetIntGameState("Aric_WHCaptain_Slain") == 1)
            {
                The.Game.FinishQuestStep("Hunting the Hunter", "Slay the Witch-Hunter General");

            }

        }

        // GetInfluencer is primarily used to determine who should be referenced when
        // naming an item. If the player successfully rolls an item naming opportunity
        // upon completing the quest, they will be able to name their item after the
        // culture of either Wardens Esther or Tszappur.
        public override GameObject GetInfluencer()
        {

            return GameObject.FindByBlueprint("BaseNaphtaali");
        }
    }
}

