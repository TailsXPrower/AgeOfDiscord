namespace Sandbox.Events;

public class UnitMoveSystem : Component
{
	protected override void OnFixedUpdate()
	{
		if (Scene.Components.GetInDescendants<Settings>() == null)
			return;
		
		var time = Scene.Components.GetInDescendants<TimeComponent>();
		
		if (!time.IsHourTicked())
			return;
		
		if (IsProxy)
			return;
		
		foreach (var movement in GameMap.Countries.Values.SelectMany( country => country.Armies.Values ).Select(army => army.Movement))
		{
			if (!movement.IsMoving)
				continue;
			
			UpdateArmy(Scene.Components.GetInDescendants<GameMap>(), movement.Army.Country.Name, movement.Army.Id);
		}
	}
	
	[Broadcast]
	private static void UpdateArmy(GameMap gameMap, string countryName, Guid id)
	{
		var country = GameMap.Countries[countryName];
		var army = country.Armies[id];
		
		army.Movement.Progress += army.Movement.Speed;

		if ( army.Movement.Progress >= 100 )
		{
			army.Movement.Check(gameMap);
		}
	}
}
