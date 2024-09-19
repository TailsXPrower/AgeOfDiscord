using System;

namespace Sandbox;

public class TimeComponent : Component
{
	[HostSync] 
	[Property]
	public long CurrentTime { get; private set; }

	[HostSync] 
	[Property]
	public long Speed { get; private set; } = 10000;

	[HostSync] 
	[Property]
	public bool Pause { get; set; } = true;
	
	[HostSync] 
	[Property]
	public int CurrentSpeed { get; private set; } = 1;

	protected override void OnStart()
	{
		CurrentTime = 0;
		ChangeSpeed( 1 );
	}

	protected override void OnFixedUpdate()
	{
		if ( Input.Pressed( "Time" ) )
		{
			Pause = !Pause;
		}
		
		if (Pause)
			return;
		
		CurrentTime += Speed;
	}

	public void ChangeSpeed( int speed )
	{
		CurrentSpeed = speed;
		switch (speed)
		{
			case 1:
				Speed = 1000;
				break;
			case 2:
				Speed = 5000;
				break;
			case 3:
				Speed = 10000;
				break;
			case 4:
				Speed = 100000;
				break;
			case 5:
				Speed = 1000000;
				break;
		}
	}
	
	public bool IsMonthTicked()
	{
		if ( Pause )
			return false;
		
		var date = new DateTime( 1129, 1, 1 );
		date += TimeSpan.FromMilliseconds(CurrentTime - Speed);
		return GetDate().Month != date.Month;
	}
	
	public bool IsWeekTicked()
	{
		if ( Pause )
			return false;
		
		var date = new DateTime( 1129, 1, 1 );
		date += TimeSpan.FromMilliseconds(CurrentTime - Speed);
		return date.DayOfWeek == DayOfWeek.Sunday && GetDate().DayOfWeek == DayOfWeek.Monday;
	}
	
	public bool IsDayTicked()
	{
		if ( Pause )
			return false;
		
		var date = new DateTime( 1129, 1, 1 );
		date += TimeSpan.FromMilliseconds(CurrentTime - Speed);
		return GetDate().Day != date.Day;
	}

	public bool IsHourTicked()
	{
		if ( Pause )
			return false;
		
		var date = new DateTime( 1129, 1, 1 );
		date += TimeSpan.FromMilliseconds(CurrentTime - Speed);
		return GetDate().Hour != date.Hour;
	}

	public DateTime GetDate()
	{
		var date = new DateTime( 1129, 1, 1 );
		date += TimeSpan.FromMilliseconds(CurrentTime);
		return date;
	}

	public string GetFormattedTime()
	{
		var date = new DateTime( 1129, 1, 1 );
		date += TimeSpan.FromMilliseconds(CurrentTime);
		return date.ToString( "yyyy'-'MM'-'dd' 'HH':'mm" );
	}
	
	public string GetFormattedTime(long time)
	{
		var date = new DateTime( 1129, 1, 1 );
		date += TimeSpan.FromMilliseconds(time);
		return date.ToString( "yyyy'-'MM'-'dd' 'HH':'mm" );
	}
}
