using Sandbox;
using System;
using System.Linq;

namespace SpaceTest;

public partial class Pawn : DimensionalPhysicsEntity
{
	public static Dimension LocalDimension => (Local.Pawn as Pawn)?.Dimension;

	public CameraMode CameraMode {
		get => Components.Get<CameraMode>();
		set => Components.Add( value );
	}

	public Pawn()
	{

	}

	public Pawn( Dimension dimension ) : base( dimension )
	{

	}

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

		CameraMode = new TestCamera();
	}

	public override void BuildInput( InputBuilder inputBuilder )
	{
		if ( Local.Client.Components.Get<DevCamera>() is not null )
		{
			return;
		}
	}

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		Rotation = Input.Rotation;
		EyeRotation = Rotation;

		// build movement from the input values
		var movement = new Vector3( Input.Forward, Input.Left, 0 ).Normal;

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

		if ( IsClient && Input.Pressed( InputButton.Reload ) )
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
	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );

		// Update rotation every frame, to keep things smooth
		Rotation = Input.Rotation;
		EyeRotation = Rotation;

		if ( SceneModelManager is null ) return;
		SceneModelManager.SetTransform( Transform );
		//DebugOverlay.Sphere( Transform.Position, 5.0f, Color.Yellow, depthTest: false );
	}
}
