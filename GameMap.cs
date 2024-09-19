using System.Text.Json.Nodes;
using Sandbox.GameData;
using AgeOfDiscord.UI;
using AgeOfDiscord.UI.Menu;
using Menu;
using Sandbox.Configs;
using Sandbox.Modes;
using Sandbox.Network;

public sealed class GameMap : Component
{
	public static readonly Dictionary<string, Culture> Cultures = new();
	public static readonly Dictionary<string, Biome> Biomes = new();
	public static readonly Dictionary<Vector2, Province> Provinces = new();
	public static readonly Dictionary<string, Country> Countries = new();
	public static readonly Dictionary<string, Town> Towns = new();
	public static readonly Dictionary<string, Leader> Leaders = new();
	public static readonly Dictionary<Guid, Battle> Battles = new();

	public static Province SelectedProvince;
	public static Country SelectedCountry;

	public int Width;
	public int Height;

	public Color32[] RemapArr;
	public Texture PaletteTex;

	public Color32 PrevColor;
	public bool SelectAny = false;
	
	protected override void OnStart()
	{
		base.OnStart();

		try
		{
			CreateMap();
		}
		catch ( NullReferenceException )
		{
			if (SettingsMenu.Debug)
				Log.Warning( "Something wrong has happened with generating the map? I don't know actually :D" );
			return;
		}
		
		ConfigStructure.Load(FileSystem.Mounted.ReadJson<JsonObject>( "/data/structures.json" ), Settings.Structures);
		ConfigUnit.Load(FileSystem.Mounted.ReadJson<JsonObject>( "/data/units.json" ), Settings.Units);
		
		Culture.Load(FileSystem.Mounted.ReadJson<JsonObject>( "/data/culture.json" ), Cultures);
		Biome.Load(FileSystem.Mounted.ReadJson<JsonObject>( "/data/biomes.json" ), Biomes);
		Leader.Load(FileSystem.Mounted.ReadJson<JsonObject>( "/data/leaders.json" ), Leaders);
		Province.Load(FileSystem.Mounted.ReadJson<JsonObject>( "/data/provinces.json" ), Provinces);
		Town.Load(FileSystem.Mounted.ReadJson<JsonObject>( "/data/towns.json" ), Towns);
		Country.Load(FileSystem.Mounted.ReadJson<JsonObject>( "/data/countries.json" ), Countries);

		Battles.Clear();
		
		SelectedProvince = null;
		SelectedCountry = null;
		UnitMode.SelectedArmy = null;
		
		foreach (var country in Countries.Values)
		{
			foreach (var province in country.Provinces)
			{
				province.Country = country;
				PaletteTex.Update(country.Color, province.GetX(), province.GetY());
			}
		}

		if ( Networking.IsHost )
			return;
		
		try
		{
			var settings = Scene.Components.GetInDescendants<Settings>();
			var localPlayer = Scene.GetAllComponents<Player>()
				.First( player => player.Network.OwnerId == Connection.Local.Id );
			settings.CountryName = localPlayer.Country;
		}
		catch ( InvalidOperationException )
		{
			if (SettingsMenu.Debug)
				Log.Warning("We couldn't load the game properly");
		}
	}

	private void CreateMap()
	{
		var renderer = GameObject.Components.Get<ModelRenderer>();
		var material = renderer.MaterialOverride;
		var texture = material.GetTexture( "g_tMainColor" );
		var mainArr = texture.GetPixels();
		
		Width = texture.Width;
		Height = texture.Height;

		var main2Remap = new Dictionary<Color32, Color32>();
		RemapArr = new Color32[mainArr.Length];
		var idx = 0;
		for(var i = 0; i < mainArr.Length; i++){
			var mainColor = mainArr[i];
			if(!main2Remap.ContainsKey(mainColor)){
				var low = (byte)(idx % 256);
				var high = (byte)(idx / 256);
				main2Remap[mainColor] = new Color32(low, high, 0);
				idx++;
			}
			var remapColor = main2Remap[mainColor];
			RemapArr[i] = remapColor;
		}

		var paletteArr = new Color32[256*256];
		for(var i = 0; i < paletteArr.Length; i++){
			paletteArr[i] = new Color32(255, 255, 255);
		}
		
		var remapTex = Texture.Create( Width, Height, ImageFormat.RGBA8888_LINEAR ).Finish();
		remapTex.Update( RemapArr, 0, 0, Width, Height );
		material.Set("g_tRemapTex", remapTex);

		PaletteTex = Texture.Create( 256, 256, ImageFormat.Default ).Finish();
		PaletteTex.Update( paletteArr, 0, 0, 256, 256 );
		material.Set("g_tPaletteTex", PaletteTex);
	}

	protected override void OnUpdate()
	{
		foreach (var structure in Provinces.Values.SelectMany(province => province.Structures.Values))
		{
			structure.OnUpdate(this);
		}

		try
		{
			var gameUi = Scene.Components.GetInDescendants<GameUI>();
			gameUi.CurrentMode.Process(Scene);
		}
		catch ( NullReferenceException )
		{
			if (SettingsMenu.Debug)
				Log.Warning( "Something wrong has happened with updating the map? I don't know actually :D" );
		}
	}
	
	public Color32 GetColor(Color32 coord, bool hover)
	{
		if ( hover )
		{
			return new Color32( 50, 0, 255 );
		}

		if ( !Provinces.TryGetValue( new Vector2( coord.r, coord.g ), out var value ) )
		{
			return new Color32( 255, 255, 255 );
		}

		if ( value == SelectedProvince )
		{
			return new Color32( 255, 0, 255 );
		}

		var gameUi = Scene.Components.GetInDescendants<GameUI>();
		if ( gameUi.CurrentMode == IMode.ProvinceMode && SelectedCountry != null)
		{
			if ( SelectedCountry.Provinces.Contains( value ) )
			{
				return new Color32( 0, 255, 255 );	
			}

			if ( SelectedCountry.Relations[value.Country] == Relation.War )
			{
				return new Color32( 255, 0, 0 );	
			}
			
			if ( SelectedCountry.Relations[value.Country] == Relation.Ally )
			{
				return new Color32( 0, 255, 0 );	
			}

			return new Color32( 180, 180, 180 );
		}
		
		if ( gameUi.CurrentMode == IMode.CultureMode)
		{
			if ( value.Culture != null )
			{
				return value.Culture.Color;	
			}

			return new Color32( 180, 180, 180 );
		}

		if ( gameUi.CurrentMode == IMode.BuildMode )
		{
			if ( value.Country != Settings.PlayableCountry )
			{
				return new Color32( 180, 180, 180 );
			}

			if ( BuildMenu.SelectedStructure != null )
			{
				switch (BuildMenu.SelectedStructure.OnlyForTown)
				{
					case false when value.Town != null:
						return new Color32( 180, 180, 180 );
					case true when value.Town == null:
						return new Color32( 180, 180, 180 );
				}
			}
		}
				
		if ( value.Country != null )
		{
			return value.Country.Color;
		}

		return new Color32( 255, 255, 255 );
	}
	
	public void ChangeColor(Color32 remapColor, Color32 showColor){
		int xp = remapColor.r;
		int yp = remapColor.g;

		PaletteTex.Update(showColor, xp, yp);
	}

	public static IEnumerable<Structure> GetStructures()
	{
		return GameMap.Provinces.Values.SelectMany( province => province.Structures.Values );
	}
}
