using System.Text.Json.Nodes;

namespace Sandbox.GameData;

public class Culture
{
	public string Name;
	public string EliteUnit;
	public Color32 Color;
	public Vector2 Center;

	public Vector2 GetCenter()
	{
		return GameMap.Provinces[Center].Center;
	}
	
	public static void Load( JsonObject json, Dictionary<string, Culture> map )
	{
		map.Clear();
		var cultures = json["cultures"];

		if ( cultures == null )
		{
			Log.Warning("Error while processing Cultures!");
			return;
		}
		
		foreach (var jsonNode in cultures.AsArray())
		{
			var culture = new Culture
			{
				Name = jsonNode["name"] != null ? jsonNode["name"].AsValue().GetValue<string>() : "Unknown name",
				EliteUnit = jsonNode["eliteUnit"] != null ? jsonNode["eliteUnit"].AsValue().GetValue<string>() : "Elite"
			};
			
			// Color
			if ( jsonNode["color"] != null )
			{
				var arr = jsonNode["color"].AsArray();
				if ( arr.Count == 3 )
				{
					culture.Color = new Color32(arr[0].GetValue<byte>(), arr[1].GetValue<byte>(), arr[2].GetValue<byte>());
				}
			}
			else
			{
				culture.Color = new Color32( 255, 0, 255, 0 );
			}
			
			// Center
			if ( jsonNode["center"] != null )
			{
				var arr = jsonNode["center"].AsArray();
				if ( arr.Count == 2 )
				{
					culture.Center = new Vector2(arr[0].GetValue<int>(), arr[1].GetValue<int>());
				}
			}
			
			map.Add(culture.Name, culture);
		}
	}
}
