using Sandbox;
using Sandbox.UI;
using System;
using System.Linq;

namespace SpaceTest;

public partial class SpaceGame : Sandbox.Game
{
	public new static SpaceGame Current => Game.Current as SpaceGame;

	public static bool Debug = true;

	[Net] public DimensionManager DimensionManager { get; set; }

	[Net] public DimensionalPortal RotatyPortal { get; set; }

	public SpaceGame()
	{

	}

	public override async void Spawn()
	{
		if ( IsServer )
		{
			DimensionManager = new DimensionManager();

			var dimension1 = DimensionManager.NewDimension(); // new Dimension();
			var dimension2 = DimensionManager.NewDimension(); // new Dimension();
			var dimension3 = DimensionManager.NewDimension(); // new Dimension();
			var dimension4 = DimensionManager.NewDimension(); // new Dimension();

			await Util.AwaitCondition( () => DimensionManager is not null );
			await DimensionManager.WaitForDimensions();



			var box1 = new DimensionalModelEntity( dimension1 );
			box1.SetModel( "models/citizen_props/crate01.vmdl" );
			box1.Position = new Vector3( 100.0f, 50.0f, 0.0f );
			box1.Rotation = Rotation.Random;

			var box2 = new DimensionalModelEntity( dimension2 );
			box2.SetModel( "models/citizen_props/crate01.vmdl" );
			box2.Position = new Vector3( -110.0f, 10.0f, 120.0f );
			box2.Rotation = Rotation.Random;

			var box3 = new DimensionalModelEntity( dimension2 );
			box3.SetModel( "models/citizen_props/crate01.vmdl" );
			box3.Position = new Vector3( 150.0f, -50.0f, 0.0f );
			box3.Rotation = Rotation.Random;



			var light = new DimensionalLight( dimension1 );
			light.Position = Vector3.One * 50.0f;
			light.Radius = 500.0f;
			light.LinearAttenuation = 15.0f;
			light.QuadraticAttenuation = 0.0f;

			var light2 = new DimensionalLight( dimension2 );
			light2.Position = new Vector3( -250.0f, 50.0f, -150.0f );
			light2.Radius = 1500.0f;
			light2.LinearAttenuation = 20.0f;
			light2.QuadraticAttenuation = 0.0f;

			var light3 = new DimensionalLight( dimension2 );
			//light3.Position = Vector3.Right * 50.0f + Vector3.Forward * 25.0f + Vector3.Down * 30.0f;
			light3.Position = new Vector3( -440.0f, 600.0f, 96.0f );
			light3.Radius = 500.0f;
			light3.LinearAttenuation = 25.0f;
			light3.QuadraticAttenuation = 0.0f;
			light3.ColorTint = Color.Orange;



			var portal = DimensionManager.NewPortal( dimension1, dimension2 ); // new DimensionalPortal( dimension1, dimension2 );
			portal.Rotation = Rotation.FromYaw( -45.0f );
			portal.Position += portal.Rotation.Forward * 200.0f;

			var portal2 = DimensionManager.NewPortal( dimension3, dimension2 ); // new DimensionalPortal( dimension3, dimension2 );
			portal2.Position = new Vector3( 300.0f, 200.0f, 0.0f );
			portal2.Rotation = Rotation.FromYaw( -75.0f );

			var portal3 = DimensionManager.NewPortal( dimension4, dimension2 ); // new DimensionalPortal( dimension4, dimension2 );
			portal3.Position = new Vector3( -500.0f, 500.0f, 0.0f );
			portal3.Rotation = Rotation.FromYaw( -45.0f + 180.0f );

			RotatyPortal = DimensionManager.NewPortal( dimension3, dimension4 ); // new DimensionalPortal( dimension3, dimension4 );
			RotatyPortal.Position = new Vector3( -300.0f, -500.0f, 0.0f );
			RotatyPortal.Rotation = Rotation.Random;

			for ( int i = 0; i < 1000; i++ )
			{
				var newBox = new DimensionalModelEntity( dimension2 );
				newBox.SetModel( "models/citizen_props/crate01.vmdl" );

				Vector3 pos;

				pos = portal.Rotation.Backward * 750.0f + Vector3.Random.Normal * (400.0f);


				newBox.Rotation = Rotation.Random;
				newBox.Position = pos + (newBox.Model.RenderBounds.Size.z * 0.5f) * newBox.Rotation.Down;
			}

			//var citizen = new DimensionalModelEntity();
			//citizen.SetModel( "models/citizen/citizen.vmdl" );
			//citizen.Position = portal.Rotation.Backward * 750.0f;
			//citizen.Rotation = Rotation.LookAt( -citizen.Position.Normal, Vector3.Up );
			//Dimension2.AddAndInitEntity( citizen );
		}
	}

	public SpaceSky SpaceSky;

	Vector3 rotAxis = new Vector3( 1.0f, 2.0f, 3.0f).Normal;
	Vector3 originalPos = new Vector3( -300.0f, -500.0f, 0.0f );

	public override void ClientSpawn()
	{
		Map.Camera.AddHook( new DimensionBlitHook() );
	}

	[Event.Tick.Server]
	public void UpdateRotatyPortal()
	{
		rotAxis *= Rotation.RotateAroundAxis( Vector3.Up, Time.Delta * 30.0f );
		RotatyPortal.Rotation *= Rotation.RotateAroundAxis( rotAxis, Time.Delta * 30.0f );

		//var offset = new Vector3( MathF.Sin( Time.Now / 3.5f ) * 250.0f, MathF.Cos( Time.Now / 3.5f ) * 250.0f, MathF.Sin( Time.Now / 4.5f ) * 75.0f );
		var offset = new Vector3( MathF.Sin( Time.Now / 3.5f ) * 250.0f, MathF.Cos( Time.Now / 3.5f ) * 250.0f, 0.0f );

		//RotatyPortal.Rotation = Rotation.LookAt( offset.Normal, Vector3.Up ) * Rotation.FromYaw( 90.0f );
		RotatyPortal.Position = originalPos + offset;
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override async void ClientJoined( Client client )
	{
		base.ClientJoined( client );

		await DimensionManager.WaitForDimensions();

		var dimension = DimensionManager.Dimensions.First();

		// Create a pawn for this client to play with
		var pawn = new Pawn( dimension );
		client.Pawn = pawn;
		pawn.Position = new Vector3( 300.0f, 0, 0 );

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}
	}

	[ConCmd.Server]
	public static void RespawnPlayer()
	{
		if ( ConsoleSystem.Caller?.Pawn is Pawn pawn )
		{
			pawn.Respawn();
		}
	}

	[ConCmd.Server]
	public static void changefov( float fov )
	{
		if ( ConsoleSystem.Caller?.Pawn is Pawn pawn )
		{
			pawn.CameraMode.FieldOfView = fov;
		}
	}
}





/*

[Net] ModelEntity TheObject { get; set; }

public override void Spawn()
{
	if ( Host.IsServer )
	{

		var light = new PointLightEntity();
		light.Position = Vector3.Zero;
		light.Brightness = 0.25f;

		_ = new PostProcessingEntity()
		{
			PostProcessingFile = "postprocessing/spacetest.vpost"
		};

		//TheObject = new ModelEntity( "models/citizen_props/crate01.vmdl" );
		//TheObject.Position = new Vector3( 100.0f, 0, 0 );

		var light2 = new PointLightEntity();
		light2.Position = new Vector3( 75.0f, 150.0f, 75.0f );
		light2.Brightness = 0.25f;
	}

}

public SpaceSky SpaceSky;
AlternateDimension AlternateDimension;

public override void ClientSpawn()
{
	if ( Host.IsClient )
	{
		SpaceSky = new SpaceSky( Map.Scene );
		AlternateDimension = new AlternateDimension();
		Map.Camera.AddHook( AlternateDimension );

		var model = Model.Load( "models/citizen_props/crate01.vmdl" );
		var bsize = model.Bounds.Size.Length;

		for (int i = 0; i < 100; i++ )
		{
			Vector3 pos;

			do
			{
				pos = Vector3.Random * 1000.0f;
			}
			while ( pos.Distance( Vector3.Zero ) < AlternateDimension.MaxSize + bsize );

			var rot = Rotation.Random;
			var scl = Rand.Float( 0.5f, 2.5f );
			var m = new SceneModel( Map.Scene, model, new Transform( pos, rot, scl ) );
			m.RenderingEnabled = false;
			AlternateDimension.Register( m );
		}
	}
}

*/
