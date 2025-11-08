using Genkit;
using Qud.API;
using XRL.World;
using XRL.World.Parts;

namespace XRL.World.WorldBuilders
{
  [JoppaWorldBuilderExtension]
  public class Gearlink_FORGETTABLE_RanchBuilderExtension : IJoppaWorldBuilderExtension
  {
    private string World = "JoppaWorld";
    public string secretID = "$Gearlink_FORGETTABLE_ranch";

    public override void OnAfterBuild(JoppaWorldBuilder builder)
    {
      // The game calls this method after JoppaWorld generation takes place.
      // JoppaWorld generation includes the creation of lairs, historic ruins, villages, and more.
      MetricsManager.LogInfo("Gearlink_FORGETTABLE_RanchBuilderExtension running");
      // Select zone
      Location2D location = builder.mutableMap.popMutableLocationInArea(
        18 * 3, // Min zone x inclusive
        18 * 3, // Min zone y inclusive
        20 * 3 - 1, // Max zone x inclusive
        21 * 3 - 1 // Max zone y inclusive
      );
      string zoneID = Zone.XYToID(this.World, location.X, location.Y, 10);

      // Save the zoneID so it can be accessed later
      builder.game.SetStringGameState("Gearlink_FORGETTABLE_ranchID", zoneID);

      // Add builders
      The.ZoneManager.ClearZoneBuilders(zoneID);
      The.ZoneManager.AddZonePostBuilder(zoneID, "ClearAll");
      The.ZoneManager.AddZonePostBuilder(zoneID, "MapBuilder", "FileName", (object)"Maps/Ranch.rpm");
      // The.ZoneManager.AddZonePostBuilder(zone, "Music", "Track", (object)"MoghrayiRemembrance");

      // Create secret
      builder.AddSecret(
        secretZone: zoneID,
        name: "abandoned ranch",
        adj: new string[3] { "pig", "settlement", "humanoid" },
        category: "Settlements",
        secretid: this.secretID
      );
      builder.AddLocationFinder(zoneID, secretID);

      // Create location info
      GeneratedLocationInfo generatedLocationInfo = new GeneratedLocationInfo{
        name = "abandoned ranch",
        targetZone = zoneID,
        zoneLocation = Location2D.Get(location.X, location.Y),
        secretID = this.secretID  
      };
      builder.worldInfo.friendlySettlements.Add(generatedLocationInfo);

      // Set zone as non-mutable
      builder.mutableMap.SetMutable(generatedLocationInfo.zoneLocation, 0);
    }
	}
}
