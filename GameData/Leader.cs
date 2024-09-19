using System.Text.Json.Nodes;

namespace Sandbox.GameData;

public class Leader
{
	public string Name;
	public string Portrait;
	
	public static void Load( JsonObject json, Dictionary<string, Leader> map )
	{
		map.Clear();
		var leaders = json["leaders"];

		if ( leaders == null )
		{
			Log.Warning("Error while processing Leaders!");
			return;
		}
		
		foreach (var jsonNode in leaders.AsArray())
		{
			var leader = new Leader
			{
				Name = jsonNode["name"] != null ? jsonNode["name"].AsValue().GetValue<string>() : "Unknown name",
				Portrait = jsonNode["portrait"] != null ? jsonNode["portrait"].AsValue().GetValue<string>() : "unknown"
			};
			
			map.Add(leader.Name, leader);
		}
	}
}
