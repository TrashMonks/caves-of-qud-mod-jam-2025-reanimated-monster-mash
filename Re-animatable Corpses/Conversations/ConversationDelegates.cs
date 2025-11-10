using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Conversations;
using XRL.World.Effects;
using XRL.World.Parts;

namespace UD_FleshGolems
{
    [HasConversationDelegate]
    public static class ConversationDelegates
    {
        //
        // Predicates
        // 
        [ConversationDelegate(Speaker = true)]
        public static bool IfSourceOfSuffering(DelegateContext Context)
        {
            if (Conversation.Speaker is GameObject speaker
                && The.Player is GameObject player)
            {
                GameObject sufferer = null;
                if (Context.Target == speaker)
                {
                    sufferer = player;
                }
                else
                {
                    sufferer = speaker;
                }
                return sufferer.TryGetEffect(out UD_FleshGolems_UnendingSuffering unendingSuffering)
                    && unendingSuffering.SourceObject == Context.Target;
            }
            return false;
        }

        [ConversationDelegate(Speaker = true)]
        public static bool IfHaveCompanionWithBlueprint(DelegateContext Context)
        {
            return Context.Target.GetCompanionsReadonly(The.ActiveZone.Width, GO => GO.Blueprint == Context.Value).Count > 0;
        }

        [ConversationDelegate]
        public static bool IfCurrentNodeID(DelegateContext Context)
        {
            return ConversationUI.CurrentNode is Node currentNode
                && Context.Value.CachedCommaExpansion().Contains(currentNode.ID);
        }

        [ConversationDelegate]
        public static bool IfStateGreaterThanState(DelegateContext Context)
        {
            Context.Value.AsDelimitedSpans(',', out var State1, out var State2);
            return The.Game.GetIntGameState(new string(State1)) > The.Game.GetIntGameState(new string(State2));
        }

        [ConversationDelegate(Inverse = false)]
        public static bool IfHaveAnyMorphotype(DelegateContext Context)
        {
            bool haveMorphotype = Context.Target.TryGetPart(out Mutations mutations)
                && mutations.ActiveMutationList.Any(m => m.GetMutationEntry().Category.Name == "Morphotypes");
            return Context.Value.EqualsNoCase("true") == haveMorphotype;
        }

        [ConversationDelegate]
        public static bool IfHaveUnfinishedQuestPropertyValue(DelegateContext Context)
        {
            Context.Value.Split('`', out var questIDProperty, out var value);
            questIDProperty.Split('~', out var questID, out var propertyName);
            return The.Game.Quests.TryGetValue(questID, out Quest quest)
                && quest.GetProperty(propertyName) is var property
                && property.EqualsNoCase(value);
        }

        //
        // Actions
        // 
        [ConversationDelegate]
        public static void ModIntState(DelegateContext Context)
        {
            Context.Value.AsDelimitedSpans(',', out var State, out var Value);
            if (!Value.IsEmpty && int.TryParse(Value, out int result))
            {
                The.Game.ModIntGameState(new string(State), result);
            }
        }

        [ConversationDelegate]
        public static void MatchIntState(DelegateContext Context)
        {
            Context.Value.AsDelimitedSpans(',', out var MakeThisState, out var MatchThisOne);
            if (!MatchThisOne.IsEmpty || !The.Game.HasIntGameState(new string(MatchThisOne)))
            {
                The.Game.RemoveIntGameState(new string(MakeThisState));
            }
            else
            {
                int matchValue = The.Game.GetIntGameState(new string(MatchThisOne));
                The.Game.SetIntGameState(new string(MakeThisState), matchValue);
            }
        }
    }
}
