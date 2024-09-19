using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Sandbox.Configs;
using Sandbox.Exceptions;

namespace Sandbox.GameData;

public class Town
{
	public string Name;

	private Province _province;
	public Province Province
	{
		get => _province;
		private set
		{
			_province = value;
			value.Town = this;
		}
	}

	public Country Country;

	public Town(string name, Province province)
	{
		Name = name;
		Province = province;
		Province.Town = this;
	}
	
	public bool IsCapital()
	{
		return Country != null && Country.Capital.Equals( this );
	}

	public Model GetModel()
	{
		var townHall = Province.Structures.Values.ToList()[0].Config.ModelName;
		var model = Model.Load( townHall );
		return model;
	}
	
	public static void Load( JsonObject json, Dictionary<string, Town> map )
	{
		map.Clear();
		var towns = json["towns"];

		if ( towns == null )
		{
			Log.Warning("Error while processing Towns!");
			return;
		}
		
		foreach (var jsonNode in towns.AsArray())
		{
			var name = jsonNode["name"] != null ? jsonNode["name"].AsValue().GetValue<string>() : "Unknown name";

			Province province = null;
			// Province
			if ( jsonNode["province"] != null )
			{
				var arr = jsonNode["province"].AsArray();
				if ( arr.Count == 2 )
				{
					province = GameMap.Provinces[new Vector2(arr[0].GetValue<int>(), arr[1].GetValue<int>())];
				}
			}
			
			if (province == null)
				return;
		
			var town = new Town( name, province );
			
			var townHall = jsonNode["townHall"] != null ? jsonNode["townHall"].AsValue().GetValue<string>() : "Town Hall";
			province.AddStructure(Guid.NewGuid(), townHall, true);
			
			map.Add(town.Name, town);
		}
	}
}
