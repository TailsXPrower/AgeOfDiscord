using System;

public class AnimationClip
{
	GameObject _gameObject;
	string _id;
	float _startTime;
	float _duration;
	Func<double, double> _easingFunction;
	Action<GameObject, float> _consumer;
	List<Action<GameObject>> _onComplete = new();
	List<Action<GameObject>> _onStop = new();
	bool _isCompleted;
	bool _repeat;
	AnimationClip _after;

	public AnimationClip( GameObject gameObject, string id, double seconds, Func<double, double> easingFunction, Action<GameObject, float> consumer )
	{
		_gameObject = gameObject;
		_id = id;
		var endTime = (float)(_startTime + seconds);
		_duration = endTime - _startTime;
		_easingFunction = easingFunction;
		_consumer = consumer;
	}

	public string GetId()
	{
		return _id;
	}

	public GameObject GetGameObject()
	{
		return _gameObject;
	}

	public bool IsCompleted()
	{
		return _isCompleted;
	}

	public bool GetRepeat()
	{
		return _repeat;
	}

	public AnimationClip SetRepeat( bool repeat )
	{
		_repeat = repeat;
		return this;
	}

	public AnimationClip OnComplete( Action<GameObject> consumer )
	{
		_onComplete.Add( consumer );
		return this;
	}

	public AnimationClip OnStop( Action<GameObject> consumer )
	{
		_onStop.Add( consumer );
		return this;
	}

	public void Stopped()
	{
		foreach ( var action in _onStop )
		{
			action.Invoke( _gameObject );
		}
	}

	public AnimationClip PlayAfter( double duration, Action<GameObject, float> consumer )
	{
		_after = new AnimationClip( _gameObject, _id, duration, _easingFunction, consumer );
		return _after;
	}

	public AnimationClip PlayAfter( GameObject element, string id, double duration, Func<double, double> easingFunc, Action<GameObject, float> consumer )
	{
		_after = new AnimationClip( element, id, duration, easingFunc, consumer );
		return _after;
	}

	public AnimationClip PlayAfter( string id, double duration, Func<double, double> easingFunc, Action<GameObject, float> consumer )
	{
		_after = new AnimationClip( _gameObject, id, duration, easingFunc, consumer );
		return _after;
	}

	public AnimationClip PlayAfter( GameObject element, string id, double duration, Action<GameObject, float> consumer )
	{
		_after = new AnimationClip( element, id, duration, EasingFunc.EaseOutCubic, consumer );
		return _after;
	}

	public AnimationClip PlayAfter( string id, double duration, Action<GameObject, float> consumer )
	{
		_after = new AnimationClip( _gameObject, id, duration, EasingFunc.EaseOutCubic, consumer );
		return _after;
	}

	public void Tick()
	{
		if ( _isCompleted )
			return;

		var currentTime = Time.Now;
		if ( _startTime == 0f )
		{
			_startTime = currentTime;
		}

		var currentDuration = currentTime - _startTime;
		var progress = MathF.Min( currentDuration / _duration, 1.0f );
		if ( progress >= 1.0 )
		{
			_consumer.Invoke( _gameObject, 1.0f );
			var enumerator = _onComplete.GetEnumerator();

			while ( enumerator.MoveNext() )
			{
				var consumer = enumerator.Current;
				consumer?.Invoke( _gameObject );
			}

			enumerator.Dispose();

			if ( _repeat )
			{
				_startTime = currentTime;
			}
			else
			{
				_isCompleted = true;
				if ( _after != null )
				{
					Animation.Play( _after );
				}
			}
		}
		else
		{
			_consumer.Invoke( _gameObject, (float)_easingFunction.Invoke( progress ) );
		}
	}
}
