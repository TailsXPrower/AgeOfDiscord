using Sandbox.Utils;

namespace Sandbox.GameData;

public class Battle
{
	public Guid Id;
	public string Name;
	
	public Army Aggressor;
	public Army Defender;

	public float AttackChance;
	public float DefenseChance;

	public Battle( Guid id, Army aggressor, Army defender )
	{
		Id = id;
		Aggressor = aggressor;
		Defender = defender;

		Name = $"Battle of {Defender.Province.Name}";

		Aggressor.CurrentBattle = this;
		Defender.CurrentBattle = this;
		
		GameMap.Battles.Add(id, this);
		
		CalculateChances();
	}

	[Broadcast]
	public static void NewBattle(Guid id, string countryAggressor, Guid armyAggressor, 
		string countryDefender, Guid armyDefender)
	{
		var aggressor = GameMap.Countries[countryAggressor];
		
		if ( !aggressor.Armies.TryGetValue(armyAggressor, out var aggressorArmy) )
		{
			return;
		}
		
		var defender = GameMap.Countries[countryDefender];
		
		if ( !defender.Armies.TryGetValue(armyDefender, out var defenderArmy) )
		{
			return;
		}
		
		_ = new Battle( id, aggressorArmy, defenderArmy );
	}

	public void Progress(Scene scene)
	{
		var chance = new Random().Next(100);
		var punched = chance < AttackChance * 100;

		var gameMap = scene.Components.GetInDescendants<GameMap>();
		var collision = gameMap.GameObject.Components.Get<PlaneCollider>();
		
		if ( punched )
		{
			var damage = Aggressor.GetDamage() * AttackChance;
			Army.Damage(Defender.Country.Name, Defender.Id, damage);
			Logger.Error(gameMap,  collision.Transform.Local.PointToWorld( Defender.Province.Center ), $"-{damage:0} HP");
		}
		else
		{
			var damage = Defender.GetDamage() * DefenseChance;
			Army.Damage(Aggressor.Country.Name, Aggressor.Id, damage);
			Logger.Error(gameMap, collision.Transform.Local.PointToWorld( Aggressor.Province.Center ), $"-{damage:0} HP");
		}

		if ( Defender.Health > 0 )
		{
			var healed = Defender.Units.Sum( unit => unit.Config.Healer && unit.Health > 0 ? 20 : 0 );

			if ( healed > 0 )
			{
				Army.Heal(Defender.Country.Name, Defender.Id, healed);
				 //Logger.Error(gameMap, collision.Transform.Local.PointToWorld( Defender.Province.Center ), $"+{healed} HP");
			}
		}
		
		if ( Aggressor.Health > 0 )
		{
			var healed = Aggressor.Units.Sum( unit => unit.Config.Healer && unit.Health > 0 ? 20 : 0 );

			if ( healed > 0 )
			{
				Army.Heal(Defender.Country.Name, Defender.Id, healed);
				//Logger.Error(gameMap, collision.Transform.Local.PointToWorld( Aggressor.Province.Center ), $"+{healed} HP");
			}
		}
		
		CalculateChances(Id);
		
		if ( Defender.Health == 0 || Aggressor.Health == 0 )
		{
			BattleEnd(scene.Components.GetInDescendants<GameMap>(), Id);
		}
	}

	[Broadcast]
	public static void BattleEnd(GameMap gameMap, Guid battleId)
	{
		if (!GameMap.Battles.TryGetValue(battleId, out var battle))
			return;

		battle.Defender.CurrentBattle = null;
		battle.Aggressor.CurrentBattle = null;
		GameMap.Battles.Remove( battleId );

		if ( battle.Defender.Health > 0 )
		{
			if ( battle.Defender.Movement.DestinationProvince != null )
			{
				battle.Defender.Movement.IsMoving = true;
				battle.Defender.Movement.Check( gameMap );
			}
		}
		
		if ( battle.Aggressor.Health > 0 )
		{
			if ( battle.Aggressor.Movement.DestinationProvince != null )
			{
				battle.Aggressor.Movement.IsMoving = true;
				battle.Aggressor.Movement.Check( gameMap );
			}
		}
	}
	
	[Broadcast]
	public static void CalculateChances(Guid id)
	{
		GameMap.Battles[id].CalculateChances();
	}
	
	public void CalculateChances()
	{
		float maxChance = Aggressor.GetDamage() + Defender.GetDefense();
		AttackChance = Aggressor.GetDamage() / maxChance;
		DefenseChance = Defender.GetDefense() / maxChance;
	}
}
