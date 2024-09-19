using AgeOfDiscord.UI;
using Sandbox.Configs;
using Sandbox.Exceptions;
using Sandbox.Modes;

namespace Sandbox.GameData;

public class Structure
{
	public Guid Id;
	
	public readonly ConfigStructure Config;
	public readonly Province Province;

	public long BuiltHammers = 0;
	public bool IsCompleted; 

	public Structure( string config, Province province )
	{
		Config = Settings.Structures[config];
		Province = province;
	}

	public void OnTick()
	{
		switch ( GetName() )
		{
			case "Farm":
				if ( Config.EffectiveBiomes.Contains( Province.Biome.Name ) )
				{
					Province.Country.AddManpower( 200 );
				}
				else
				{
					Province.Country.AddManpower( 100 );
				}
				break;
			case "Mine":
				if ( Config.EffectiveBiomes.Contains( Province.Biome.Name ) )
				{
					Province.Country.Ore += 10;
				}
				else
				{
					Province.Country.Ore += 2;
				}
				break;
			case "Sawmill":
				if ( Config.EffectiveBiomes.Contains( Province.Biome.Name ) )
				{
					Province.Country.Wood += 10;
				}
				else
				{
					Province.Country.Wood += 2;
				}
				break;
		}
	}

	public void OnUpdate(GameMap gameMap)
	{
		if (Config.OnlyForTown || Config.TownHall)
			return;

		var gameUi = gameMap.Scene.Components.GetInDescendants<GameUI>();
		var collision = gameMap.GameObject.Components.Get<PlaneCollider>();

		if ( gameUi.CurrentMode == IMode.BuildMode && (Province.Country == Settings.PlayableCountry || Settings.PlayableCountry.IsSpectator) )
		{
			if ( IsCompleted )
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.Model( Config.ModelName, new Transform(collision.Transform.Local.PointToWorld( new Vector3(Province.Center, 0) ), new Rotation(), 0.8f));
			}
			else
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.Model( "models/structures/building.vmdl", new Transform(collision.Transform.Local.PointToWorld( new Vector3(Province.Center, 0) ), new Rotation(), 0.8f));	
			}
		}
	}

	public bool IsActive()
	{
		return IsCompleted;
	}
	
	[Broadcast]
	public static void Delete(Guid id, Vector2 coords)
	{
		var province = GameMap.Provinces[coords];
		if (province.Structures.Remove( id, out var structure ))
		{
			province.Country.Wood += structure.Config.WoodCost / 2;
		}
	}

	public void SetCompleted( bool completed )
	{
		IsCompleted = completed;
	}

	public string GetName() => Config.Name;
	public string GetDescription() => Config.Description;
}
