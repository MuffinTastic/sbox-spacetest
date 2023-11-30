using Sandbox;
using System;
using System.Linq;

namespace SpaceTest;

public partial class Pawn : DimensionalPhysicsEntity
{
	public static Dimension LocalDimension => (Game.LocalPawn as Pawn)?.Dimension;

	public Pawn()
	{

	}

	public Pawn( Dimension dimension ) : base( dimension )
	{

	}

	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		//
		// Use a watermelon model
		//
		SetModel( "models/citizen_props/crate01.vmdl" );

		base.Spawn();
		Respawn();
	}
	public void Respawn()
	{
		EnableShadowInFirstPerson = true;
	}

	public override void BuildInput()
	{
		if ( Game.LocalClient.Components.Get<DevCamera>() is not null )
		{
			return;
		}

		InputDirection = Input.AnalogMove;

		var look = Input.AnalogLook;

		var viewAngles = ViewAngles;
		viewAngles += look;
		ViewAngles = viewAngles.Normal;
	}

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		Rotation = ViewAngles.ToRotation();

		// build movement from the input values
		var movement = InputDirection.Normal;

		// rotate it to the direction we're facing
		Velocity = Rotation * movement;

		// apply some speed to it
		Velocity *= Input.Down( InputButton.Run ) ? 1000 : 200;

		// apply it to our position using MoveHelper, which handles collision
		// detection and sliding across surfaces for us
		MoveHelper helper = new MoveHelper( Position, Velocity );
		helper.Trace = helper.Trace.Size( 16 );
		if ( helper.TryMove( Time.Delta ) > 0 )
		{
			Position = helper.Position;
		}

		if ( Game.IsClient && Input.Pressed( InputButton.Reload ) )
		{
			SpaceSky.RandomizeSkies();
		}

		if ( Input.Pressed( InputButton.Use ) )
		{
			SpaceGame.Debug = !SpaceGame.Debug;
		}
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		Camera.Main.Rotation = ViewAngles.ToRotation();
		Camera.Main.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
		Camera.Main.FirstPersonViewer = null;

		Vector3 targetPos;

		var center = Position + Vector3.Up * 64;

		var pos = center;
		var rot = Camera.Rotation;

		float distance = 80.0f * Scale;
		targetPos = pos + rot.Right * ((Model.RenderBounds.Mins.x + 32) * Scale / 2);
		targetPos += rot.Forward * -distance;

		Camera.Main.Position = targetPos;

		if ( SceneModelManager is null ) return;
		SceneModelManager.SetTransform( Transform );
	}
}
