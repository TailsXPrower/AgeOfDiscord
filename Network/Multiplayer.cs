using Sandbox.Network;
using System;
using System.Threading.Tasks;
using Menu;
using Sandbox.GameData;

namespace Sandbox;

[Title( "Multiplayer" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class Multiplayer : Component, Component.INetworkListener
{
	public Dictionary<string, Guid> StartArmies { get; set; } = new();
	
	protected override async Task OnLoad()
	{
		if ( !GameNetworkSystem.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby...";
			await Task.DelayRealtimeSeconds( 0.1f );
			GameNetworkSystem.CreateLobby();
		}
	}
	
	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive( Connection channel )
	{
		if ( !LobbyMenu.SelectedCountries.ContainsKey(channel.Id) && LobbyMenu.GameStarted )
		{
			using ( Rpc.FilterInclude( channel ) )
			{
				Kick();
				return;
			}
		}
		
		Log.Info( $"Player '{channel.DisplayName}' has joined the game" );

		var go = Scene.CreateObject();
		var player = go.Components.Create<Player>();
		go.Name = channel.DisplayName;
		go.NetworkSpawn( channel );

		if ( StartArmies.Count == 0 )
		{
			foreach ( var country in GameMap.Countries.Values )
			{
				StartArmies.Add( country.Name, Guid.NewGuid() );
			}
		}

		using ( Rpc.FilterInclude( channel ) )
		{
			string countryName = null;
			if ( LobbyMenu.RandomCountries )
			{
				var random = new Random();
				var countries = GameMap.Countries.Values.Where( country => country.IsPlayer && country.GetPlayer( Scene ) == null ).ToList();
				countryName = countries[random.Next(countries.Count)].Name;
			}
			
			LoadPlayer(player, LobbyMenu.Introduction, LobbyMenu.RandomCountries, StartArmies, countryName);
		}
	}

	public void OnDisconnected( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has left the game" );
		Unloaded(channel);
	}
	
	[Broadcast]
	public void Kick()
	{
		GameNetworkSystem.Disconnect();
		Scene.LoadFromFile( "Scenes/mainmenu.scene" );
	}

	[Broadcast]
	public void LoadPlayer(Player player, bool intro, bool random, Dictionary<string, Guid> startArmies, string countryName = null)
	{
		player.Load(countryName);
		LobbyMenu.Introduction = intro;
		LobbyMenu.RandomCountries = random;
		LobbyMenu.GameStarted = true;
		
		foreach (var pair in startArmies)
		{
			var country = GameMap.Countries[pair.Key];
			var army = new Army( pair.Value, country.GetEliteUnit(), country, country.Capital.Province );
			country.Armies.Add(army.Id, army);
		}
	}
	
	[Broadcast]
	public void Unloaded(Connection channel)
	{
		try
		{
			var player = Scene.GetAllComponents<Player>().First( player => player.Network.OwnerId == channel.Id );
			player.Destroy();
		} catch (InvalidOperationException) {}
	}
}
