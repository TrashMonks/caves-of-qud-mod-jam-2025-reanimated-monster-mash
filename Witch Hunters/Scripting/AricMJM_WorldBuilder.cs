using Genkit;
using System;
using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.World.WorldBuilders;
using XRL.World.ZoneBuilders;

namespace Aric.MJM
{
    [JoppaWorldBuilderExtension]
    public class AricMJM_WorldBuilder : IJoppaWorldBuilderExtension
    {
        public MutabilityMap mutableMap = new MutabilityMap();
        public override void OnAfterBuild(JoppaWorldBuilder builder)
        {

            Zone WorldZone = The.ZoneManager.GetZone("JoppaWorld");
                      
            //Palladium Reef

            var AricCampPRlocation = builder.popMutableLocationOfTerrain("PalladiumReef", centerOnly: true);
            var AricCampPRzoneID = builder.ZoneIDFromXY("JoppaWorld", AricCampPRlocation.X, AricCampPRlocation.Y);

            The.ZoneManager.AddZoneBuilder(AricCampPRzoneID, 6000, "AricMJM_WH_Encampment");

            var PRSecret = builder.AddSecret(AricCampPRzoneID,
                 "Camp of the Witch Hunter General",
                    new string[2] { "lair", "templar" },
                   "Lairs",
                   "$Aric_MJM_WHEncampment");

            (The.ZoneManager.GetZone("JoppaWorld").GetCell(AricCampPRlocation.X / 3, AricCampPRlocation.Y / 3).GetFirstObjectWithPart("TerrainTravel")?.GetPart<TerrainTravel>()).AddEncounter(new EncounterEntry("The haunting memory of a scream fills the air. A camp of vile templar witch hunters is near. Will you investigate?", AricCampPRzoneID, "", "$Aric_MJM_WHEncampment", Optional: true));


            builder.mutableMap.SetMutable(AricCampPRlocation, 0);



        }

    }
}
