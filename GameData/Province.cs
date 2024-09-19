using System.Text.Json.Nodes;

namespace Sandbox.GameData;

public class Province
{
	public string Name;
	public Vector2 RemapCoordinates;
	public Vector2 Center;
	public int Manpower;
	public List<Vector2> Neighbors;
	public Dictionary<Guid, Structure> Structures = new();
	public Country Country;
	public Town Town;
	public Biome Biome;
	public Culture Culture;
	public Army Army;

	public float GetX()
	{
		return RemapCoordinates.x;
	}
	
	public float GetY()
	{
		return RemapCoordinates.y;
	}

	public int GetManpower()
	{
		try
		{
			if ( Culture != Country.Culture )
			{
				return (int)(Manpower * 0.4);
			}	
		} catch (NullReferenceException) {}

		return Manpower;
	}
	
	public void AddStructure(Guid id, string name, bool completed = false)
	{
		var structure = new Structure( name, this ) { Id = id };
		structure.SetCompleted(completed);
		Structures.Add(id, structure);
	}
	
	public bool HasStructure(string name)
	{
		return Structures.Values.Any( structure => structure.GetName().Equals( name ) );
	}
	
	public static void Load( JsonObject json, Dictionary<Vector2, Province> map )
	{
		map.Clear();
		var provinces = json["provinces"];

		if ( provinces == null )
		{
			Log.Warning("Error while processing Provinces!");
			return;
		}
		
		foreach (var jsonNode in provinces.AsArray())
		{
			var province = new Province
			{
				Name = jsonNode["name"] != null ? jsonNode["name"].AsValue().GetValue<string>() : "Unknown name",
				Biome = jsonNode["biome"] != null ? GameMap.Biomes[jsonNode["biome"].AsValue().GetValue<string>()] : GameMap.Biomes["Plains"],
				Culture = jsonNode["culture"] != null ? GameMap.Cultures[jsonNode["culture"].AsValue().GetValue<string>()] : null,
				Manpower = jsonNode["manpower"] != null ? jsonNode["manpower"].AsValue().GetValue<int>() : 1000
			};

			// Remap
			if ( jsonNode["remap"] != null )
			{
				var arr = jsonNode["remap"].AsArray();
				if ( arr.Count == 2 )
				{
					province.RemapCoordinates = new Vector2(arr[0].GetValue<int>(), arr[1].GetValue<int>());
				}
			}
			else
			{
				province.RemapCoordinates = new Vector2( 0, 0 );
			}
			
			// Center
			if ( jsonNode["center"] != null )
			{
				var arr = jsonNode["center"].AsArray();
				if ( arr.Count == 2 )
				{
					province.Center = new Vector2(arr[0].GetValue<float>(), arr[1].GetValue<float>());
				}
			}
			else
			{
				province.Center = new Vector2( 0, 0 );
			}
			
			province.Neighbors = new List<Vector2>();
			// Neighbors
			if ( jsonNode["neighbors"] != null )
			{
				var arr = jsonNode["neighbors"].AsArray();
				foreach (var node in arr)
				{
					if ( node.AsArray().Count == 2 )
					{
						province.Neighbors.Add(new Vector2(node.AsArray()[0].GetValue<int>(), node.AsArray()[1].GetValue<int>()));
					}	
				}
			}
			
			map.Add(province.RemapCoordinates, province);
		}
	}
}
