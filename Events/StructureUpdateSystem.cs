using Sandbox.GameData;

namespace Sandbox.Events;

public class StructureUpdateSystem : Component
{
	protected override void OnFixedUpdate()
	{
		var time = Scene.Components.GetInDescendants<TimeComponent>();

		if (IsProxy)
			return;
		
		if ( time.IsMonthTicked() )
		{
			foreach ( var country in GameMap.Countries.Values )
			{
				Country.OnMonthTick(country.Name);
			}
		}
		
		if ( time.IsWeekTicked() )
		{
			foreach ( var country in GameMap.Countries.Values )
			{
				Country.OnTick(country.Name);
			}
		}
		
		if (!time.IsHourTicked())
			return;
		
		foreach (var structure in GameMap.GetStructures())
		{
			if (structure.IsCompleted)
				continue;

			UpdateStructure(structure.Id, structure.Province.RemapCoordinates);
		}
	}

	[Broadcast]
	public static void UpdateStructure(Guid id, Vector2 coords)
	{
		var province = GameMap.Provinces[coords];
		var structure = province.Structures[id];
		
		structure.BuiltHammers += structure.Province.GetManpower();

		if ( structure.BuiltHammers > structure.Config.HammerCost )
		{
			structure.SetCompleted(true);
		}
	}
}
