namespace XRL.World.ZoneBuilders
{
    public class AricMJM_WH_Encampment
    {
        public bool BuildZone(Zone Z)
        {
            return new AricMJMEncampmentMaker().BuildZone(Z, bRoads: true, "CanvasWall", RoundBuildings: false, "3d1", "5-Cistern", "AricMJM_Witch_Hunter_Tent", "AricMJM_Witch_Hunter_Camp", null, ClearCombatObjectsFirst: true, "AricMJM_Witch_Hunter_General");
        }
    }
}