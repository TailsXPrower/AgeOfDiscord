using System;
using System.Text.Json.Nodes;

namespace Sandbox.Configs;

public class ConfigStructure
{
	public string Name;
	public string Description;
	public int Cost;
	public int WoodCost;
	public int HammerCost;
	public bool OnlyForTown;
	public bool TownHall;
	public int MaxPerTown;
	public List<string> EffectiveBiomes = new();

	public string ModelName;

	public static void Load( JsonObject json, Dictionary<string, ConfigStructure> map )
	{
		map.Clear();
		var structures = json["structures"];

		if ( structures == null )
		{
			Log.Warning("Error while processing Structures!");
			return;
		}
		
		foreach (var jsonNode in structures.AsArray())
		{
			var structure = new ConfigStructure
			{
				Name = jsonNode["name"] != null ? jsonNode["name"].AsValue().GetValue<string>() : "Unknown name",
				Description = jsonNode["description"] != null ? jsonNode["description"].AsValue().GetValue<string>() : "Unknown description",
				Cost = jsonNode["cost"] != null ? jsonNode["cost"].AsValue().GetValue<int>() : 0,
				WoodCost = jsonNode["woodCost"] != null ? jsonNode["woodCost"].AsValue().GetValue<int>() : 0,
				HammerCost = jsonNode["hammerCost"] != null ? jsonNode["hammerCost"].AsValue().GetValue<int>() : 0,
				OnlyForTown = jsonNode["onlyForTown"] != null && jsonNode["onlyForTown"].AsValue().GetValue<bool>(),
				TownHall = jsonNode["townHall"] != null && jsonNode["townHall"].AsValue().GetValue<bool>(),
				MaxPerTown = jsonNode["maxPerTown"] != null ? jsonNode["maxPerTown"].AsValue().GetValue<int>() : 0,
				ModelName = jsonNode["modelName"] != null ? jsonNode["modelName"].AsValue().GetValue<string>() : "models/citizen_props/chair02.vmdl"
			};
			
			if ( jsonNode["effectiveBiomes"] != null )
			{
				var arr = jsonNode["effectiveBiomes"].AsArray();
				foreach (var node in arr)
				{
					var biome = node.AsValue().GetValue<string>();
					structure.EffectiveBiomes.Add(biome);
				}
			}
			
			map.Add(structure.Name, structure);
		}
	}
}
