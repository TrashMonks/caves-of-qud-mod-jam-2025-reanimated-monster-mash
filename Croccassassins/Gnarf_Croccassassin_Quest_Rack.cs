using XRL.World.Conversations.Parts;

namespace XRL.World.Parts
{
    public class Gnarf_Croccassassin_Quest_Rack : IPart
    {
        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("CommandTakeObject");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "CommandTakeObject")
            {
                if (E.GetGameObjectParameter("Object") is GameObject obj
                    && obj.Blueprint == "Gnarf_Lurking Croc")
                {
                    The.Game.FinishQuestStep("Gnarf_Croccassassin_Quest", "Set the trap");
                }
            }
            return base.FireEvent(E);
        }
    }
}