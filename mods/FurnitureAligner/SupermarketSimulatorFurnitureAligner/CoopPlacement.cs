using Photon.Pun;

namespace SupermarketSimulatorFurnitureAligner;

internal static class CoopPlacement
{
	internal static bool InMultiplayer
	{
		get
		{
			try
			{
				return PhotonNetwork.InRoom;
			}
			catch
			{
				return false;
			}
		}
	}

	internal static bool AllowOutsideBypass =>
		FurnitureAlignerRuntime.IsActive
		&& FurnitureAlignerPlugin.AllowOutside.Value
		&& !InMultiplayer;
}
