using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;

public partial class DimensionalPortal : Entity, IDimensionalEntity
{
	public Dimension Dimension { get; set; }

	[Net] public Dimension Dimension1 { get; set; }
	[Net] public Dimension Dimension2 { get; set; }

	public Dimension GetOtherDimension( Dimension dimension )
	{
		if ( dimension == Dimension1 )
			return Dimension2;
		else if ( dimension == Dimension2 )
			return Dimension1;
		else
			return null;
	}
	public bool IsConnected( Dimension dimension )
	{
		return dimension == Dimension1 || dimension == Dimension2;
	}

	public bool IsFirstDimension( Dimension dimension )
	{
		return dimension == Dimension1;
	}

	public SceneDimensionalPortal GetScenePortalForDimension( Dimension dimension )
	{
		if ( dimension == Dimension1 )
			return _scenePortal1;
		else if ( dimension == Dimension2 )
			return _scenePortal2;
		else
			return null;
	}

	public DimensionalPortal()
	{

	}

	public DimensionalPortal( Dimension dimension1, Dimension dimension2 ) : this()
	{
		Dimension1 = dimension1;
		Dimension2 = dimension2;
	}

	public override void Spawn()
	{
		Transmit = TransmitType.Always;
		Predictable = true;

		Size = new Vector2( 75.0f );
	}

	public bool IsInFront( Dimension dimension, Vector3 position )
	{
		if ( dimension != Dimension1 && dimension != Dimension2 )
			return false;

		var relative = position - Position;
		var dot = relative.Dot( Rotation.Forward );

		bool flip = dimension == Dimension1; // it has to be one of our dimensions anyways, this works
		float flipdot = flip ? dot : -dot;
		return flipdot > 0.0f;
	}

	public override void ClientSpawn()
	{
		InitSceneObject();
	}

	public SceneDimensionalPortal _scenePortal1;
	public SceneDimensionalPortal _scenePortal2;

	public async void InitSceneObject()
	{
		await Util.AwaitCondition( () => Dimension1 is not null && Dimension2 is not null );
		var dim1 = Dimension1.AwaitReady();
		var dim2 = Dimension2.AwaitReady();
		await Task.WhenAll( dim1, dim2 );

		UpdateMeshAndBounds();

		_scenePortal1 = new SceneDimensionalPortal( 0, 75.0f, this, Dimension1, Dimension2 );
		_scenePortal2 = new SceneDimensionalPortal( 1, 75.0f, this, Dimension2, Dimension1 );

		ResetRendering();
	}

	public bool DoneRendering { get; private set; } = false;

	public void ResetRendering()
	{
		DoneRendering = false;
		if ( _scenePortal1 is null || _scenePortal2 is null ) return;
		_scenePortal1.ResetDoneRendering();
		_scenePortal2.ResetDoneRendering();
	}

	public void SetDoneRendering()
	{
		DoneRendering = true;
	}

	[Event.Tick.Server]
	private void Update()
	{
		float rate = 0.1f;
		var portalNormal = new Vector3( MathF.Sin( Time.Now * rate ), MathF.Cos( Time.Now * rate ), 0.0f ).Normal;

		//Position = new Vector3( 0.0f, 0.0f, 0.0f );
		//Rotation = Rotation.LookAt( portalNormal, Vector3.Up );
	}

	[Event.Frame]
	private void OnFrame()
	{
		if ( _scenePortal1 is null || _scenePortal2 is null ) return;

		//DebugOverlay.Sphere( Position, 5.0f, Color.Green, depthTest: false );
		UpdateMeshAndBounds();

		_scenePortal1.Transform = Transform;
		_scenePortal1.Bounds = Bounds;
		_scenePortal2.Transform = Transform;
		_scenePortal2.Bounds = Bounds;
	}

	public Vector3[] VertexPositions;
	public BBox Bounds;
	[Net] public Vector2 Size { get; set; }

	private void UpdateVertices()
	{
		VertexPositions = new Vector3[]
		{
			Size.x * Transform.Rotation.Right  + Size.y * Transform.Rotation.Up,
			Size.x * Transform.Rotation.Left   + Size.y * Transform.Rotation.Up,
			Size.x * Transform.Rotation.Left   + Size.y * Transform.Rotation.Down,

			Size.x * Transform.Rotation.Left   + Size.y * Transform.Rotation.Down,
			Size.x * Transform.Rotation.Right  + Size.y * Transform.Rotation.Down,
			Size.x * Transform.Rotation.Right  + Size.y * Transform.Rotation.Up,
		};
	}

	public void UpdateMeshAndBounds()
	{
		UpdateVertices();

		Bounds = VertexPositions.GetAABB( offset: Transform.Position );
		
		if ( IsConnected( Pawn.LocalDimension ) && SpaceGame.Debug )
		{
			DebugOverlay.Line( Position + VertexPositions[0], Position + VertexPositions[1], Color.Green, depthTest: false );
			DebugOverlay.Line( Position + VertexPositions[0], Position + VertexPositions[4], Color.Green, depthTest: false );
			DebugOverlay.Line( Position + VertexPositions[2], Position + VertexPositions[3], Color.Green, depthTest: false );
			DebugOverlay.Line( Position + VertexPositions[3], Position + VertexPositions[4], Color.Green, depthTest: false );
			DebugOverlay.Box( Bounds.Mins, Bounds.Maxs, Color.Yellow.WithAlpha( 0.5f ), depthTest: false );
		}
	}
}



public partial class SceneDimensionalPortal : SceneCustomObject
{
	int _id;
	DimensionalPortal _parent;
	private Dimension _dimension;
	private Dimension _targetDimension;
	private Material PortalBlit;
	private Vector2 _size;

	private Vector3[] _vertexPositions;

	private readonly object RenderLock = new object();

	private int rendercount = 0;
	private bool _doneRendering;

	public void SetDoneRendering()
	{
		lock ( RenderLock )
		{
			_doneRendering = true;
		}
	}

	public void ResetDoneRendering()
	{
		lock ( RenderLock )
		{
			rendercount = 0;
			_doneRendering = false;
		}
	}


	public SceneDimensionalPortal( int id, Vector2 size, DimensionalPortal parent, Dimension dimension, Dimension targetDimension ) : base( dimension.SceneWorld )
	{
		_id = id;
		_size = size;
		_parent = parent;
		_dimension = dimension;
		_targetDimension = targetDimension;
		PortalBlit = Material.Load( "materials/portalblit.vmat" );

		Flags.IsOpaque = true;
		Flags.IsTranslucent = false;
	}

	public override void RenderSceneObject()
	{
		if ( Graphics.LayerType != SceneLayerType.Opaque )
			return;


		if ( _doneRendering )
			return;

		_doneRendering = true;

		// TODO: Floating origin

		var portalOrigin = Transform.Position;

		var portalPlane = new Plane(
			portalOrigin,
			Transform.Rotation.Forward
		);

		_targetDimension.UpdateSceneCameraFromGlobal();

		var vertices = _parent.VertexPositions.Select( v => new Vertex( portalOrigin + v, Vector4.Zero, Color32.White ) ).ToArray();

		Vector4 clip = new Vector4( portalPlane.Normal, portalPlane.Distance );

		bool flip = _id == 0;
		bool front = _parent.IsInFront( _dimension, CurrentView.Position );

		var materialAttributes = new RenderAttributes();

		// Log.Info( $"{_id} {this} {_parent.NetworkIdent}, {flip}" );

		if ( _dimension == Pawn.LocalDimension && SpaceGame.Debug )
			DebugOverlay.Line( Position, Position + Rotation.Forward * 50.0f * (flip ? 1.0f : -1.0f), Color.Cyan, depthTest: false );

		if ( !front )
		{
			return;
		}

		if ( flip )
		{
			clip = -clip;
		}

		if ( !_parent.IsConnected( Pawn.LocalDimension ) )
		{
			//return;
		}

		//DebugOverlay.Sphere( Vector3.Zero, 5.0f, Color.Red, depthTest: false );
		//DebugOverlay.Line( portalPlane.Normal * portalPlane.Distance, portalPlane.Normal * portalPlane.Distance + portalPlane.Normal * 100.0f, Color.Cyan, depthTest: false );

		_targetDimension.SceneCamera.Attributes.SetCombo( "D_ENABLE_USER_CLIP_PLANE", true );
		_targetDimension.SceneCamera.Attributes.Set( "EnableClipPlane", true );
		_targetDimension.SceneCamera.Attributes.Set( "ClipPlane0", clip );

		using var renderTarget = RenderTarget.GetTemporary();

		Graphics.RenderTarget = renderTarget;
		Graphics.Clear( false, true );

		Graphics.RenderToTexture( _targetDimension.SceneCamera, renderTarget.ColorTarget );

		//Graphics.Clear( false, true );

		Graphics.RenderTarget = null;
		
		_targetDimension.SceneCamera.Attributes.Clear();

		var attributes = new RenderAttributes();
		attributes.SetCombo( "D_FRONT", flip );
		attributes.Set( "ColorBuffer", renderTarget.ColorTarget );

		Graphics.Draw( vertices, vertices.Length, PortalBlit, attributes );

		rendercount++;
	}
}





/*
public partial class DimensionalPortalRenderHook : RenderHook
{
	private static Material PortalBlit = Material.Load( "materials/portalblit.vmat" );
	Dimension _dimension;

	public DimensionalPortalRenderHook( Dimension dimension )
	{
		_dimension = dimension;
	}

	public override void OnFrame( SceneCamera target )
	{
		_dimension.UpdateSceneCameraFromGlobal();
	}

	public override void OnStage( SceneCamera target, Stage renderStage )
	{
		Log.Info( $"Start frame Dimension {_dimension}" );

		if ( renderStage == Stage.AfterTransparent )
		{
			var connectedPortals = Entity.All.OfType<DimensionalPortal>()
				.Where( p => p.IsConnected( _dimension ) )
				.OrderBy( p => p.Position.Distance( target.Position ) );

			foreach ( var portal in connectedPortals )
			{
				RenderPortal( target, portal );
			}
		}
	}

	private void RenderPortal( SceneCamera target, DimensionalPortal portal )
	{
		if ( portal.DoneRendering )
			return;

		portal.SetDoneRendering();

		// TODO: Floating origin

		var portalPlane = new Plane(
			portal.Position,
			portal.Rotation.Forward
		);

		var portalRotation = Rotation.LookAt( portalPlane.Normal, Vector3.Up );

		var vertices = portal.VertexPositions.Select( v => new Vertex( portal.Position + v, Vector4.Zero, Color32.White ) ).ToArray();

		var relative = target.Position - portal.Position;
		var dot = relative.Dot( portalPlane.Normal );

		Vector4 clip = new Vector4( portalPlane.Normal, portalPlane.Distance );
		bool flip = portal.IsFirstDimension( _dimension );
		float flipdot = flip ? dot : -dot;
		bool front = flipdot > 0.0f;

		var materialAttributes = new RenderAttributes();

		if ( !front )
		{
			return;
		}

		if ( flip )
		{
			clip = -clip;
		}

		DebugOverlay.Sphere( Vector3.Zero, 5.0f, Color.Red, depthTest: false );
		DebugOverlay.Line( portalPlane.Normal * portalPlane.Distance, portalPlane.Normal * portalPlane.Distance + portalPlane.Normal * 100.0f, Color.Cyan, depthTest: false );

		target.Attributes.SetCombo( "D_ENABLE_USER_CLIP_PLANE", true );
		target.Attributes.Set( "EnableClipPlane", true );
		target.Attributes.Set( "ClipPlane0", clip );

		using var renderTarget = RenderTarget.GetTemporary();

		Graphics.RenderTarget = renderTarget;
		Graphics.Clear( false, true );

		var camera = portal.GetOtherDimension( _dimension ).SceneCamera;
		Graphics.RenderToTexture( camera, renderTarget.ColorTarget );

		Graphics.Clear( false, true );

		Graphics.RenderTarget = null;

		target.Attributes.Clear();

		Graphics.Attributes.Set( "ColorBuffer", renderTarget.ColorTarget );

		var recty = new Rect( Vector2.Zero, new Vector2( 1.0f, 1.0f ) );

		Graphics.Draw( vertices, vertices.Length, PortalBlit );

		var flipstring = flip ? "First" : "Second";
		Log.Info( $"{flipstring} {portal}" );
	}
}*/
