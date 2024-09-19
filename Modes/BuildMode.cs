using AgeOfDiscord.UI;
using Sandbox.Exceptions;
using AgeOfDiscord.UI.Menu;
using Sandbox.GameData;
using Sandbox.Utils;

namespace Sandbox.Modes;

public class BuildMode : IMode
{
	public void OnDeselect( Scene scene )
	{
		var ui = scene.Components.GetInDescendants<GameUI>();
		var menus = ui.Panel.ChildrenOfType<HUD>().ToList();
		if ( menus.Count != 0 )
		{
			var menu = menus.First();
			var structureMenus = menu.ChildrenOfType<StructureMenu>().ToList();
			if ( structureMenus.Count != 0 )
			{
				structureMenus.First().Close();
			}
			
			var buildMenus = menu.ChildrenOfType<BuildMenu>().ToList();
			if ( buildMenus.Count != 0 )
			{
				buildMenus.First().Close();
			}
		}
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
		var mousePos = Mouse.Position;
		var ray = camera.ScreenPixelToRay(mousePos);
		var trace = scene.Trace.Ray( ray, 10000 ).Run();
		var collision = gameMap.GameObject.Components.Get<PlaneCollider>();

		if ( Settings.PlayableCountry.IsSpectator )
		{
			foreach (var country in GameMap.Countries.Values)
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.Text(country.Capital.Name, new Transform(collision.Transform.Local.PointToWorld( country.Capital.Province.Center )), "Vinque", 16F);
				Gizmo.Draw.Model(country.Capital.GetModel(), new Transform(collision.Transform.Local.PointToWorld( country.Capital.Province.Center ), new Rotation(), 0.3f) );
				
				foreach (var town in country.Towns.Where(town => !town.IsCapital()))
				{
					Gizmo.Draw.Text(town.Name, new Transform(collision.Transform.Local.PointToWorld( town.Province.Center )), "Vinque");
					Gizmo.Draw.Model(town.GetModel(), new Transform(collision.Transform.Local.PointToWorld( town.Province.Center ), new Rotation(), 0.3f));
				}
			}
		}
		else
		{
			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.Text(Settings.PlayableCountry.Capital.Name, new Transform(collision.Transform.Local.PointToWorld( Settings.PlayableCountry.Capital.Province.Center )), "Vinque", 16F);
			Gizmo.Draw.Model(Settings.PlayableCountry.Capital.GetModel(), new Transform(collision.Transform.Local.PointToWorld( Settings.PlayableCountry.Capital.Province.Center ), new Rotation(), 0.3f) );		
		
			foreach (var town in Settings.PlayableCountry.Towns.Where(town => !town.IsCapital()))
			{
				Gizmo.Draw.Text(town.Name, new Transform(collision.Transform.Local.PointToWorld( town.Province.Center )), "Vinque");
				Gizmo.Draw.Model(town.GetModel(), new Transform(collision.Transform.Local.PointToWorld( town.Province.Center ), new Rotation(), 0.3f));
			}
		}
		
		Gizmo.Draw.LineThickness = 4;
		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.LineCircle(trace.HitPosition, Vector3.Up, 4, 0, 360F, 32);

		if ( !trace.Hit )
		{
			return;
		}

		var p = collision.Transform.Local.PointToLocal( trace.HitPosition ); ;

		var width = gameMap.Width;
		var height = gameMap.Height;
		var remapArr = gameMap.RemapArr;
		
		var x = (int)MathF.Floor( width / 2F * (p.y/50)) + height / 2;
		var y = (int)MathF.Floor( width / 2F * (p.x/50)) + width / 2;
		// Rotate 90 degrees
		y = width - 1 - y;
			
		// Flip by Y
		y = height - y;
			
		if (remapArr.Length < x + y * width)
			return;

		var remapColor = remapArr[x + y * width];
		
		OnHover( scene, trace.HitPosition, new Vector2(remapColor.r, remapColor.g) );

		if(!gameMap.SelectAny || !gameMap.PrevColor.Equals(remapColor)){
			if(gameMap.SelectAny)
			{
				gameMap.ChangeColor(gameMap.PrevColor, gameMap.GetColor( gameMap.PrevColor, false ));	
			}
			
			gameMap.SelectAny = true;
			gameMap.PrevColor = remapColor;
			
			if (remapColor is { r: 0, g: 0 })
				return;
			
			if (GameMap.Provinces[new Vector2(remapColor.r, remapColor.g)].Country != Settings.PlayableCountry)
				return;
			
			gameMap.ChangeColor(remapColor, gameMap.GetColor(remapColor, true));
		}
	}
	
	public static void SelectProvince( GameMap gameMap, Province province )
	{
		if (province.Country != Settings.PlayableCountry)
			return;
		
		if ( GameMap.SelectedProvince != null )
		{
			var previousProvince = GameMap.SelectedProvince;
			GameMap.SelectedProvince = null;
			gameMap.PaletteTex.Update(
				gameMap.GetColor(
					new Color32( (byte)previousProvince.GetX(), (byte)previousProvince.GetY(), 0 ),
					false ), previousProvince.GetX(), previousProvince.GetY() );
		}

		GameMap.SelectedProvince = province;

		var color = new Color32( (byte)province.RemapCoordinates.x, (byte)province.RemapCoordinates.y, 0 );
		gameMap.ChangeColor( color,
			gameMap.GetColor( color, false ) );
	}

	public void OnHover(Scene scene, Vector3 hitPos, Vector2 remap)
	{
		var gameMap = scene.Components.GetInDescendants<GameMap>();
		var collision = gameMap.GameObject.Components.Get<PlaneCollider>();

		if ( BuildMenu.SelectedStructure != null )
		{
			if ( BuildMenu.SelectedStructure.OnlyForTown )
			{
				Gizmo.Draw.Color = Color.White.WithAlpha(0.5f);
				Gizmo.Draw.Model( BuildMenu.SelectedStructure.ModelName, new Transform(hitPos, new Rotation()));
			}
		}

		if ( GameMap.Provinces.TryGetValue( remap, out var value ) )
		{
			if (value.Country != Settings.PlayableCountry)
				return;
			
			if ( value.Structures.Count == 0 && value.Town == null && BuildMenu.SelectedStructure != null )
			{
				Gizmo.Draw.Color = Color.White.WithAlpha(0.5f);
				Gizmo.Draw.Model( BuildMenu.SelectedStructure.ModelName, new Transform(collision.Transform.Local.PointToWorld( value.Center ), new Rotation()));
			}
			
			if ( Input.Pressed( "Select" ) )
			{
				var ui = scene.Components.GetInDescendants<GameUI>();
				var menus = ui.Panel.ChildrenOfType<HUD>().ToList();
				
				if ( BuildMenu.SelectedStructure != null )
				{
					try
					{
						Settings.PlayableCountry.BuildStructure( BuildMenu.SelectedStructure?.Name, value );
						BuildMenu.SelectedStructure = null;
						Sound.Play("build");
						
						foreach (var color in GameMap.Provinces.Values.Select(province => new Color32( (byte)province.RemapCoordinates.x, (byte)province.RemapCoordinates.y, 0 )))
						{
							gameMap.ChangeColor(color, gameMap.GetColor(color, false));
						}
						
						if ( menus.Count != 0 )
						{
							var menu = menus.First();
							var structureMenus = menu.ChildrenOfType<StructureMenu>().ToList();

							if ( structureMenus.Count != 0 )
							{
								var structureMenu = structureMenus.First();
								structureMenu.Delete();
							}
							
							menu.AddChild( new StructureMenu() );
						}
					}
					catch ( Exception e )
					{
						Sound.Play( "decline" );
						Logger.ErrorSelf(gameMap, hitPos, e.Message);
					}	
				}
				else
				{
					SelectProvince( gameMap, value );
				}
				
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
	}
}
