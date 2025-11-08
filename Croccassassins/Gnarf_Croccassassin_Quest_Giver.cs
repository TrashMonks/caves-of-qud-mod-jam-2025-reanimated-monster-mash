using XRL.Language;
using XRL.World.Parts;
using XRL.World.Text;

namespace XRL.World.Conversations.Parts
{

    public class Gnarf_Croccassassin_Quest_Giver : IConversationPart
    {
        public override bool WantEvent(int ID, int Propagation)
        {
            return
                base.WantEvent(ID, Propagation)
                || ID == PrepareTextEvent.ID
                ;
        }

        public override bool HandleEvent(PrepareTextEvent E)
        {
            E.Text.Replace("=directions=", LoreGenerator.GenerateLandmarkDirectionsTo(The.Game.GetStringGameState("Gnarf_Chomping_Zone")));
            E.Text.Replace("=shoerack=", The.Game.GetStringGameState("Gnarf_Chomping_Rack_Name"));
            return base.HandleEvent(E);
        }

    }

    public class Gnarf_Croccassassin_Remove_Self : IConversationPart
    {
        public override bool WantEvent(int ID, int Propagation)
        {
            return
                base.WantEvent(ID, Propagation)
                || ID == EnteredElementEvent.ID
                ;
        }

        public override bool HandleEvent(EnteredElementEvent E)
        {
            var quest = The.Game.StartQuest("Gnarf_Croccassassin_Quest", The.Speaker?.DisplayName);
            var step = quest.StepsByID["Find the target"];
            step.Text = step.Text
                .Replace("=directions=", LoreGenerator.GenerateLandmarkDirectionsTo(The.Game.GetStringGameState("Gnarf_Chomping_Zone")));
            var me = The.Speaker;
            GameManager.Instance.gameQueue.queueTask(() => me.Destroy(Silent: true));
            return base.HandleEvent(E);
        }
        
    }

}