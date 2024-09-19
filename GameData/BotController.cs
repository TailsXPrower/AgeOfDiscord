using AgeOfDiscord.UI;
using Sandbox.Events;

namespace Sandbox.GameData;

public class BotController
{
	public Country Country;

	private readonly List<Resource> ResourcesWeNeed = new();
	public readonly List<Province> OurProvinces = new();

	public void Think(Scene scene)
	{
		if ( HasWar() )
		{
			War(scene);
		}
		
		ArmyInExile( scene );
		
		var hasWood = CheckWood();

		if ( !hasWood )
		{
			CheckRequiredStructures( Resource.Wood );
		}
		
		var hasOre = CheckOre();
		
		if ( !hasOre )
		{
			CheckRequiredStructures( Resource.Ore );
		}
		
		var hasManpower = CheckManpower();
		
		if ( !hasManpower )
		{
			CheckRequiredStructures( Resource.Manpower );
		}

		if ( hasWood && hasOre && hasManpower && ResourcesWeNeed.Count == 0 )
		{
			FreeBuild();
		}
		
		var hasArmy = CheckArmy();

		if ( !hasArmy )
		{
			TryCreateArmy();
		}
		
		//Log.Info($"Country: {Country.Name}, Can war: {Country.CountReadyArmies()}");
		
		// Check trading
		foreach (var resource in ResourcesWeNeed.ToList())
		{
			switch ( resource )
			{
				case Resource.Wood when hasWood:
					ResourcesWeNeed.Remove( Resource.Wood );
					break;
				case Resource.Ore when hasOre:
					ResourcesWeNeed.Remove( Resource.Ore );
					break;
				case Resource.Manpower when hasManpower:
					ResourcesWeNeed.Remove( Resource.Manpower );
					break;
			}
			
			TryTrade(resource);
		}

		if ( !HasWar() )
		{
			DiscardAllies( scene );
		}
		else
		{
			CheckAllies(scene);
		}

		if ( Country.GetHostileCountries().Count < 3 && Country.GetAllyCountries().Count == 0)
		{
			CheckWar(scene);
		}

		CheckWarHostile( scene );
	}

	private void CheckAllies(Scene scene)
	{
		var countries = Country.GetHostileCountries();
		foreach (var possibleAlly in from country in countries from possibleAlly in country.Relations.Keys where
			         possibleAlly.Relations.ContainsKey(country) && possibleAlly.Relations[country] == Relation.War && 
			         possibleAlly.Relations.ContainsKey(Country) && possibleAlly.Relations[Country] == Relation.Neutral && possibleAlly.IsBot( scene ) select possibleAlly)
		{
			Country.MakeAlly( scene.Components.GetInDescendants<GameUI>(), Country.Name, possibleAlly.Name );
		}
	}
	
	private void DiscardAllies(Scene scene)
	{
		var random = new Random();
		var countries = Country.GetAllyCountries();
		foreach (var country in countries)
		{
			var hasArmy = false;
			foreach (var army in country.Provinces.Where(countryProvince => countryProvince.Army != null && countryProvince.Army.Country == Country).Select( countryProvince => countryProvince.Army ))
			{
				hasArmy = true;
				
				if (army.Movement.DestinationProvince != null && army.Movement.DestinationProvince.Country == Country)
					continue;
				
				Army.MoveTo(scene.Components.GetInDescendants<GameMap>(), army.Country.Name, army.Id, army.Province.RemapCoordinates, Country.Provinces[random.Next(Country.Provinces.Count)].RemapCoordinates);
			}

			if ( hasArmy )
			{
				return;
			}
			
			Country.MakeNeutral( Country.Name, country.Name );
		}
	}
	
	private void CheckWarHostile(Scene scene)
	{
		if ( Country.Armies.Values.Count < 3 || Country.CountReadyArmies() < 3 )
		{
			return;
		}

		var countriesToWar = Country.GetHostileCountries().SelectMany( country => country.Relations )
			.Where(pair => pair.Value == Relation.Ally)
			.Select( pair => pair.Key )
			.Where( country => CanDeclareWar( scene, country ) && country.Relations[Country] != Relation.War ).ToList();
		
		if (countriesToWar.Count == 0)
			return;
		
		var random = new Random();
		var country = countriesToWar[random.Next( countriesToWar.Count )];
		
		Country.DeclareWar(scene.Components.GetInDescendants<GameUI>(), country.Name, Country.Name);
	}

	private void CheckWar(Scene scene)
	{
		if ( Country.Armies.Values.Count < 3 || Country.CountReadyArmies() < 3 )
		{
			return;
		}

		var countriesToWar = Country.GetBorderingCountries().Where( country => CanDeclareWar( scene, country ) && country.Relations[Country] != Relation.War ).ToList();
		
		if (countriesToWar.Count == 0)
			return;
		
		var random = new Random();
		var country = countriesToWar[random.Next( countriesToWar.Count )];
		
		Country.DeclareWar(scene.Components.GetInDescendants<GameUI>(), Country.Name, country.Name);
	}

	private bool CanDeclareWar( Scene scene, Country country )
	{
		if ( country.Relations
		    .Any( pair => pair.Key != Country && pair.Key.Relations[Country] == Relation.Ally && pair.Value == Relation.War ) )
		{
			return true;
		}

		if ( Country.CountReadyArmies() < country.CountReadyArmies() )
			return false;

		var time = scene.Components.GetInDescendants<TimeComponent>();
		
		return time.GetDate().Year >= 1130 || country.Provinces.Select( province => province.Culture ).Any( culture => culture.Equals( Country.Culture ) );
	}
	
	private bool HasWar()
	{
		return Country.Relations.Values.Any( relation => relation == Relation.War );
	}

	public void ArmyInExile(Scene scene)
	{
		if ( Country.Armies.Values.All( army =>
			    army.Province.Country == Country || army.Country.Relations[army.Province.Country] == Relation.Ally ) )
		{
			return;
		}
		
		var gameMap = scene.Components.GetInDescendants<GameMap>();

		foreach (var army in Country.Armies.Values.Where( army =>
			         army.Province.Country != Country && army.Country.Relations[army.Province.Country] != Relation.Ally ))
		{
			if ( army.Country.Relations[army.Province.Country] == Relation.War )
			{
				Country.ClaimProvince(gameMap, army.Province.RemapCoordinates);
				continue;
			}
			
			if (army.Movement.IsMoving)
				continue;
			
			var province = Country.Provinces.First();
			Army.MoveTo(gameMap, army.Country.Name, army.Id, army.Province.RemapCoordinates, province.RemapCoordinates);
		}
	}

	public void War(Scene scene)
	{
		var gameMap = scene.Components.GetInDescendants<GameMap>();
		var weLosing = OurProvinces.Any( province => province.Country != Country );
		if ( Country.Armies.Values.All( army => army.Movement.IsMoving || army.CurrentBattle != null ) )
		{
			return;
		}
		
		if ( weLosing )
		{
			foreach (var ourProvince in OurProvinces.Where( province => province.Country != Country ))
			{
				if ( Country.Armies.Values.Any( army => army.Movement.DestinationProvince == ourProvince ) )
				{
					continue;
				}

				if (Country.Armies.Values.All( army => army.Movement.IsMoving || army.CurrentBattle != null ))
					break;
				
				var army = Country.Armies.Values.First( army => !army.Movement.IsMoving && army.CurrentBattle == null );
				Army.MoveTo(gameMap, army.Country.Name, army.Id, army.Province.RemapCoordinates, ourProvince.RemapCoordinates);
			}
		}
		else
		{
			var hostiles = Country.GetHostileCountries();
			foreach (var army in Country.Armies.Values.Where( army => !army.Movement.IsMoving && army.CurrentBattle == null ))
			{
				foreach (var province in army.Province.Neighbors.Select(neighbor => GameMap.Provinces[neighbor]))
				{
					if (!hostiles.Contains(province.Country))
						continue;
					
					Army.MoveTo(gameMap, army.Country.Name, army.Id, army.Province.RemapCoordinates, province.RemapCoordinates);
					break;
				}
			}

			var random = new Random();
			
			foreach (var hostile in hostiles)
			{
				var province = hostile.Provinces[random.Next(hostile.Provinces.Count)];
				
				if ( Country.Armies.Values.Any( army => army.Movement.DestinationProvince == province ) )
				{
					continue;
				}

				if (Country.Armies.Values.All( army => army.Movement.IsMoving || army.CurrentBattle != null ))
					break;
					
				var army = Country.Armies.Values.First( army => !army.Movement.IsMoving && army.CurrentBattle == null );
				Army.MoveTo(gameMap, army.Country.Name, army.Id, army.Province.RemapCoordinates, province.RemapCoordinates);
			}
		}
	}

	private bool CheckArmy()
	{
		CheckUnits();
		
		if ( Country.Provinces.All( province => province.Army != null ) )
			return true;

		if ( GetMaxArmy() <= Country.Armies.Count )
		{
			return true;
		}

		return false;
	}
	
	private int GetMaxArmy()
	{
		return Country.Towns.Count;
	}

	private void TryCreateArmy()
	{
		if ( Country.Ore < 15 )
		{
			return;
		}
		
		var provinces = Country.Provinces.FindAll(province => province.Army == null);

		if ( provinces.Count == 0 )
		{
			return;
		}

		var random = new Random();
		var province = provinces[random.Next(0, provinces.Count)];

		var id = Guid.NewGuid();
		Army.NewArmy( Country.Name, id, province.RemapCoordinates );
		CheckUnits( Country.Armies[id] );
	}

	private void CheckUnits()
	{
		var units = Settings.Units.Values
			.Where( unit => unit.Name != Country.GetEliteUnit() && unit.IsOurCulture( Country ) ).ToList();
		var random = new Random();
		foreach (var army in Country.Armies.Values.Where(army => army.Units.Count < army.GetMaxUnits()))
		{
			var requiredUnits = army.GetMaxUnits() - army.Units.Count;
			for (var i = 0; i < requiredUnits; i++)
			{
				if ( army.Country.Ore < 15 || army.Country.GetManpower() < 100 )
				{
					break;
				}
			
				if ( army.Units.Count >= army.GetMaxUnits() )
				{
					break;
				}
				
				var config = units[random.Next(units.Count)];
				Army.NewUnit( Country.Name, army.Id, config.Name );
			}
		}
	}
	
	private void CheckUnits(Army army)
	{
		var units = Settings.Units.Values
			.Where( unit => unit.Name != Country.GetEliteUnit() && unit.IsOurCulture( Country ) ).ToList();
		var random = new Random();
		
		var requiredUnits = army.GetMaxUnits() - army.Units.Count;
		for (var i = 0; i < requiredUnits; i++)
		{
			if ( army.Country.Ore < 15 || army.Country.GetManpower() < 100 )
			{
				break;
			}
			
			if ( army.Units.Count >= army.GetMaxUnits() )
			{
				break;
			}
				
			var config = units[random.Next(units.Count)];
			Army.NewUnit( Country.Name, army.Id, config.Name );
		}
	}

	private void TryTrade(Resource resource)
	{
		var weNeed = GetRequired( resource );
		var weTrade = GetTrade();

		if ( GetRequired( weTrade ) == 0 )
		{
			return;
		}
		
		if ( GetValue( weTrade ) / GetRequired( weTrade ) < 2 )
		{
			return;
		}

		var countriesToTrade = GameMap.Countries.Values.Where( country => country != Country && GetValue(country, resource) > weNeed && !TradeSystem.HasOffer(country) && !country.Relations.ContainsValue( Relation.War ) ).ToList();
		
		if (countriesToTrade.Count == 0)
			return;
		
		var random = new Random();
		var country = countriesToTrade[random.Next(countriesToTrade.Count)];

		var weOffer = (int)(TradeOffer.ExchangeRates[resource.ToString()] * weNeed /
		               TradeOffer.ExchangeRates[weTrade.ToString()]);

		if ( weOffer == 0 )
		{
			return;	
		}
		
		if (country.LastTrades.Any(lastTrade => lastTrade.IsCancelled && lastTrade.From.Equals(Country) && lastTrade.FromOffer.ContainsKey(weTrade.ToString())
		                                                                       && lastTrade.ToOffer.ContainsKey(resource.ToString())))
			return;

		country.LastTrades.RemoveAll( lastTrade => lastTrade.From.Equals( Country ) );
		
		TradeOffer.SendTrade(Guid.NewGuid(), Country.Name, country.Name, new Dictionary<string, int> { { weTrade.ToString(), weOffer } }, new Dictionary<string, int> { { resource.ToString(), weNeed } });
	}

	private Resource GetTrade()
	{
		return Country.Balance > Country.Wood && Country.Balance > Country.Ore ? Resource.Coins :
			Country.Wood > Country.Balance && Country.Wood > Country.Ore ? Resource.Wood :
			Country.Ore > Country.Balance && Country.Ore > Country.Wood ? Resource.Ore : Resource.Manpower;
	}
	
	private int GetValue(Resource resource)
	{
		return GetValue( Country, resource );
	}
	
	private int GetValue(Country country, Resource resource)
	{
		return resource switch
		{
			Resource.Coins => country.Balance,
			Resource.Wood => country.Wood,
			Resource.Ore => country.Ore,
			Resource.Manpower => country.GetManpower()
		};
	}

	private void FreeBuild()
	{
		if ( Country.Provinces.All( province => province.Structures.Count != 0 ) )
			return;

		if ( Country.Balance < 40 || Country.Wood < 30 )
		{
			return;
		}
		
		var random = new Random();
		var structure = random.Next( 4 ) switch
		{
			0 => "Farm",
			1 => "Sawmill",
			2 => "Mine",
			3 => "Well"
		};

		if ( !Country.CanAfford( structure ) )
		{
			return;
		}
		
		if ( !Country.HasFreeBuilders() )
		{
			return;
		}
		
		foreach (var province in Country.Provinces.Where(province => province.Town == null && province.Structures.Count == 0))
		{
			TryBuild( structure, province );
		}
	}

	private void CheckRequiredStructures(Resource resource)
	{
		var structure = resource switch
		{
			Resource.Coins => "Farm",
			Resource.Wood => "Sawmill",
			Resource.Ore => "Mine",
			Resource.Manpower => "Farm",
			_ => ""
		};
		
		var biome = resource switch
		{
			Resource.Coins => "Plains",
			Resource.Wood => "Forest",
			Resource.Ore => "Mountains",
			Resource.Manpower => "Plains",
			_ => ""
		};

		var structures = Country.CountStructure( structure );
		if ( structures >= 2 || structures >= Country.Provinces.Count(province => province.Biome.Name == biome))
		{
			return;
		}
		
		foreach (var province in Country.Provinces.Where(province => province.Town == null && province.Structures.Count == 0 && province.Biome.Name == biome))
		{
			TryBuild( structure, province );
		}
	}

	public void TryBuild( string name, Province province )
	{
		if ( !Country.CanAfford( name ) )
		{
			return;
		}
		
		if ( !Country.HasFreeBuilders() )
		{
			return;
		}
	
		Country.BuildStructure(name, province);
	}

	private bool CheckWood()
	{
		if (Country.Wood > GetRequired(Resource.Wood))
			return true;

		if (Country.GetStructures().Count( structure => structure.GetName() == "Sawmill" ) >= 2)
			return true;

		if ( !Country.Provinces.Any( province =>
			    province.Town == null && province.Structures.Count == 0 && province.Biome.Name == "Forest" ) )
		{
			if (!ResourcesWeNeed.Contains( Resource.Wood ))
				ResourcesWeNeed.Add(Resource.Wood);
			return true;
		}

		return false;
	}
	
	private int GetRequired(Resource resource)
	{
		return resource switch
		{
			Resource.Coins => 10 * Country.Provinces.Count,
			Resource.Wood => 10 * Country.Provinces.Count,
			Resource.Ore => 10 * Country.Provinces.Count,
			Resource.Manpower => 1000
		};;
	}

	private bool CheckOre()
	{
		if (Country.Ore > GetRequired(Resource.Ore))
			return true;

		if (Country.GetStructures().Count( structure => structure.GetName() == "Mine" ) >= 2)
			return true;

		if ( !Country.Provinces.Any( province =>
			    province.Town == null && province.Structures.Count == 0 && province.Biome.Name == "Mountains" ) )
		{
			if (!ResourcesWeNeed.Contains( Resource.Ore ))
				ResourcesWeNeed.Add(Resource.Ore);
			return true;
		}

		return false;
	}
	
	private bool CheckManpower()
	{
		if (Country.GetStructures().Count( structure => structure.GetName() == "Farm" ) >= 2)
			return true;

		if ( Country.GetManpower() > 1000 )
			return true;

		if (!Country.Provinces.Any( province =>
			    province.Town == null && province.Structures.Count == 0 && province.Biome.Name == "Plains" ) )
		{
			if (!ResourcesWeNeed.Contains(Resource.Manpower))
				ResourcesWeNeed.Add(Resource.Manpower);
			return true;
		}

		return false;
	}

	private enum Resource
	{
		Coins,
		Wood,
		Ore,
		Manpower
	}
}
