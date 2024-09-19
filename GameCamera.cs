using Sandbox;

public sealed class GameCamera : Component
{
	[Property] public GameMap GameMap;
	
	private const int Step = 100;
	private float MouseVelocity = 1;

	protected override void OnUpdate()
	{
		var camera = GameObject.Components.Get<CameraComponent>();
		
		//UpdateTemperature( camera );
		
		var mousePos = Mouse.Position;
		var ray = camera.ScreenPixelToRay(mousePos);
		var trace = Scene.Trace.Ray( ray, 10000 ).Run();
		
		camera.Transform.Position += trace.Direction * Input.MouseWheel.y * Step;
		camera.Transform.Position = camera.Transform.Position.WithZ( camera.Transform.Position.z.Clamp( 300, 1800 ) );

		var rotation = camera.Transform.Rotation;
		if ( camera.Transform.Position.z < 500 )
		{
			var angles = rotation.Angles();
			angles.pitch = 60 - 15 * (1 - (camera.Transform.Position.z - 400) / 200);
			camera.Transform.Rotation = angles.ToRotation();
		}
		else
		{
			var angles = rotation.Angles();
			angles.pitch = 60;
			camera.Transform.Rotation = angles.ToRotation();
		}
		
		MouseVelocity = 0.5f + (1800 - camera.Transform.Position.z) / 1800 * 4;
		
		if(Input.Down( "Select2" ))
		{
			Mouse.CursorType = "pointer";
			
			if (Mouse.Delta.AlmostEqual(0))
				return;
			
			camera.Transform.Position += new Vector3(-Mouse.Velocity.y/MouseVelocity, -Mouse.Velocity.x/MouseVelocity);
		}
		else
		{
			Mouse.CursorType = "auto";
		}
		
		var renderer = GameMap.GameObject.Components.Get<ModelRenderer>();
		camera.Transform.Position = new Vector3( camera.Transform.Position.x.Clamp(renderer.Bounds.Maxs.x + 400, renderer.Bounds.Mins.x + 400), 
			camera.Transform.Position.y.Clamp(renderer.Bounds.Maxs.y, renderer.Bounds.Mins.y),
			camera.Transform.Position.z );

		Mouse.Visible = true;
	}

	public void MoveToCountry()
	{
		var camera = GameObject.Components.Get<CameraComponent>();
		var collider = GameMap.GameObject.Components.Get<PlaneCollider>();
		var renderer = GameMap.GameObject.Components.Get<ModelRenderer>();
		camera.Transform.Position = collider.Transform.Local.PointToWorld(Settings.PlayableCountry.GetCenter()).WithZ(1200);
		camera.Transform.Position += new Vector3( 700, 0, 0 );
		camera.Transform.Position = new Vector3( camera.Transform.Position.x.Clamp(renderer.Bounds.Maxs.x + 400, renderer.Bounds.Mins.x + 400), 
			camera.Transform.Position.y.Clamp(renderer.Bounds.Maxs.y, renderer.Bounds.Mins.y),
			camera.Transform.Position.z );
	}

	void UpdateTemperature(CameraComponent camera)
	{
		var colorGrading = camera.GameObject.Components.Get<ColorGrading>();
		var time = Scene.Components.GetInDescendants<TimeComponent>();
		var date = time.GetDate();
		if ( date.Hour < 6 )
		{
			if ( date.Hour == 5 )
			{
				colorGrading.ColorTempK = MathX.Lerp( 40000, 2300, date.Minute / 60f  );
			}
			else
			{
				colorGrading.ColorTempK = 40000;	
			}
		} else if ( date.Hour < 12 )
		{
			if ( date.Hour == 11 )
			{
				colorGrading.ColorTempK = MathX.Lerp( 2300, 6500, date.Minute / 60f  );
			}
			else
			{
				colorGrading.ColorTempK = 2300;	
			}
		} else if ( date.Hour < 19 )
		{
			if ( date.Hour == 18 )
			{
				colorGrading.ColorTempK = MathX.Lerp( 6500, 2300, date.Minute / 60f  );
			}
			else
			{
				colorGrading.ColorTempK = 6500;	
			}
		} else if ( date.Hour < 22 )
		{
			if ( date.Hour == 21 )
			{
				colorGrading.ColorTempK = MathX.Lerp( 2300, 40000, date.Minute / 60f  );
			}
			else
			{
				colorGrading.ColorTempK = 2300;	
			}
		} else if ( date.Hour < 24 )
		{
			colorGrading.ColorTempK = 40000;
		}
	}
}
