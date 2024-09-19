using System;
using System.Text.Json.Nodes;
using Sandbox.GameData;

namespace Sandbox.Configs;

public class ConfigUnit
{
	public string Name;
	public string Description;
	public int Damage;
	public int Defense;
	public int Speed;
	public int OreCost;
	public int ManpowerCost;
	public int MaxHealth = 100;
	public bool Healer = false;
	public string ModelName;
	public string StructureRequired;
	public string Culture;
	public string Country;

	public static void Load( JsonObject json, Dictionary<string, ConfigUnit> map )
	{
		map.Clear();
		var units = json["units"];

		if ( units == null )
		{
			Log.Warning("Error while processing Units!");
			return;
		}
		
		foreach (var jsonNode in units.AsArray())
		{
			var unit = new ConfigUnit
			{
				Name = jsonNode["name"] != null ? jsonNode["name"].AsValue().GetValue<string>() : "Unknown name",
				Description = jsonNode["description"] != null ? jsonNode["description"].AsValue().GetValue<string>() : "Unknown description",
				Damage = jsonNode["damage"] != null ? jsonNode["damage"].AsValue().GetValue<int>() : 0,
				Defense = jsonNode["defense"] != null ? jsonNode["defense"].AsValue().GetValue<int>() : 0,
				Speed = jsonNode["speed"] != null ? jsonNode["speed"].AsValue().GetValue<int>() : 4,
				OreCost = jsonNode["oreCost"] != null ? jsonNode["oreCost"].AsValue().GetValue<int>() : 0,
				ManpowerCost = jsonNode["manpowerCost"] != null ? jsonNode["manpowerCost"].AsValue().GetValue<int>() : 0,
				ModelName = jsonNode["modelName"] != null ? jsonNode["modelName"].AsValue().GetValue<string>() : "models/characters/army.vmdl",
				Healer = jsonNode["healer"] != null && jsonNode["healer"].AsValue().GetValue<bool>(),
				StructureRequired = jsonNode["structure"]?.AsValue().GetValue<string>(),
				Culture = jsonNode["culture"] != null ? jsonNode["culture"].AsValue().GetValue<string>() : "Arvedain",
				Country = jsonNode["country"] != null ? jsonNode["country"].AsValue().GetValue<string>() : ""
			};
			
			map.Add(unit.Name, unit);
		}
	}

	public bool IsAvailable( Country country )
	{
		return (StructureRequired == null || country.HasStructure( StructureRequired )) && IsOurCulture( country );
	}
	
	public bool IsOurCulture( Country country )
	{
		return GetCulture().Equals(country.Culture) && (!country.Name.Equals("The Kingdom of Astacks") || country.Name.Equals("The Kingdom of Astacks") && Country.Equals("The Kingdom of Astacks"));
	}

	public Culture GetCulture()
	{
		return GameMap.Cultures[Culture];
	}
}
