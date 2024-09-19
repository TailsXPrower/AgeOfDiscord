using Sandbox.Configs;
using Sandbox.Modes;
using Sandbox.Utils;

namespace Sandbox.GameData;

public class Army
{
	public Guid Id;
	public readonly ConfigUnit Config;
	public Province Province;
	public Country Country;
	
	public Battle CurrentBattle;

	public string Name = "Army";
	public float Health = 100;

	public int MaxUnits = 30;
	public readonly List<Unit> Units = new();

	public ArmyMovement Movement;

	public Army( Guid id, string config, Country country, Province province )
	{
		Id = id;
		Config = Settings.Units[config];
		Province = province;
		province.Army = this;
		Country = country;

		Name = $"Army {Country.Armies.Count}";

		Movement = new ArmyMovement( this );

		_ = new Unit( config, this );
	}
	
	[Broadcast]
	public static void NewArmy( string countryName, Guid id, Vector2 coords, bool free = false )
	{
		var country = GameMap.Countries[countryName];
		var province = GameMap.Provinces[coords];
		
		var army = new Army( id, country.GetEliteUnit(), country, province );

		if ( !free )
		{
			country.Ore -= army.Config.OreCost;
		}
		
		country.Armies.Add(army.Id, army);
	}
	
	[Broadcast]
	public static void NewUnit( string countryName, Guid id, string configName )
	{
		var country = GameMap.Countries[countryName];

		if ( !country.Armies.TryGetValue(id, out var army) )
		{
			return;
		}

		_ = new Unit( configName, army );
	}

	public void Delete()
	{
		Province.Army = null;
		Country.Armies.Remove( Id );
		
		var ore = Units.Sum( unit => unit.Config.OreCost * (unit.Health / unit.Config.MaxHealth));
		var manpower = Units.Sum( unit => unit.Config.ManpowerCost * (unit.Health / unit.Config.MaxHealth));

		Country.Ore += ore / 2;
		Country.AddManpower( manpower );
	}
	
	[Broadcast]
	public static void Heal( string countryName, Guid id, float health )
	{
		var country = GameMap.Countries[countryName];
		if (!country.Armies.ContainsKey(id))
			return;
		country.Armies[id].Heal(health);
	}
	
	public void Heal( float health )
	{
		var remainingHealth = (int)health;
		foreach (var unit in GetUnits().OrderBy( unit => unit.Health ))
		{
			if (unit.Health == unit.Config.MaxHealth)
				break;
			
			var healed = Math.Min(remainingHealth, unit.Config.MaxHealth - unit.Health);
			unit.Health += healed;
			remainingHealth -= healed;
		}

		var maxHealth = Units.Sum( unit => unit.Config.MaxHealth );
		var currentHealth = Units.Sum( unit => unit.Health );
		Health = currentHealth / (float)maxHealth * 100;
	}

	[Broadcast]
	public static void Damage( string countryName, Guid id, float damage )
	{
		var country = GameMap.Countries[countryName];
		country.Armies[id].Damage(damage);
	}

	public void Damage( float damage )
	{
		var remainingDamage = (int)damage;
		foreach (var unit in GetUnits().Where(unit => unit.Health > 0))
		{
			if (remainingDamage == 0)
				break;
			
			var damaged = Math.Min(remainingDamage, unit.Health);
			unit.Health -= damaged;
			remainingDamage -= damaged;
		}

		var maxHealth = Units.Sum( _ => 100 );
		var currentHealth = Units.Sum( unit => unit.Health );
		Health = currentHealth / (float)maxHealth * 100;
		
		if (Health == 0)
			Delete();
	}

	public List<Unit> GetUnits()
	{
		return Units.OrderBy( unit => unit.Config.Name.Equals(Country.GetEliteUnit()) ).ThenBy( unit => unit.Config.Name is "Wizard" ).ToList();
	}
	
	public float GetHealth()
	{
		return Health;
	}
	
	public int GetDamage()
	{
		return Units.Sum( unit => unit.Health > 0 ? unit.Config.Damage : 0 );
	}
	
	public int GetDefense()
	{
		return Units.Sum( unit => unit.Health > 0 ? unit.Config.Defense : 0 );
	}

	public int GetMaxUnits()
	{
		return MaxUnits + Country.CountStructure( "Well" );
	}
	
	[Broadcast]
	public static void MoveTo(GameMap gameMap, string countryName, Guid id, Vector2 coordsFrom, Vector2 coordsTo)
	{
		var from = GameMap.Provinces[coordsFrom];
		var to = GameMap.Provinces[coordsTo];
		var country = GameMap.Countries[countryName];

		if ( !country.Armies.TryGetValue(id, out var army) )
		{
			return;
		}
		
		if (gameMap == null)
			return;
		
		var movement = army.Movement;
			
		if (movement.IsMoving)
			movement.EndWalk();

		if ( movement.Army.CurrentBattle != null )
		{
			movement.EndWalk();
			Battle.BattleEnd( gameMap, movement.Army.CurrentBattle.Id );
		}
			
		movement.StartingProvince = from;
		movement.CurrentProvince = from;
		movement.DestinationProvince = to;
			
		movement.BuildPath(movement.StartingProvince);
		movement.CalculateSpeed();

		if ( !movement.PathProvinces.Contains(movement.DestinationProvince) )
		{
			if ( !movement.Army.Country.IsBot( gameMap.Scene ) )
			{
				if ( !movement.Army.Country.GetPlayer( gameMap.Scene ).IsProxy )
				{
					var collision = gameMap.GameObject.Components.Get<PlaneCollider>();
					using (Rpc.FilterInclude(movement.Army.Country.GetPlayer( gameMap.Scene ).Network.OwnerConnection))
						Logger.Error(gameMap, collision.Transform.Local.PointToWorld(movement.DestinationProvince.Center), "We cannot get here!");	
				}
				movement.EndWalk();
				return;
			}

			if ( movement.PathProvinces.First is { Value: not null })
			{
				MoveTo(gameMap, countryName, id, movement.CurrentProvince.RemapCoordinates, movement.PathProvinces.First.Value.RemapCoordinates);
				return;
			}
				
			movement.EndWalk();
			return;
		}

		movement.IsMoving = true;
	}
	
	[Broadcast]
	public static void EndWalk(string countryName, Guid armyId)
	{
		var country = GameMap.Countries[countryName];
		
		if (!country.Armies.TryGetValue(armyId, out var army))
			return;
		
		var movement = army.Movement;
		movement.StartingProvince = null;
		movement.CurrentProvince = null;
		movement.DestinationProvince = null;
		movement.IsMoving = false;
		movement.Progress = 0;
		movement.PathProvinces.Clear();
	}

	public class Unit
	{
		public readonly ConfigUnit Config;
		public int Health = 100;
		
		public Unit( string config, Army army )
		{
			Config = Settings.Units[config];

			if ( !config.Equals(army.Country.GetEliteUnit()) && army.Country.Ore < Config.OreCost )
			{
				Sound.Play( "decline" );
				return;
			}
			
			if ( !config.Equals(army.Country.GetEliteUnit()) && army.Country.GetManpower() < Config.ManpowerCost )
			{
				Sound.Play( "decline" );
				return;
			}
			
			if ( army.Units.Count >= army.GetMaxUnits() )
			{
				Sound.Play( "decline" );
				return;
			}

			if ( !config.Equals( army.Country.GetEliteUnit() ) )
			{
				army.Country.Ore -= Config.OreCost;
				army.Country.SubtractManpower(Config.ManpowerCost);
			}
			
			army.Units.Add(this);
		}
	}

	public class ArmyMovement
	{
		public Province StartingProvince;
		public Province CurrentProvince;
		public Province DestinationProvince;

		public bool IsMoving;
		public int Progress = 0;
		public int Speed;

		public readonly LinkedList<Province> PathProvinces = new();
		
		public readonly Army Army;

		public ArmyMovement( Army army )
		{
			Army = army;
		}
		
		public void Check(GameMap gameMap)
		{
			if ( Army.CurrentBattle != null )
			{
				EndWalk();
			}
			
			Progress = 0; 
			var province = PathProvinces.First;

			if ( province?.Value == null )
			{
				EndWalk();
				return;
			}
			
			if ( province.Value.Army != null && (province.Value.Army.Country == Army.Country || province.Value.Army.Country.Relations[Army.Country] != Relation.War) )
			{
				if (Networking.IsHost)
				{
					MoveTo(gameMap, Army.Country.Name, Army.Id, CurrentProvince.RemapCoordinates, DestinationProvince.RemapCoordinates );
				}
				return;
			}
			
			switch (province.Value.Army)
			{
				case { CurrentBattle: not null } when DestinationProvince == province.Value:
					EndWalk();
					return;
				case { CurrentBattle: not null } when DestinationProvince != province.Value:
					if (Networking.IsHost)
					{
						MoveTo(gameMap, Army.Country.Name, Army.Id, CurrentProvince.RemapCoordinates, DestinationProvince.RemapCoordinates );
					}
					return;
			}

			if (province.Value.Army != null && province.Value.Army.Country.Relations[Army.Country] == Relation.War)
			{
				if (Networking.IsHost)
				{
					Battle.NewBattle(Guid.NewGuid(), Army.Country.Name, Army.Id, province.Value.Army.Country.Name, province.Value.Army.Id);
				}
				IsMoving = false;
				return;
			}
			
			if (Army.Country != province.Value.Country && province.Value.Country.Relations.TryGetValue( Army.Country, out var value ) && value == Relation.War )
			{
				if ( Army.Country.GetAllyCountries()
				    .Any( country => country.Controller.OurProvinces.Contains( province.Value ) ) )
				{
					foreach (var allyCountry in Army.Country.GetAllyCountries().Where(allyCountry => allyCountry.Controller.OurProvinces.Contains( province.Value )))
					{
						allyCountry.ClaimProvince( gameMap, province.Value.RemapCoordinates);
						break;
					}
				}
				else
				{
					Army.Country.ClaimProvince( gameMap, province.Value.RemapCoordinates);
				}
			}
			
			CurrentProvince.Army = null;
			CurrentProvince = province?.Value;
			Army.Province = CurrentProvince;

			if ( CurrentProvince != null )
			{
				CurrentProvince.Army = Army;
				
				if ( !Army.Country.IsBot(gameMap.Scene) )
				{
					if (!Army.Country.GetPlayer( gameMap.Scene ).IsProxy && UnitMode.SelectedArmy == Army)
					{
						UnitMode.SelectProvince( gameMap, CurrentProvince );	
					}
				}
			}

			if ( CurrentProvince == DestinationProvince )
			{
				EndWalk();
				return;
			}
			
			PathProvinces.RemoveFirst();
			CalculateSpeed();

			if ( PathProvinces.First?.Value.Army != null )
			{
				if (Networking.IsHost)
				{
					MoveTo(gameMap, Army.Country.Name, Army.Id, CurrentProvince.RemapCoordinates, DestinationProvince.RemapCoordinates );
				}
			}
		}

		public void BuildPath( Province province )
		{
			while ( true )
			{
				if ( province == null ) return;
				
				// Check for destination in Neighbors
				foreach ( var coords in province.Neighbors.Where( coords => DestinationProvince.RemapCoordinates == coords ) )
				{
					PathProvinces.AddLast( GameMap.Provinces[coords] );
					return;
				}

				Province possibleProvince = null;
				
				var distance = 100000f;
				foreach ( var coords in province.Neighbors )
				{
					var neighborProvince = GameMap.Provinces[coords];
					
					if (neighborProvince.Army != null && (!neighborProvince.Country.Relations.TryGetValue(Army.Country, out var relation) || relation != Relation.War))
						continue;

					if ( Army.Country.Relations.Values.Any( value => value == Relation.War ) || DestinationProvince.Country != Army.Country )
					{
						if (neighborProvince.Country != Army.Country && neighborProvince.Country.Relations[Army.Country] != Relation.War && neighborProvince.Country.Relations[Army.Country] != Relation.Ally)
							continue;
					}
					
					if ( neighborProvince.Center.Distance( DestinationProvince.Center ) < distance )
					{
						possibleProvince = GameMap.Provinces[coords];
						distance = neighborProvince.Center.Distance( DestinationProvince.Center );
					}
				}
				
				if ( PathProvinces.Contains( possibleProvince ) ) return;

				PathProvinces.AddLast( possibleProvince );
				province = possibleProvince;
			}
		}

		public void CalculateSpeed()
		{
			if ( Army.Units.Count == 1 )
			{
				Speed = 4;
				return;
			}
			
			var lowerSpeed = 200;
			foreach (var unit in Army.Units
				         .Where(unit => !unit.Config.Name.Equals(Army.Country.GetEliteUnit()))
				         .Where(unit => unit.Config.Speed <= lowerSpeed))
			{
				lowerSpeed = unit.Config.Speed;
			}

			Speed = lowerSpeed;
			
			switch (CurrentProvince.Biome.Name)
			{
				case "Plains":
					Speed += 2;
					break;
				case "Mountains":
					Speed -= 1;
					break;
				case "Swamp":
					Speed -= 2;
					break;
				case "Desert":
					Speed -= 1;
					break;
			}
		}

		public string GetEstimatedArrival(Scene scene)
		{
			var time = scene.Components.GetInDescendants<TimeComponent>();
			var estimatedTime = time.CurrentTime + ((((100 - Progress) + 100 * (PathProvinces.Count - 1)) / Speed) * (60 * 60 * 1000));
			
			return time.GetFormattedTime(estimatedTime);
		}

		public void EndWalk()
		{
			StartingProvince = null;
			CurrentProvince = null;
			DestinationProvince = null;
			IsMoving = false;
			Progress = 0;
			PathProvinces.Clear();
		}
	}
}
