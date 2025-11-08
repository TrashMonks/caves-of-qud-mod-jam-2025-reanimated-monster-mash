using System.Linq;
using HistoryKit;
using XRL.World.Parts;

namespace XRL.World.WorldBuilders
{
    [JoppaWorldBuilderExtension]
    public class Gnarf_Croccassassin_WorldBuilder : IJoppaWorldBuilderExtension
    {
        public override void OnAfterBuild(JoppaWorldBuilder Builder)
        {
            base.OnAfterBuild(Builder);
            var snapjawFort = Builder.worldInfo.enemySettlements.Where(info => info.name == "a snapjaw fort").ToList().GetRandomElement();

            var rack = GameObjectFactory.create("Gnarf_Shoe Rack");
            rack.RequirePart<DisplayNameAdjectives>().AddAdjective(HistoricStringExpander.ExpandString("<spice.commonPhrases.odious.!random>").Color("K"));
            rack.Inventory.RemoveAll(obj => obj.Blueprint == "Croccasins");
            rack.SetImportant(true, true);
            var rackStep = rack.RequirePart<QuestStepFinisher>();
            rackStep.Quest = "Gnarf_Croccassassin_Quest";
            rackStep.Step = "Find the target";
            rackStep.Trigger = "Seen";
            rack.RequirePart<Gnarf_Croccassassin_Quest_Rack>();

            var rackId = The.ZoneManager.CacheObject(rack);
            The.ZoneManager.AddZonePostBuilder(snapjawFort.targetZone, "AddObjectBuilder", "Object", rackId);

            The.Game.SetStringGameState("Gnarf_Chomping_Zone", snapjawFort.targetZone);
            The.Game.SetStringGameState("Gnarf_Chomping_Zone_Secret", snapjawFort.secretID);
            The.Game.SetStringGameState("Gnarf_Chomping_Rack", rackId);
            The.Game.SetStringGameState("Gnarf_Chomping_Rack_Name", rack.GetDisplayName());

            // MetricsManager.LogInfo($"Adding {rack.GetDisplayName()} to {snapjawFort.targetZone}");
        }
    }
}