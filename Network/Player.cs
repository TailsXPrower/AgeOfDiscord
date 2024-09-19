using Sandbox.Network;
using System;
using Menu;
using Sandbox.GameData;

namespace Sandbox;

[Title( "Player" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class Player : Component
{
	[Sync] [Property]
	public string Country { get; set; }

	protected override void OnUpdate()
	{
		base.OnUpdate();

		var time = Scene.Components.GetInDescendants<TimeComponent>();
		
		if (time == null)
			return;
		
		if (!time.IsHourTicked())
			return;
		
		if (SettingsMenu.Debug)
			Log.Info($"Player {Network.OwnerConnection.DisplayName} is {Country}");
	}

	public void Load(string countryName)
	{
		if ( countryName != null )
		{
			Country = countryName;
		}
		else
		{
			Country = CountryInfo.Info.Name == null ? "The Kingdom of Astacks" : CountryInfo.Info.Name;
		}

		var settings = Scene.Components.GetInDescendants<Settings>();
		settings.CountryName = Country;
	}
}
