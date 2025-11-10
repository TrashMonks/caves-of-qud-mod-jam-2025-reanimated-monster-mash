using System;
using System.Collections.Generic;

using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Conversations.Parts;
using XRL.World.QuestManagers;

namespace XRL.World.Quests
{
    [Serializable]
    public class UD_FleshGolems_YouRaiseMeUpQuestSystem : IQuestSystem
    {
        public const string ModPrefix = "UD_FleshGolems ";

        public const string MongerQuestID = ModPrefix + "You Raise Me Up";

        public static UD_FleshGolems_CorpseQuestSystem CorpseQuestSystem = The.Game.RequireSystem<UD_FleshGolems_CorpseQuestSystem>();

        public static List<string> StepIDs => new()
        {
            ModPrefix + "I'm Dead Serious!",
            ModPrefix + "Don't be nervous!",
            ModPrefix + "A real fixer-upper!",
            ModPrefix + "You must construct additional pylons!",
            ModPrefix + "Flip the switch!",
        };

        public List<QuestStep> QuestSteps => new(Quest?.StepsByID.Values);

        public UD_FleshGolems_YouRaiseMeUpQuestSystem()
        {
            QuestID = MongerQuestID;
        }

        public override void Start()
        {
            UD_FleshGolems_CorpseQuestSystem.StartQuest(this, Quest);
            base.Start();
        }

        public override void Register(XRLGame Game, IEventRegistrar Registrar)
        {
            Registrar.Register(ZoneActivatedEvent.ID);
            Registrar.Register(QuestStepFinishedEvent.ID);
        }

        public override void RegisterPlayer(GameObject Player, IEventRegistrar Registrar)
        {
            Registrar.Register(AfterConsumeEvent.ID);
        }

        public override bool HandleEvent(QuestStepFinishedEvent E)
        {
            // UnityEngine.Debug.Log(E.Quest + ", " + E.Step.ID + " Finished");
            if (E.Quest == Quest
                && E.Step is QuestStep thisStep
                && QuestSteps.Contains(thisStep))
            {
                if (thisStep.ID == StepIDs[0]
                    && The.Game.Quests.TryGetValue(CorpseQuestSystem.QuestID, out Quest corpseQuest))
                {
                    corpseQuest.Finish();
                }
                if (QuestSteps[^1] != thisStep
                    && QuestSteps[QuestSteps.IndexOf(E.Step) + 1] is QuestStep nextStep)
                {

                    nextStep.Hidden = false;
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ZoneActivatedEvent E)
        {
            
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AfterConsumeEvent E)
        {
            
            return base.HandleEvent(E);
        }

        public override GameObject GetInfluencer()
        {
            return GameObject.FindByBlueprint("UD_FleshGolems Mad Monger");
        }
    }
}