using System.Text.Json.Nodes;
using AgeOfDiscord.UI;
using AgeOfDiscord.UI.Menu;
using Sandbox.Events;
using Sandbox.Exceptions;
using Sandbox.Utils;

namespace Sandbox.GameData;

public class TradeOffer
{
	public Guid Id;
	
	public static Dictionary<string, float> ExchangeRates = new()
	{
		{"Coins", 10},
		{"Wood", 1},
		{"Ore", 2},
		{"Manpower", 0.1f}
	};
	
	public Country From;
	public Country To;
	
	public Dictionary<string, int> FromOffer;
	public Dictionary<string, int> ToOffer;

	public bool IsProcessing;
	public bool IsCancelled;

	public void Process( Scene scene )
	{
		IsProcessing = true;
		
		if ( To.IsBot( scene ) )
		{
			if ( !IsFairTrade() )
			{
            	throw new GameException( "Trade is not fair." );
            }
            		
			if ( !HasEnough() )
			{
				throw new GameException( "Trade is not fair." );
			}

			Success(Id);
            
			if ( !From.IsBot( scene ) )
			{
				using (Rpc.FilterInclude(From.GetPlayer( scene ).Network.OwnerConnection))
				{
					Logger.Notify(scene.Components.GetInDescendants<GameUI>(), $"{To.Name} has accepted your offer", "Perfect!");	
				}
			}	
			
			TradeSystem.RemoveOffer( Id );
		}
		else
		{
			if ( !HasEnough() )
			{
				throw new GameException( "Trade is not fair." );
			}
			
			To.LastTrades.Add( this );
			
			using ( Rpc.FilterInclude( To.GetPlayer( scene ).Network.OwnerConnection ) )
			{
				SendConfirmation( Id, scene.Components.GetInDescendants<GameUI>() );
			}
		}
	}

	[Broadcast]
	private static void SendConfirmation(Guid id, GameUI gameUi)
	{
		Sound.Play("trade");
		var trade = TradeSystem.Offers[id];
		var confirmation = new Confirmation
		{
			OnAgree = () =>
			{
				Success( id );
				
				if ( !trade.From.IsBot( gameUi.Scene ) )
				{
					using ( Rpc.FilterInclude( trade.From.GetPlayer( gameUi.Scene ).Network.OwnerConnection ) )
					{
						Logger.Notify(gameUi, $"{trade.To.Name} has accepted your offer", "Perfect!");	
					}		
				}
			},
			OnDisagree = () =>
			{
				TradeSystem.RemoveOffer( trade.Id, true );

				if ( !trade.From.IsBot( gameUi.Scene ) )
				{
					using ( Rpc.FilterInclude( trade.From.GetPlayer( gameUi.Scene ).Network.OwnerConnection ) )
					{
						Logger.Notify(gameUi, $"Offer to {trade.To.Name} was cancelled..", "So sad..");	
					}		
				}
			},
			Message = $"{trade.From.Name} is offering {trade.FromOffer.Aggregate("", ( current, pair ) => current + $" {pair.Value} {pair.Key}, " )}in exchange for {trade.ToOffer.Aggregate("", ( current, pair ) => current + $" {pair.Value} {pair.Key}, " )}"
		};
		gameUi.Panel.AddChild( confirmation );
	}

	[Broadcast]
	private static void Success(Guid id)
	{
		var trade = TradeSystem.Offers[id];
		foreach (var pair in trade.FromOffer)
		{
			switch (pair.Key)
			{
				case "Coins":
					trade.From.Balance -= pair.Value;
					trade.To.Balance += pair.Value;
					break;
				case "Wood":
					trade.From.Wood -= pair.Value;
					trade.To.Wood += pair.Value;
					break;
				case "Ore":
					trade.From.Ore -= pair.Value;
					trade.To.Ore += pair.Value;
					break;
				case "Manpower":
					trade.From.SubtractManpower(pair.Value);
					trade.To.AddManpower(pair.Value);
					break;
			}
		}
            		
		foreach (var pair in trade.ToOffer)
		{
			switch (pair.Key)
			{
				case "Coins":
					trade.To.Balance -= pair.Value;
					trade.From.Balance += pair.Value;
					break;
				case "Wood":
					trade.To.Wood -= pair.Value;
					trade.From.Wood += pair.Value;
					break;
				case "Ore":
					trade.To.Ore -= pair.Value;
					trade.From.Ore += pair.Value;
					break;
				case "Manpower":
					trade.To.SubtractManpower(pair.Value);
					trade.From.AddManpower(pair.Value);
					break;
			}
		}
		
		TradeSystem.Offers.Remove( id );
	}
	
	[Broadcast]
	public static void SendTrade(Guid id, string from, string to, Dictionary<string, int> offer, Dictionary<string, int> want)
	{
		TradeSystem.Offers.Add(id, new TradeOffer
		{
			Id = id,
			From = GameMap.Countries[from],
			To = GameMap.Countries[to],
			FromOffer = offer,
			ToOffer = want
		});
	}

	public bool IsFairTrade()
	{
		var offerValue = FromOffer.Sum( pair => (int)(ExchangeRates[pair.Key] * pair.Value) );
		var wantValue = ToOffer.Sum( pair => (int)(ExchangeRates[pair.Key] * pair.Value) );
		
		return !((float)wantValue / offerValue > 1.125);
	}
	
	public bool HasEnough()
	{
		var enough = true;
		foreach (var pair in FromOffer)
		{
			if ( pair.Key.Equals("Coins") && pair.Value > From.Balance )
			{
				enough = false;
				break;
			}
			
			if ( pair.Key.Equals("Wood") && pair.Value > From.Wood )
			{
				enough = false;
				break;
			}
			
			if ( pair.Key.Equals("Ore") && pair.Value > From.Ore )
			{
				enough = false;
				break;
			}
			
			if ( pair.Key.Equals("Manpower") && pair.Value > From.GetManpower() )
			{
				enough = false;
				break;
			}
		}
		
		foreach (var pair in ToOffer.TakeWhile(_ => enough))
		{
			if ( pair.Key.Equals("Coins") && pair.Value > To.Balance * 0.75 )
			{
				enough = false;
				break;
			}
			
			if ( pair.Key.Equals("Wood") && pair.Value > To.Wood * 0.75 )
			{
				enough = false;
				break;
			}
			
			if ( pair.Key.Equals("Ore") && pair.Value > To.Ore * 0.75 )
			{
				enough = false;
				break;
			}
			
			if ( pair.Key.Equals("Manpower") && pair.Value > To.GetManpower() * 0.75 )
			{
				enough = false;
				break;
			}
		}
		
		return enough;
	}
}
