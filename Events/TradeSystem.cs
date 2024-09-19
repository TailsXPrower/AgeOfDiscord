using AgeOfDiscord.UI;
using Sandbox.GameData;
using Sandbox.Utils;

namespace Sandbox.Events;

public class TradeSystem : Component
{
	public static Dictionary<Guid, TradeOffer> Offers = new();
	
	protected override void OnFixedUpdate()
	{
		if (Scene.Components.GetInDescendants<Settings>() == null)
			return;
		
		var time = Scene.Components.GetInDescendants<TimeComponent>();
		
		if (!time.IsHourTicked())
			return;
		
		if (IsProxy)
			return;
		
		foreach (var tradeOffer in new List<TradeOffer>(Offers.Values))
		{
			if (tradeOffer.IsProcessing)
				return;
			
			try
			{
				tradeOffer.Process( Scene );
			}
			catch (Exception ex)
			{
				if ( !tradeOffer.From.IsBot( Scene ) )
				{
					using ( Rpc.FilterInclude( tradeOffer.From.GetPlayer( Scene ).Network.OwnerConnection ) )
					{
						Logger.Notify(Scene.Components.GetInDescendants<GameUI>(), "Offer was cancelled due to: "+ex.Message, "So sad..");	
					}	
				}
				RemoveOffer( tradeOffer.Id );
			}
		}
	}

	[Broadcast]
	public static void RemoveOffer( Guid uid, bool isCancelled = false )
	{
		if (Offers.Remove( uid, out var tradeOffer ))
			tradeOffer.IsCancelled = isCancelled;
	}

	public static bool HasOffer( Country country )
	{
		return Offers.Values.Any(offer => offer.To == country);
	}
}
