using Sandbox.GameData;

namespace Sandbox.Events;

public class UnitBattleSystem : Component
{
	protected override void OnFixedUpdate()
	{
		if (Scene.Components.GetInDescendants<Settings>() == null)
			return;
		
		var time = Scene.Components.GetInDescendants<TimeComponent>();
		
		if (!time.IsDayTicked())
			return;
		
		if (IsProxy)
			return;
		
		foreach (var battle in new List<Battle>(GameMap.Battles.Values))
		{
			battle.Progress(Scene);
		}
	}
}
