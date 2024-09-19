using Sandbox;

public sealed class MenuCamera : Component
{
	private TimeUntil nextMove = 0;
	private Vector3 Direction;
	
	protected override void OnUpdate()
	{
		if ( nextMove )
		{
			var minY = 39f;
			var maxY = 265f;
			var maxZ = 394f;
			var minZ = 95f;
			var random = new Random();
			nextMove = 6;
			GameObject.Transform.Position = new Vector3( 58.493f, minY + (maxY - minY) * random.NextSingle(),
				minZ + (maxZ - minZ) * random.NextSingle() );
			Direction = new Vector3( 58.493f, minY + (maxY - minY) * random.NextSingle(),
				minZ + (maxZ - minZ) * random.NextSingle() );

			var spotLight = GameObject.Components.GetInDescendants<SpotLight>();
			
			var origPos = GameObject.Transform.Position;
			Animation.Play( GameObject, "fade-in", 1, EasingFunc.Linear, ( o, progress ) =>
			{
				spotLight.Radius = 500 * progress;
			} );
			Animation.Play( GameObject, "camera", 6, EasingFunc.Linear, ( o, progress ) =>
			{
				o.Transform.Position = origPos.LerpTo( Direction, progress );

				if ( progress < 0.83f )
					return;

				var currentProgress = (progress - 0.83f) / 0.17f;
				spotLight.Radius = 500 * (1 - currentProgress);
			} );
		}
	}
}
