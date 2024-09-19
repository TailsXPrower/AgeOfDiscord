using System;

public class Animation : Component 
{
	protected override void OnEnabled()
	{
		base.OnEnabled();
		_clips.Clear();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		ProcessAnimations();
	}

	protected override void OnDisabled()
	{
		_clips.Clear();
		base.OnDisabled();
	}

	static Dictionary<GameObject, Dictionary<string, AnimationClip>> _clips = new();
	
	public static AnimationClip Play(AnimationClip clip) {
        if (HasAnimation(clip.GetGameObject(), clip.GetId())) {
            Stop(clip.GetGameObject(), clip.GetId());
        }

        var elementClips = GetElementClips(clip.GetGameObject());
        elementClips.Add(clip.GetId(), clip);
        clip.Tick();
        return clip;
    }

    public static AnimationClip Play(GameObject gameObject, string id, double duration, Func<double, double> easingFunc, Action<GameObject, float> consumer) {
        var clip = new AnimationClip(gameObject, Connection.Local.Id+id, duration, easingFunc, consumer);
        return Play(clip);
    }

    public static AnimationClip Play(GameObject gameObject, string id, double duration, Action<GameObject, float> consumer) {
        return Play(gameObject, id, duration, EasingFunc.EaseOutCubic, consumer);
    }

    public static void Stop(GameObject gameObject, string id) {
        RemoveClip(gameObject, id);
    }

    public static void Stop(GameObject gameObject) {
	    _clips.Remove(gameObject);
    }

    public static bool HasAnimation(GameObject gameObject, string id) {
	    _clips.TryGetValue( gameObject, out var elementClips );
        return elementClips?.ContainsKey(id) ?? false;
    }

    public static Dictionary<string, AnimationClip> GetElementClips(GameObject gameObject)
    {
	    _clips.TryGetValue( gameObject, out var elementClips );
	    if ( elementClips != null )
		    return elementClips;
	    
	    elementClips = new Dictionary<string, AnimationClip>();
        _clips.Add(gameObject, elementClips);

        return elementClips;
    }

    private static void RemoveClip(GameObject gameObject, string id) {
        var elementClips = GetElementClips(gameObject);
        if ( !elementClips.ContainsKey( id ) )
	        return;

        var clip = elementClips[id];
	    elementClips.Remove(id);
	    
        if (elementClips.Count <= 0) {
	        _clips.Remove(gameObject);
        }

        clip.Stopped();
    }

    protected override void OnDestroy()
    {
	    base.OnDestroy();
	    _clips.Clear();
    }

    public void ProcessAnimations() {
	    if ( _clips.Count <= 0 ) 
		    return;

	    if ( Scene == null )
	    {
		    _clips.Clear();
		    return;
	    }

	    Dictionary<GameObject, string> toRemove = new();
        var enumerator = _clips.Keys.ToList().GetEnumerator();

        while(enumerator.MoveNext()) {
            var gameObject = enumerator.Current;
            if ( gameObject == null )
				continue;

            var elementClips = _clips[gameObject];
            var enumeratorClip = new List<AnimationClip>(elementClips.Values).GetEnumerator();

            while(enumeratorClip.MoveNext()) {
	            var clip = enumeratorClip.Current;
	            if ( clip == null )
		            continue;
	            
	            if ( gameObject.Scene.Id != Scene.Id )
	            {
		            if (!toRemove.ContainsKey(gameObject))
			            toRemove.Add(gameObject, clip.GetId());
		            continue;
	            }

	            clip.Tick();
	            if (clip.IsCompleted()) {
		            if (!toRemove.ContainsKey(gameObject))
						toRemove.Add(gameObject, clip.GetId());
	            }
            }
            enumeratorClip.Dispose();
        }
        
        enumerator.Dispose();

        var enumeratorRemove = toRemove.GetEnumerator();

        while(enumeratorRemove.MoveNext()) {
	        var set = enumeratorRemove.Current;
	        var clips = GetElementClips( set.Key );

	        if ( !clips.TryGetValue(set.Value, out var clip) )
	        {
		        RemoveClip(set.Key, set.Value);
		        return;
	        }

	        if (clip != null && clip.IsCompleted()) {
                RemoveClip(set.Key, set.Value);
            }
        }

        enumeratorRemove.Dispose();
    }
}
