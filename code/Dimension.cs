using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;

public partial class Dimension : Entity
{
	[Net] public IList<DimensionalEntity> Entities { get; private set; }

	// purposefully not networked
	public bool Ready { get; private set; } = false;

	public override void Spawn()
	{
		Transmit = TransmitType.Always;

		base.Spawn();

		Ready = true;
	}


	// Client-Only

	public SceneWorld SceneWorld { get; private set; }
	public SceneCamera SceneCamera { get; private set; }
	public SpaceSky SpaceSky { get; private set; }

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		SceneWorld = new SceneWorld();
		
		SceneCamera = new SceneCamera( $"Dimension {NetworkIdent}" );
		SceneCamera.World = SceneWorld;
		SceneCamera.EnablePostProcessing = false;

		SpaceSky = new SpaceSky( SceneWorld );

		Ready = true;
	}

	private bool SceneWorldValid => IsServer ? true : SceneWorld.IsValid();

	public async Task AwaitReady()
	{
		do
		{
			await GameTask.Delay( 50 );
		}
		while ( !(Ready && SceneWorldValid) );
	}

	public void UpdateSceneCameraFromGlobal()
	{
		SceneCamera.Position = Map.Camera.Position;
		SceneCamera.Rotation = Map.Camera.Rotation;
		// See: PerformS1FovHack
		SceneCamera.FieldOfView = MathF.Atan( MathF.Tan( Map.Camera.FieldOfView.DegreeToRadian() * 0.5f ) * (Screen.Aspect * 0.75f) ).RadianToDegree() * 2.0f;
		SceneCamera.ZFar = Map.Camera.ZFar;
		SceneCamera.ZNear = Map.Camera.ZNear;
	}
}
