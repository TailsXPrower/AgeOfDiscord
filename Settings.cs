
using System.Text.Json.Nodes;
using Sandbox.Configs;
using Sandbox.GameData;

public sealed class Settings : Component
{
	public static Dictionary<string, ConfigStructure> Structures = new();
	public static Dictionary<string, ConfigUnit> Units = new();
	
	[Property] public GameMap GameMap;

	[Property]
	public string CountryName
	{
		get => PlayableCountry?.Name;
		set
		{
			if ( GameMap.Countries.TryGetValue( value, out var country ) )
			{
				PlayableCountry = country;
				var camera = Scene.Components.GetInDescendants<GameCamera>();
				camera.MoveToCountry();
			}
		}
	}

	public static Country PlayableCountry;

	protected override void OnUpdate()
	{
		
	}
}
