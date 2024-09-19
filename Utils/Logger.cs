using AgeOfDiscord.UI;
using AgeOfDiscord.UI.Menu;

namespace Sandbox.Utils;

public class Logger
{
	[Broadcast]
	public static void Error(GameMap map, Vector3 pos, string message)
	{
		var startPos = pos;
		
		if (map == null)
			return;
		
		Animation.Play( map.GameObject, message, 2f, EasingFunc.Linear, (obj, progress) =>
		{
			var alpha = 1f;

			if ( progress < 0.2f )
			{
				alpha = progress / 0.2f;
			}
						
			if ( progress > 0.8f )
			{
				alpha = 1 - (progress - 0.8f) / 0.2f;
			}

			startPos.z += 0.5f;

			try
			{
				Gizmo.Draw.Color = Color.White.WithAlpha(alpha);
				Gizmo.Draw.Text(message, new Transform(startPos));
			}
			catch ( NullReferenceException )
			{ }
		} );
	}
	
	public static void ErrorSelf(GameMap map, Vector3 pos, string message)
	{
		var startPos = pos;
		
		if (map == null)
			return;
		
		Animation.Play( map.GameObject, message, 2f, EasingFunc.Linear, (obj, progress) =>
		{
			var alpha = 1f;

			if ( progress < 0.2f )
			{
				alpha = progress / 0.2f;
			}
						
			if ( progress > 0.8f )
			{
				alpha = 1 - (progress - 0.8f) / 0.2f;
			}

			startPos.z += 0.5f;
					
			Gizmo.Draw.Color = Color.White.WithAlpha(alpha);
			Gizmo.Draw.Text(message, new Transform(startPos));
		} );
	}

	[Broadcast]
	public static void Notify( GameUI gameUi, string message, string confirm = "Okay" )
	{
		if (gameUi == null)
			return;
		
		var notification = new Notification
		{
			Message = message,
			Accept = confirm
		};
		gameUi.Panel.AddChild( notification );
	}
	
	public static void NotifySelf( GameUI gameUi, string message, string confirm = "Okay" )
	{
		if (gameUi == null)
			return;
		
		var notification = new Notification
		{
			Message = message,
			Accept = confirm
		};
		gameUi.Panel.AddChild( notification );
	}
	
	public static void EndGame( Scene scene, string message)
	{
		var time = scene.Components.GetInDescendants<TimeComponent>();
		var gameUi = scene.Components.GetInDescendants<GameUI>();
		
		if (gameUi == null)
			return;
		
		if (time == null)
			return;
		
		time.Pause = true;
		
		var endGame = new EndGame
		{
			Message = message
		};
		gameUi.Panel.AddChild( endGame );
	}
}
