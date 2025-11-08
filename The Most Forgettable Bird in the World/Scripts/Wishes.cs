using XRL;
using XRL.UI;
using XRL.Wish;

[HasWishCommand]
public class Gearlink_FORGETTABLE_WishHandler
{
  // Handles "testwish" with nothing else! (no string param)
  [WishCommand(Command = "Gearlink_FORGETTABLE_gotoranch")]
  public static void Gearlink_FORGETTABLE_GoToRanchHandler()
  {
    Popup.Show("Going to the ranch!");
    The.Player.ZoneTeleport(
      The.Game.GetStringGameState("Gearlink_FORGETTABLE_ranchID"),
      74,
      0
    );
  }
}