using System.Text.Json.Nodes;

namespace Sandbox.GameData;

public class Biome
{
	public string Name;
	public string ImageUrl;
	
	public static void Load( JsonObject json, Dictionary<string, Biome> map )
	{
		map.Clear();
		var biomes = json["biomes"];

		if ( biomes == null )
		{
			Log.Warning("Error while processing Biomes!");
			return;
		}
		
		foreach (var jsonNode in biomes.AsArray())
		{
			var biome = new Biome
			{
				Name = jsonNode["name"] != null ? jsonNode["name"].AsValue().GetValue<string>() : "Unknown name",
				ImageUrl = jsonNode["image_url"] != null ? jsonNode["image_url"].AsValue().GetValue<string>() : "unknown"
			};
			
			map.Add(biome.Name, biome);
		}
	}
}
