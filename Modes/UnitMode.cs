using AgeOfDiscord.UI;
using AgeOfDiscord.UI.Menu;
using Sandbox.GameData;

namespace Sandbox.Modes;

public class UnitMode : IMode
{
	public static Army SelectedArmy;

	public void OnDeselect( Scene scene )
	{
		var ui = scene.Components.GetInDescendants<GameUI>();
		var menus = ui.Panel.ChildrenOfType<HUD>().ToList();
		if ( menus.Count != 0 )
		{
			var menu = menus.First();
			var armyMenus = menu.ChildrenOfType<ArmyMenu>().ToList();

			if ( armyMenus.Count != 0 )
			{
				armyMenus.First().Close();
			}
			
			var unitMenus = menu.ChildrenOfType<UnitMenu>().ToList();

			if ( unitMenus.Count != 0 )
			{
				unitMenus.First().Close();
			}
		}	
	}

	public void OnSelect( Scene scene )
	{
		SelectedArmy = GameMap.SelectedProvince?.Army;
		
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
		
		/*foreach (var province in GameMap.Provinces.Values)
		{
			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.Text( province.Center.ToString(), new Transform(collision.Transform.Local.PointToWorld( new Vector3(province.Center, 0) )));
		}*/

		if ( Settings.PlayableCountry.IsSpectator )
		{
			foreach (var army in GameMap.Countries.Values.SelectMany(country => country.Armies.Values))
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.Model( army.Config.ModelName, new Transform(collision.Transform.Local.PointToWorld( new Vector3(army.Province.Center, 0) )));
				
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.Text( army.Country.Name, new Transform(collision.Transform.Local.PointToWorld( new Vector3(army.Province.Center, 0) )));
			}
		}
		else
		{
			foreach (var army in Settings.PlayableCountry.Armies.Values)
			{
				Gizmo.Draw.Color = Color.Green.WithAlpha(0.4f);
				Gizmo.Draw.LineThickness = 4;
				Gizmo.Draw.LineCircle( collision.Transform.Local.PointToWorld( new Vector3(army.Province.Center, 0)), Vector3.Up, 24f, 0, 360, 64 );	
			
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.Model( army.Config.ModelName, new Transform(collision.Transform.Local.PointToWorld( new Vector3(army.Province.Center, 0) )));
			
				foreach (var province in army.Province.Neighbors.Select(neighbor => GameMap.Provinces[neighbor]))
				{
					if ( province.Army != null && province.Army.Country != Settings.PlayableCountry && province.Army.Country.Relations[Settings.PlayableCountry] != Relation.Ally)
					{
						Gizmo.Draw.Model( province.Army.Config.ModelName, new Transform(collision.Transform.Local.PointToWorld( new Vector3(province.Army.Province.Center, 0) )));
					}
				}
			}
		
			foreach (var army in Settings.PlayableCountry.Relations.Where( pair => pair.Value == Relation.Ally).Select(pair => pair.Key).SelectMany( country => country.Armies.Values ))
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.Model( army.Config.ModelName, new Transform(collision.Transform.Local.PointToWorld( new Vector3(army.Province.Center, 0) )));
			
				foreach (var province in army.Province.Neighbors.Select(neighbor => GameMap.Provinces[neighbor]))
				{
					if ( province.Army != null && province.Army.Country != army.Country && province.Army.Country != Settings.PlayableCountry && province.Army.Country.Relations[Settings.PlayableCountry] != Relation.Ally)
					{
						Gizmo.Draw.Model( province.Army.Config.ModelName, new Transform(collision.Transform.Local.PointToWorld( new Vector3(province.Army.Province.Center, 0) )));
					}
				}
			}
		}

		if ( SelectedArmy != null && SelectedArmy.Movement.IsMoving )
		{
			var startProvince = SelectedArmy.Province;
			foreach (var province in SelectedArmy.Movement.PathProvinces)
			{
				var start = collision.Transform.Local.PointToWorld( new Vector3( startProvince.Center, 0 ) ) + new Vector3(0, 0, 1);
				var end = collision.Transform.Local.PointToWorld( new Vector3( province.Center, 0 ) ) + new Vector3(0, 0, 1);
				
				Gizmo.Draw.LineThickness = 3;
				if ( SelectedArmy.Movement.PathProvinces.Last?.Value == province )
				{
					if ( SelectedArmy.Movement.CurrentProvince == startProvince )
					{
						Gizmo.Draw.Color = Color.White;
						Gizmo.Draw.Arrow(start.LerpTo( end, SelectedArmy.Movement.Progress / 100F ), end );
						
						Gizmo.Draw.Color = Color.Orange;
						Gizmo.Draw.Line( start, start.LerpTo( end, SelectedArmy.Movement.Progress / 100F ) );
					}
					else
					{
						Gizmo.Draw.Color = Color.White;
						Gizmo.Draw.Arrow(start, end );
					}

					startProvince = province;
				}
				else
				{
					if ( SelectedArmy.Movement.CurrentProvince == startProvince )
					{
						Gizmo.Draw.Color = Color.White;
						Gizmo.Draw.Line( start.LerpTo( end, SelectedArmy.Movement.Progress / 100F ), end );
						
						Gizmo.Draw.Color = Color.Orange;
						Gizmo.Draw.Line( start, start.LerpTo( end, SelectedArmy.Movement.Progress / 100F ) );
					}
					else
					{
						Gizmo.Draw.Color = Color.White;
						Gizmo.Draw.Line( start, end );
					}

					startProvince = province;
				}
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

		OnProvinceHover( scene, new Vector2( remapColor.r, remapColor.g ) );

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

	public static void SelectProvince( GameMap gameMap, Province province )
	{
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
		
		if (GameMap.SelectedProvince.Army != null && GameMap.SelectedProvince.Army.Country == Settings.PlayableCountry)
			SelectedArmy = GameMap.SelectedProvince.Army;
		else
			SelectedArmy = null;

		var color = new Color32( (byte)province.RemapCoordinates.x, (byte)province.RemapCoordinates.y, 0 );
		gameMap.ChangeColor( color,
			gameMap.GetColor( color, false ) );
		
		var ui = gameMap.Scene.Components.GetInDescendants<GameUI>();
		var menus = ui.Panel.ChildrenOfType<HUD>().ToList();
		if ( menus.Count != 0 )
		{
			var menu = menus.First();
			var armyMenus = menu.ChildrenOfType<ArmyMenu>().ToList();

			if ( armyMenus.Count != 0 )
			{
				var armyMenu = armyMenus.First();

				if ( SelectedArmy != null )
				{
					armyMenu.Army = SelectedArmy;
				}
				else
				{
					armyMenu.Close();
				}
			}
			else
			{
				if ( SelectedArmy != null )
				{
					menu.AddChild( new ArmyMenu
					{
						Army = SelectedArmy
					} );	
				}
			}
			
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
		}	
	}

	private bool _isCameraMoving;
	private TimeSince timeSinceClicked;
	public void OnProvinceHover( Scene scene, Vector2 remap )
	{
		var gameMap = scene.Components.GetInDescendants<GameMap>();
		if ( GameMap.Provinces.TryGetValue( remap, out var value ) )
		{
			var ui = scene.Components.GetInDescendants<GameUI>();
			var battles = ui.Panel.ChildrenOfType<Battles>().ToList();
			if ( battles.Count != 0 )
			{
				var menu = battles.First();
				var battle = menu.ChildrenOfType<BattleMenu>().ToList();
				
				if (battle.Count != 0)
					return;
			}

			if ( Input.Pressed( "Select" ) )
			{
				SelectProvince( gameMap, value );
			}
			
			if ( Input.Pressed( "Select2" ) )
			{
				timeSinceClicked = 0;
			}

			if ( Input.Down( "Select2" ) && timeSinceClicked > 0.15f )
			{
				_isCameraMoving = true;
			}

			if ( Input.Released( "Select2" ) && !_isCameraMoving)
			{
				if (SelectedArmy == null)
					return;

				if ( SelectedArmy.Province == value )
				{
					Army.EndWalk(SelectedArmy.Country.Name, SelectedArmy.Id);
					return;
				}
				
				if (Settings.PlayableCountry.IsSpectator)
					return;
				
				if (value.Country != Settings.PlayableCountry && value.Country.Relations[Settings.PlayableCountry] != Relation.War && value.Country.Relations[Settings.PlayableCountry] != Relation.Ally)
					return;
				
				if (value.Army is { CurrentBattle: not null })
					return;
				
				Army.MoveTo(gameMap, SelectedArmy.Country.Name, SelectedArmy.Id, SelectedArmy.Province.RemapCoordinates, value.RemapCoordinates);
			}
			
			if ( Input.Released( "Select2" ) && _isCameraMoving )
			{
				timeSinceClicked = 0;
				_isCameraMoving = false;
			}
		}
	}
}
