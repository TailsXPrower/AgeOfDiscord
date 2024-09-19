using System.Text.Json.Nodes;
using AgeOfDiscord.UI;
using AgeOfDiscord.UI.Menu;
using Menu;
using Sandbox.Configs;
using Sandbox.Events;
using Sandbox.Exceptions;
using Sandbox.Modes;
using Sandbox.Network;
using Sandbox.Utils;

namespace Sandbox.GameData;

public class Country
{
	public string Name;
	public Town Capital;
	public List<Province> Provinces;
	public List<Town> Towns = new();
	public Dictionary<Guid, Army> Armies = new();
	public Color32 Color;
	
	public Dictionary<Country, Relation> Relations = new();
	
	public List<TradeOffer> LastTrades = new();
	
	public Culture Culture;
	public Leader Leader;
	public string Flag;

	public int Balance = 100;
	public int Wood = 100;
	public int Ore = 100;
	
	public bool IsPlayer = false;
	public bool IsSpectator = false;
	public BotController Controller;
	
	public int GetWoodIncome()
	{
		return 1 + Provinces.SelectMany(province => province.Structures.Values).Where(structure => structure.IsCompleted && structure.GetName().Equals("Sawmill")).Sum(
			structure => structure.Config.EffectiveBiomes.Contains( structure.Province.Biome.Name ) ? 10 : 2 );
	}
	
	public int GetOreIncome()
	{
		return Provinces.SelectMany(province => province.Structures.Values).Where(structure => structure.IsCompleted && structure.GetName().Equals("Mine")).Sum(
			structure => structure.Config.EffectiveBiomes.Contains( structure.Province.Biome.Name ) ? 10 : 2 );
	}
	
	public int GetManpowerIncome()
	{
		return 10 + Provinces.SelectMany(province => province.Structures.Values).Where(structure => structure.IsCompleted && structure.GetName().Equals("Farm")).Sum(
			structure => structure.Config.EffectiveBiomes.Contains( structure.Province.Biome.Name ) ? 200 : 100 );
	}

	public bool IsBot(Scene scene)
	{
		return scene.GetAllComponents<Player>().All( player => player.Country != Name );
	}
	
	public Player GetPlayer(Scene scene)
	{
		try
		{
			return scene.GetAllComponents<Player>().First( player => player.Country == Name );
		}
		catch ( InvalidOperationException )
		{
			return null;
		}
	}
	
	public void AddManpower(int manpower)
	{
		if (Provinces.Count == 0)
			return;
		
		var toAdd = manpower / Provinces.Count;
		foreach (var province in Provinces.TakeWhile(_ => manpower > 0))
		{
			province.Manpower += toAdd;
			manpower -= toAdd;
		}
	}

	public void SubtractManpower(int manpower)
	{
		while ( manpower > 0)
		{
			foreach (var province in Provinces.OrderBy( province => province.Manpower ).Reverse())
			{
				if (manpower <= 0)
					break;
			
				var toSubtract = Math.Min(province.Manpower * 0.25f, manpower);
				province.Manpower -= (int)toSubtract;
				manpower -= (int)toSubtract;
			}	
		}
	}
	
	public int GetManpower()
	{
		return Provinces.Sum( province => province.GetManpower() );
	}
	
	public string GetManpowerString()
	{
		return Provinces.Sum( province => province.GetManpower() ).ToString("N0");
	}

	public int GetMoneyIncome()
	{
		return (int)(GetManpower() * 0.001);
	}

	public Vector2 GetCenter()
	{
		float x = 0, y = 0;
		foreach (var province in Provinces)
		{
			x += province.Center.x;
			y += province.Center.y;
		}

		return new Vector2( x / Provinces.Count, y / Provinces.Count );
	}
	
	public List<Country> GetBorderingCountries()
	{
		var countries = new List<Country>();
		foreach (var province in from countryProvince in Provinces from neighbor in countryProvince.Neighbors select GameMap.Provinces[neighbor] into province where province.Country != this where !countries.Contains(province.Country) select province)
		{
			countries.Add(province.Country);
		}

		return countries;
	}

	[Broadcast]
	public static void OnMonthTick(string countryName)
	{
		var country = GameMap.Countries[countryName];
		country.LastTrades.Clear();
	}

	[Broadcast]
	public static void OnTick(string countryName)
	{
		var country = GameMap.Countries[countryName];
		
		country.Balance += country.GetMoneyIncome();
		country.Wood += 1;
		
		foreach ( var structure in country.Provinces.SelectMany( province => province.Structures.Values ))
		{
			if (!structure.IsCompleted)
				continue;
			
			structure.OnTick();
		}
		
		foreach (var army in country.Armies.Values.Where(army => army.Health < 100))
		{
			if (army.CurrentBattle != null)
				continue;
			
			var healRequired = army.Units.Sum( unit => unit.Health < unit.Config.MaxHealth ? unit.Config.MaxHealth - unit.Health : 0);
			var manpowerRequired = army.Units.Sum( unit => unit.Health < unit.Config.MaxHealth ? unit.Config.ManpowerCost * (unit.Health / unit.Config.MaxHealth) : 0);

			if ( country.GetManpower() < manpowerRequired )
			{
				return;
			}
			
			country.SubtractManpower(manpowerRequired);
			army.Heal( healRequired );
		}
	}
	
	public void BuildStructure(string name, Province province)
	{
		var structure = Settings.Structures[name];
		if ( structure == null )
			throw new GameException( "There is no structure with that name!" );

		if (province.Country != this)
			throw new GameException( "The province is not in our country!" );
		
		if (GetBuildableStructures().Count >= Towns.Count)
			throw new GameException( "We can't build because all the builders are busy!" );
		
		if (Balance < structure.Cost)
			throw new GameException( "Insufficient funds" );
		
		if (Wood < structure.WoodCost)
			throw new GameException( "We don't have enough wood to build this structure!" );
		
		switch (structure.OnlyForTown)
		{
			case true when province.Town == null:
				throw new GameException( "This structure can only be built in Town Province!" );
			case false when province.Town != null:
				throw new GameException( "This structure cannot be built in Town Province!" );
			case false when province.Structures.Count > 0:
				throw new GameException( "The province has already built the structure!" );
		}
		
		if (structure.MaxPerTown != -1 && GetStructures().FindAll( townStruct => townStruct.GetName() == structure.Name ).Count >= structure.MaxPerTown )
			throw new GameException( "Town has reached the maximum of this type of structure!" );

		AddStructure(name, Name, Guid.NewGuid(), province.RemapCoordinates);
	}

	[Broadcast]
	public static void AddStructure( string name, string countryName, Guid id, Vector2 coords )
	{
		var country = GameMap.Countries[countryName];
		var province = GameMap.Provinces[coords];
		var structure = Settings.Structures[name];
		
		country.Balance -= structure.Cost;
		country.Wood -= structure.WoodCost;
		
		province.AddStructure(id, name);
	}

	public void ClaimProvince(GameMap map, Vector2 coords)
	{
		if (!GameMap.Provinces.ContainsKey(coords)) 
			return;

		var province = GameMap.Provinces[coords];

		if ( province.Country != null )
		{
			province.Country.Provinces.Remove( province );

			if ( province.Town != null )
			{
				province.Town.Country = this;
				Towns.Add(province.Town);
				province.Country.Towns.Remove( province.Town );
			}

			if ( province.Country.Provinces.Count == 0 )
			{
				if ( !province.Country.IsBot( map.Scene ) && !province.Country.GetPlayer( map.Scene ).IsProxy)
				{
					Sandbox.Services.Stats.Increment("deaths", 1);
					Logger.NotifySelf(map.Scene.Components.GetInDescendants<GameUI>(), $"You have been defeated by {Name}!", "So sad...");
					var gameUi = map.Scene.Components.GetInDescendants<GameUI>();
					var confirmation = new Confirmation
					{
						OnDisagree = () =>
						{
							if (GameNetworkSystem.IsActive)
								GameNetworkSystem.Disconnect();
		
							Sound.StopAll(0);
							map.Scene.LoadFromFile("Scenes/mainmenu.scene");
						},
						Message = $"You have been defeated by {Name}!",
						Accept = "Spectate",
						Decline = "Quit the game"
					};
					gameUi.Panel.AddChild( confirmation );
				}

				if ( !IsBot( map.Scene ) && !GetPlayer( map.Scene ).IsProxy )
				{
					Services.Stats.Increment("killed_countries", 1);
				}
				
				province.Country.Delete();
				Logger.NotifySelf(map.Scene.Components.GetInDescendants<GameUI>(), $"{Name} has defeated {province.Country.Name}!", "Astonishing...");

				Balance += province.Country.Balance / 3;
				Wood += province.Country.Wood / 3;
				Ore += province.Country.Ore / 3;

				province.Country.Balance = 0;
				province.Country.Wood = 0;
				province.Country.Ore = 0;

				if ( GameMap.Countries.Count == 1 )
				{
					if ( !IsBot( map.Scene ) && !GetPlayer( map.Scene ).IsProxy )
					{
						Services.Stats.Increment("wins",1);
					}
					
					Services.Stats.Increment("games_played", 1);
					Logger.EndGame(map.Scene, $"{Name} has won this game!");
					return;
				}
			}
		}
		
		Provinces.Add(province);
		province.Country = this;
		map.PaletteTex.Update(map.GetColor(new Color32( (byte)province.GetX(), (byte)province.GetY(), 0 ), false), province.GetX(), province.GetY());
	}
	
	public List<Country> GetHostileCountries()
	{
		return GameMap.Countries.Values.Where(country => country.Relations.ContainsKey(this) && country.Relations[this] == Relation.War).ToList();
	}
	
	public List<Country> GetAllyCountries()
	{
		return GameMap.Countries.Values.Where(country => country.Relations.ContainsKey(this) && country.Relations[this] == Relation.Ally).ToList();
	}

	public void NewTown( Vector2 coords )
	{
		if (!GameMap.Provinces.ContainsKey(coords)) 
			return;
		
		var province = GameMap.Provinces[coords];
		
		if (province.Structures.Count > 0)
			return;
		
		if (province.Town != null)
			return;
		
		if (province.Country != this)
			return;
		
		var town = new Town( province.Name, province ) { Country = this };

		GameMap.Towns.Add(town.Name, town);
		Towns.Add(town);
	}

	public void CreateArmy(Scene scene)
	{
		var provinces = Provinces.FindAll(province => province.Army == null);

		if ( provinces.Count == 0 )
		{
			return;
		}

		var random = new Random();
		var province = provinces[random.Next(0, provinces.Count)];
		
		if ( Ore < Settings.Units[GetEliteUnit()].OreCost )
		{
			Sound.Play( "decline" );
			return;
		}
		
		Army.NewArmy(Name, Guid.NewGuid(), province.RemapCoordinates);
		UnitMode.SelectProvince(scene.Components.GetInDescendants<GameMap>(), province);
	}
	
	public void CreateUnit( string configName )
	{
		if ( configName.Equals( GetEliteUnit() ) )
		{
			var provinces = Provinces.FindAll(province => province.Army == null);

			if ( provinces.Count == 0 )
			{
				return;
			}

			var random = new Random();
			var province = provinces[random.Next(0, provinces.Count)];
			
			Army.NewArmy(Name, Guid.NewGuid(), province.RemapCoordinates, true);
		}
		else
		{
			if (UnitMode.SelectedArmy != null) 
				Army.NewUnit( Name, UnitMode.SelectedArmy.Id, configName );
		}
	}
	
	public bool IsAvailableUnit(string name)
	{
		return Settings.Units[name].IsAvailable(this);
	}

	public IEnumerable<ConfigUnit> GetAvailableUnits()
	{
		return Settings.Units.Values.Where( unit => unit.Name != GetEliteUnit() && unit.IsAvailable(this) );
	}
	
	public List<Structure> GetBuildableStructures()
	{
		return Provinces.SelectMany( province => province.Structures.Values ).Where( structure => !structure.IsCompleted ).ToList();
	}
	
	public List<Structure> GetStructures()
	{
		return Provinces.SelectMany( province => province.Structures.Values ).ToList();
	}
	
	public bool CanAfford(string structureName)
	{
		var structure = Settings.Structures[structureName];
		return structure.Cost <= Balance && structure.WoodCost <= Wood;
	}

	public bool HasFreeBuilders()
	{
		return GetBuildableStructures().Count < Towns.Count;
	}
	
	public bool HasStructure(string name)
	{
		return GetStructures().Any( structure => structure.GetName().Equals( name ) && structure.IsCompleted );
	}
	
	public int CountStructure(string name)
	{
		return GetStructures().Count(structure => structure.GetName().Equals( name ));
	}

	public int CountReadyArmies()
	{
		return Armies.Values.Count( army => army.Units.Count >= 15 );
	}
	
	public string GetEliteUnit()
	{
		return  Name.Equals("The Empire of Arvelore") ? "Ballator's guard" : Culture.EliteUnit;
	}
	
	public void CheckDeclareWar(Scene scene, Country country)
	{
		if ( country.Relations
		    .Any( pair => pair.Key != this && pair.Key.Relations[this] == Relation.Ally && pair.Value == Relation.War ) )
		{
			return;
		}
		
		if ( Armies.Values.Count < 3 || CountReadyArmies() < 3 )
		{
			throw new GameException( "To declare war, you need to have 3 half full armies (Min. 15 Units per army)." );
		}

		var time = scene.Components.GetInDescendants<TimeComponent>();
		
		if ( time.GetDate().Year < 1130 && !country.Provinces.Select( province => province.Culture ).Any( culture => culture.Equals( Culture ) ) )
		{
			throw new GameException( "You can't declare war without a reason before 1130 year. (The reason is the retrieval of cultural provinces)" );
		}
	}

	[Broadcast]
	public static void DeclareWar(GameUI gameUi, string countryName, string enemyName )
	{
		Logger.NotifySelf(gameUi, $"{countryName} has declared war against {enemyName}!", "Interesting...");
		
		Sound.Play("war");

		var aggressor = GameMap.Countries[countryName];
		var country = GameMap.Countries[enemyName];
		
		country.Relations[aggressor] = Relation.War;
		aggressor.Relations[country] = Relation.War;
	}
	
	public void CheckAlly(Scene scene, Country country)
	{
		var countries = GetHostileCountries();
		if (countries.Any(hostile => country.Relations.ContainsKey(hostile) && country.Relations[hostile] == Relation.War && 
		                             country.Relations.ContainsKey(this) && country.Relations[this] == Relation.Neutral))
		{
			return;
		}
		
		if (!country.Culture.Equals(Culture) )
		{
			throw new GameException( "You can't ally country with different culture! You can only become an ally if you have a common enemy!" );
		}
		
		if ( country.Armies.Count >= Armies.Count )
		{
			throw new GameException( "To ally country, you have to be more powerful than them." );
		}
	}
	
	[Broadcast]
	public static void MakeAlly(GameUI gameUi, string countryName, string allyName )
	{
		Logger.NotifySelf(gameUi, $"{countryName} became an ally of {allyName}!", "Interesting...");

		var country = GameMap.Countries[countryName];
		var ally = GameMap.Countries[allyName];
		
		country.Relations[ally] = Relation.Ally;
		ally.Relations[country] = Relation.Ally;
	}
	
	public void CheckNeutrality(Scene scene, Country country)
	{
		if (country.Relations[this] == Relation.War)
		{
			if ( country.Capital.Province.Country == this )
			{
				throw new GameException( "You can't send neutrality because you took their capital!" );
			}
			
			if ( country.CountReadyArmies() + 1 >= CountReadyArmies())
			{
				throw new GameException( "You cannot send a neutrality request while the enemy is stronger than you!" );
			}
			
			throw new GameException( "You can't send neutrality to a country while being in war with it!" );
		}

		var hasArmy = false;
		foreach (var _ in country.Provinces.Where(countryProvince => countryProvince.Army != null && countryProvince.Army.Country == this))
		{
			hasArmy = true;
		}

		if ( hasArmy )
		{
			throw new GameException( "You cannot send neutrality to a country while your units are on its territory!" );
		}
	}
	
	[Broadcast]
	public static void MakeNeutral(string countryName, string neutralName )
	{
		var country = GameMap.Countries[countryName];
		var neutral = GameMap.Countries[neutralName];
		
		country.Relations[neutral] = Relation.Neutral;
		neutral.Relations[country] = Relation.Neutral;
	}
	
	public void SendOffer(Country toCountry, Dictionary<string, int> weOffer, Dictionary<string, int> weWant)
	{
		if ( weOffer["Coins"] > Balance || weOffer["Wood"] > Wood || weOffer["Ore"] > Ore || weOffer["Manpower"] > GetManpower() )
		{
			throw new GameException( "We cannot afford that!" );
		}

		if ( TradeSystem.HasOffer( toCountry ) )
		{
			throw new GameException( "This country is currently unable to accept the offer." );
		}

		var toRemove = weOffer.Where(pair => pair.Value <= 0)
			.Select(pair => pair.Key)
			.ToList();

		foreach (var key in toRemove)
		{
			weOffer.Remove(key);
		}
		
		toRemove = weWant.Where(pair => pair.Value <= 0)
			.Select(pair => pair.Key)
			.ToList();

		foreach (var key in toRemove)
		{
			weWant.Remove(key);
		}
		
		TradeOffer.SendTrade(Guid.NewGuid(), Name, toCountry.Name, weOffer, weWant);
	}

	public void Delete()
	{
		Sound.Play("captured");
		
		foreach (var country in GameMap.Countries.Values)
		{
			country.Relations.Remove( this );
		}

		foreach (var army in Armies.Values)
		{
			army.Delete();
		}

		GameMap.Countries.Remove( Name );
		IsSpectator = true;
	}
	
	public static void Load( JsonObject json, Dictionary<string, Country> map )
	{
		map.Clear();
		var countries = json["countries"];
		
		if ( countries == null )
		{
			Log.Warning("Error while processing Countries!");
			return;
		}
		
		foreach (var jsonNode in countries.AsArray())
		{
			var country = new Country
			{
				Name = jsonNode["name"] != null ? jsonNode["name"].AsValue().GetValue<string>() : "Unknown name",
				Flag = jsonNode["flag"] != null ? jsonNode["flag"].AsValue().GetValue<string>() : "Unknown name",
				Culture = jsonNode["culture"] != null ? GameMap.Cultures[jsonNode["culture"].AsValue().GetValue<string>()] : GameMap.Cultures["Northerners"],
				Balance = jsonNode["balance"] != null ? jsonNode["balance"].AsValue().GetValue<int>() : 0,
				Wood = jsonNode["wood"] != null ? jsonNode["wood"].AsValue().GetValue<int>() : 0,
				Ore = jsonNode["ore"] != null ? jsonNode["ore"].AsValue().GetValue<int>() : 0,
				IsPlayer = jsonNode["is_player"] != null && jsonNode["is_player"].AsValue().GetValue<bool>()
			};

			// Color
			if ( jsonNode["color"] != null )
			{
				var arr = jsonNode["color"].AsArray();
				if ( arr.Count == 3 )
				{
					country.Color = new Color32(arr[0].GetValue<byte>(), arr[1].GetValue<byte>(), arr[2].GetValue<byte>());
				}
			}
			else
			{
				country.Color = new Color32( 255, 0, 255, 0 );
			}
			
			// Towns
			if ( jsonNode["towns"] != null )
			{
				var arr = jsonNode["towns"].AsArray();
				foreach (var node in arr)
				{
					var townName = node.AsValue().GetValue<string>();
					var town = GameMap.Towns[townName];
					if (town == null)
						continue;
					town.Country = country;
					country.Towns.Add(town);
				}
			}
			
			// Capital
			if ( jsonNode["capital"] != null )
			{
				var townName = jsonNode["capital"].AsValue().GetValue<string>();
				country.Capital = GameMap.Towns[townName];
			}
			else
			{
				country.Capital = null;
			}
			
			// Leader
			if ( jsonNode["leader"] != null )
			{
				var leaderName = jsonNode["leader"].AsValue().GetValue<string>();
				country.Leader = GameMap.Leaders[leaderName];
			}
			else
			{
				country.Leader = null;
			}
			
			country.Provinces = new List<Province>();
			// Provinces
			if ( jsonNode["provinces"] != null )
			{
				var arr = jsonNode["provinces"].AsArray();
				foreach (var node in arr)
				{
					if ( node.AsArray().Count == 2 )
					{
						country.Provinces.Add(GameMap.Provinces[new Vector2(node.AsArray()[0].GetValue<int>(), node.AsArray()[1].GetValue<int>()) ]);
					}	
				}
			}

			country.Controller = new BotController { Country = country };
			country.Controller.OurProvinces.AddRange( country.Provinces );
			
			map.Add(country.Name, country);
		}
		
		foreach (var country in map.Values)
		{
			foreach (var otherCountry in map.Values.Where(otherCountry => country != otherCountry))
			{
				country.Relations.Add(otherCountry, Relation.Neutral);
			}
		}
	}
}
