using System;
using System.Collections.Generic;

using Genkit;

using Qud.API;

using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.Wish;
using XRL.World.Parts;
using XRL.World.ZoneBuilders;

namespace XRL.World.WorldBuilders
{
    [HasGameBasedStaticCache]
    [HasWishCommand]
    [JoppaWorldBuilderExtension]
    public class UD_FleshGolems_MadMonger_WorldBuilder : IJoppaWorldBuilderExtension
    {
        public const string SECRETID_MAD_MONGER = "$UD_FleshGolems_MadMonger";

        public JoppaWorldBuilder Builder;

        [GameBasedStaticCache( CreateInstance = false )]
        private static string _SecretZoneID;
        public static string SecretZoneID
        {
            get => _SecretZoneID ??= SecretMapNote?.ZoneID;
            set => _SecretZoneID = value;
        }

        [GameBasedStaticCache(CreateInstance = false)]
        private static JournalMapNote _SecretMapNote;
        public static JournalMapNote SecretMapNote
        {
            get => _SecretMapNote ??= JournalAPI.GetMapNote(SECRETID_MAD_MONGER);
            set => _SecretMapNote = value;
        }

        public Zone SecretZone => The.ZoneManager.GetZone(SecretZoneID);

        public override void OnAfterBuild(JoppaWorldBuilder Builder)
        {
            MetricsManager.rngCheckpoint("UD_FleshGolems_MadMonger_Lair_Start");
            this.Builder = Builder;
            Builder.BuildStep("Maddening Science", MaddeningScience);
            MetricsManager.rngCheckpoint("UD_FleshGolems_MadMonger_Lair_Finish");
        }

        public void MaddeningScience(string WorldID)
        {
            if (WorldID != "JoppaWorld")
            {
                return;
            }

            WorldCreationProgress.StepProgress("Maddening science...");

            GameObject theMadMonger = GameObjectFactory.Factory.CreateObject(
                ObjectBlueprint: "UD_FleshGolems Mad Monger",
                Context: nameof(UD_FleshGolems_MadMonger_WorldBuilder));

            string madMongerRefname = theMadMonger.GetReferenceDisplayName(Context: "LairName");

            // Location2D parasang = Builder.getLocationOfTier(zoneTier);
            Location2D secretParasang = Builder.getLocationWithinNFromTerrainBlueprintTier(1, 2, "TerrainBethesdaSusa", 3);
            Location2D secretZone = Location2D.Get(secretParasang.X * 3 + 1, secretParasang.Y * 3 + 1);
            SecretZoneID = Builder.ZoneIDFromXY("JoppaWorld", secretZone.X, secretZone.Y);

            string secretID = null;

            string madMongerLairTable = "DynamicInheritsTable:" + theMadMonger.GetBlueprint().Inherits + ":Tier" + The.ZoneManager.GetZoneTier(SecretZoneID);
            string objectTypeForZone = ZoneManager.GetObjectTypeForZone(secretParasang.X, secretParasang.Y, "JoppaWorld");
            string lairAdjectives = theMadMonger.GetPropertyOrTag("LairAdjectives", "");
            if (lairAdjectives.Length > 0)
            {
                lairAdjectives += ",";
            }
            lairAdjectives += GameObjectFactory.Factory.Blueprints[objectTypeForZone].GetTag("LairAdjectives", "lair");

            secretID = Builder.AddSecret(SecretZoneID, "the lab of " + madMongerRefname, new string[1] { "lair" }, "Lairs", SECRETID_MAD_MONGER);

            SecretRevealer secretRevealer = theMadMonger.RequirePart<SecretRevealer>();
            secretRevealer.id = secretID;
            secretRevealer.text = $"the location of the lab of {madMongerRefname}";
            secretRevealer.message = $"You have discovered {secretRevealer.text}!";
            secretRevealer.category = "Lairs";
            secretRevealer.adjectives = "lair";

            if (JournalAPI.GetMapNote(SECRETID_MAD_MONGER) == null)
            {
                JournalAPI.AddMapNote(
                    ZoneID: SecretZoneID,
                    text: secretRevealer.text,
                    category: secretRevealer.category,
                    attributes: secretRevealer.adjectives.CachedCommaExpansion().ToArray(),
                    secretId: SECRETID_MAD_MONGER
                );
            }
            SecretMapNote = JournalAPI.GetMapNote(SECRETID_MAD_MONGER);
            SecretMapNote.Weight = 30000;

            The.ZoneManager.ClearZoneBuilders(SecretZoneID);
            The.ZoneManager.SetZoneProperty(SecretZoneID, "SkipTerrainBuilders", true);
            The.ZoneManager.AddZonePostBuilder(SecretZoneID, "BasicLair", "Table", madMongerLairTable, "Adjectives", lairAdjectives);
            The.ZoneManager.AddZonePostBuilder(SecretZoneID, "AddObjectBuilder", "Object", The.ZoneManager.CacheObject(theMadMonger));
            The.ZoneManager.AddZonePostBuilder(SecretZoneID, "AddWidgetBuilder", "Blueprint", "UD_FleshGolems_MadMonger_LabSurface");

            Zone worldMap = The.ZoneManager.GetZone("JoppaWorld");
            Cell secretWorldMapCell = The.ZoneManager.GetZone(WorldID).GetCell(secretParasang.X, secretParasang.Y);
            GameObject secretTerrainObject = secretWorldMapCell.GetFirstObjectWithPart("TerrainTravel");
            TerrainTravel terrainTravel = null;
            Render render = null;
            if (secretTerrainObject != null)
            {
                terrainTravel = secretTerrainObject.GetPart<TerrainTravel>();
                render = secretTerrainObject.GetPart<Render>();
            }
            secretWorldMapCell.GetFirstObjectWithPart("TerrainTravel").AddPart(new UD_FleshGolems_MadMongerTerrain());
            if (Options.ShowOverlandEncounters && render != null)
            {
                render.RenderString = "&W*";
                render.ParentObject.SetStringProperty("OverlayColor", "&M");
            }
            terrainTravel?.AddEncounter(Entry: new EncounterEntry(
                Text: theMadMonger.GetBlueprint().GetTag("LairPulldownMessage", "You discover a lair. Would you like to investigate?"), 
                Zone: SecretZoneID, 
                Replacement: "",
                Secret: secretID, Optional: true));
            GeneratedLocationInfo generatedLocationInfo = new()
            {
                targetZone = Zone.XYToID("JoppaWorld", secretZone.X, secretZone.Y, 10),
                zoneLocation = secretZone,
                name = "the lab of " + madMongerRefname,
                ownerID = theMadMonger.ID,
                secretID = SECRETID_MAD_MONGER
            };
            Builder.worldInfo.lairs.Add(generatedLocationInfo);
        }

        [WishCommand(Command = "UD_FleshGolems go Mad Monger")]
        public static void GoMadMonger_WishHandler()
        {
            if (The.ZoneManager.GetZone(SecretZoneID) is Zone Z
                && The.Player is GameObject player
                && player.Physics is Physics playerPhysics)
            {
                playerPhysics.CurrentCell.RemoveObject(playerPhysics.ParentObject);
                Z.GetEmptyCells().GetRandomElement()?.AddObject(player);
                The.ZoneManager.SetActiveZone(Z);
                The.ZoneManager.ProcessGoToPartyLeader();
            }
            else
            {
                Popup.Show("Can't find the Mad Monger Zone. This is a bug.");
            }
        }
    }
}
