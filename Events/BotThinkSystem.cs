namespace Sandbox.Events;

public class BotThinkSystem : Component
{
	protected override void OnFixedUpdate()
	{
		var time = Scene.Components.GetInDescendants<TimeComponent>();
		
		if (!time.IsHourTicked())
			return;
		
		if (IsProxy)
			return;
		
		foreach (var country in GameMap.Countries.Values.Where(country => country.IsBot( Scene )))
		{
			country.Controller.Think(Scene);
		}
	}
}
