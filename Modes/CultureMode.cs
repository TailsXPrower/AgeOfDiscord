using System;
using AgeOfDiscord.UI;
using AgeOfDiscord.UI.Menu;
using Sandbox.UI;

namespace Sandbox.Modes;

public class CultureMode : IMode
{
	public void OnDeselect( Scene scene )
	{
		
	}

	public void OnSelect( Scene scene )
	{
		var gameMap = scene.Components.GetInDescendants<GameMap>();

		foreach (var color in GameMap.Provinces.Values.Select(province => new Color32( (byte)province.RemapCoordinates.x, (byte)province.RemapCoordinates.y, 0 )))
		{
			gameMap.ChangeColor(color, gameMap.GetColor(color, false));
		}
	}

	public void Process(Scene scene)
	{
		var gameMap = scene.Components.GetInDescendants<GameMap>();
		var camera = scene.Components.GetInDescendants<CameraComponent>();
		var collision = gameMap.GameObject.Components.Get<PlaneCollider>();
		
		foreach (var country in GameMap.Countries.Values)
		{
			Gizmo.Draw.Color = Color.White.WithAlpha(camera.Transform.Position.z > 1200 ? 1 - (camera.Transform.Position.z - 1200) / 200 : 1);
			var pos = collision.Transform.Local.PointToWorld( country.GetCenter() );
			Gizmo.Draw.WorldText(country.Name, new Transform(pos.WithZ( pos.z + 10 ), new Angles(0, 90, 0)), "Vinque", MathF.Min(40F, MathF.Max(24F,country.Provinces.Count * 4F)));
		}
		
		foreach (var culture in GameMap.Cultures.Values)
		{
			Gizmo.Draw.Color = Color.White.WithAlpha(camera.Transform.Position.z < 1300 ? (camera.Transform.Position.z - 1100) / 200 : 1);
			var pos = collision.Transform.Local.PointToWorld( culture.GetCenter() );
			Gizmo.Draw.WorldText(culture.Name, new Transform(pos.WithZ( pos.z + 10 ), new Angles(0, 90, 0)), "Vinque", 52F);	
		}
		
		CheckHover(scene, camera);
	}

	private void CheckHover(Scene scene, CameraComponent camera)
	{
		var gameMap = scene.Components.GetInDescendants<GameMap>();
		var mousePos = Mouse.Position;
		var ray = camera.ScreenPixelToRay(mousePos);
		var trace = scene.Trace.Ray( ray, 10000 ).Run();
		var collision = gameMap.GameObject.Components.Get<PlaneCollider>();
		
		Gizmo.Draw.LineThickness = 4;
		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.LineCircle(trace.HitPosition, Vector3.Up, 4, 0, 360F, 32);

		if ( !trace.Hit )
		{
			return;
		}

		var p = collision.Transform.Local.PointToLocal( trace.HitPosition ); ;
			
		var x = (int)MathF.Floor( gameMap.Width / 2F * (p.y/50)) + gameMap.Height / 2;
		var y = (int)MathF.Floor( gameMap.Width / 2F * (p.x/50)) + gameMap.Width / 2;
		// Rotate 90 degrees
		y = gameMap.Width - 1 - y;
			
		// Flip by Y
		y = gameMap.Height - y;
			
		if (gameMap.RemapArr.Length < x + y * gameMap.Width)
			return;

		var remapColor = gameMap.RemapArr[x + y * gameMap.Width];
		
		OnProvinceHover( scene, new Vector2(remapColor.r, remapColor.g), new Vector2(p.x, p.y) );

		if(!gameMap.SelectAny || !gameMap.PrevColor.Equals(remapColor)){
			if(gameMap.SelectAny)
			{
				gameMap.ChangeColor(gameMap.PrevColor, gameMap.GetColor( gameMap.PrevColor, false ));	
			}
			
			gameMap.SelectAny = true;
			gameMap.PrevColor = remapColor;
			
			if (remapColor is { r: 0, g: 0 })
				return;
			
			gameMap.ChangeColor(remapColor, gameMap.GetColor(remapColor, true));
		}
	}
	
	public void OnProvinceHover( Scene scene, Vector2 remap, Vector2 pos )
	{
		var gameMap = scene.Components.GetInDescendants<GameMap>();
		Gizmo.Draw.Color = Color.White;

		if ( Input.Pressed( "Select" ) )
		{
			if ( GameMap.Provinces.TryGetValue( remap, out var value ) )
			{
				if ( GameMap.SelectedProvince != null )
				{
					var previousProvince = GameMap.SelectedProvince;
					GameMap.SelectedProvince = null;
					gameMap.PaletteTex.Update(
						gameMap.GetColor( new Color32( (byte)previousProvince.GetX(), (byte)previousProvince.GetY(), 0 ),
							false ), previousProvince.GetX(), previousProvince.GetY() );
				}

				GameMap.SelectedProvince = value;

				gameMap.ChangeColor( new Color32( (byte)remap.x, (byte)remap.y, 0 ), gameMap.GetColor( gameMap.PrevColor, false ) );
			}
			
			var ui = scene.Components.GetInDescendants<GameUI>();
			var menus = ui.Panel.ChildrenOfType<HUD>().ToList();
			if ( menus.Count != 0 )
			{
				var menu = menus.First();
				var provinceMenus = menu.ChildrenOfType<ProvinceMenu>().ToList();

				if ( provinceMenus.Count != 0 )
				{
					var provinceMenu = provinceMenus.First();

					if ( GameMap.SelectedProvince != null )
					{
						provinceMenu.Province = GameMap.SelectedProvince;
					}
					else
					{
						provinceMenu.Close();
					}
				}
				else
				{
					if ( GameMap.SelectedProvince != null )
					{
						menu.AddChild( new ProvinceMenu
						{
							Province = GameMap.SelectedProvince
						} );	
					}
				}
			}
		}
	}
	
	public static void DeselectProvince(Scene scene)
	{
		if ( GameMap.SelectedProvince == null )
			return;

		GameMap.SelectedProvince = null;
		var gameMap = scene.Components.GetInDescendants<GameMap>();
		foreach ( var countryProvince in GameMap.Provinces.Values )
		{
			gameMap.PaletteTex.Update(
				gameMap.GetColor( new Color32( (byte)countryProvince.GetX(), (byte)countryProvince.GetY(), 0 ),
					false ), countryProvince.GetX(), countryProvince.GetY() );
		}
	}
}
