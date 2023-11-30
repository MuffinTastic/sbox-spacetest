using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;

public partial class DimensionalPortal : DimensionalEntity
{
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
		// overriding because the base ClientSpawn awaits on Dimension
		// which will always be null for a portal

		InitSceneObject();
	}

	public SceneDimensionalPortal _scenePortal1;
	public SceneDimensionalPortal _scenePortal2;

	public override async void InitSceneObject()
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

	[Event.Client.Frame]
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
			//DebugOverlay.Box( Bounds.Mins, Bounds.Maxs, Color.Yellow.WithAlpha( 0.5f ), depthTest: false );
			DebugOverlay.Line( Position + VertexPositions[0], Position + VertexPositions[1], Color.Green, depthTest: false );
			DebugOverlay.Line( Position + VertexPositions[0], Position + VertexPositions[4], Color.Green, depthTest: false );
			DebugOverlay.Line( Position + VertexPositions[1], Position + VertexPositions[2], Color.Green, depthTest: false );
			DebugOverlay.Line( Position + VertexPositions[3], Position + VertexPositions[4], Color.Green, depthTest: false );
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
	private RenderTarget rt;

	private readonly object RenderLock = new object();

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
			_doneRendering = false;
		}
	}


	public SceneDimensionalPortal( int id, Vector2 size, DimensionalPortal parent, Dimension dimension, Dimension targetDimension ) : base( dimension.SceneWorld )
	{
		_id = id;
		_parent = parent;
		_dimension = dimension;
		_targetDimension = targetDimension;
		PortalBlit = Material.Load( "materials/portalblit.vmat" ).CreateCopy();

		Batchable = false;

		Flags.IsOpaque = true;
		Flags.IsTranslucent = false;

		rt = RenderTarget.From( Texture.CreateRenderTarget( "wtfe", ImageFormat.Default, Screen.Size ) );
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
		bool front = _parent.IsInFront( _dimension, Camera.Main.Position );

		var materialAttributes = new RenderAttributes();

		if ( _dimension == Pawn.LocalDimension && SpaceGame.Debug )
		{
			DebugOverlay.Line( portalOrigin, portalOrigin + portalPlane.Distance * 50.0f, Color.Orange, depthTest: false );
			DebugOverlay.Line( Position, Position + Rotation.Forward * 50.0f * (flip ? 1.0f : -1.0f), Color.Cyan, depthTest: false );
		}

		if ( !front )
		{
			return;
		}

		if ( flip )
		{
			clip = -clip;
		}

		_targetDimension.SceneCamera.Attributes.SetCombo( "D_ENABLE_USER_CLIP_PLANE", true );
		_targetDimension.SceneCamera.Attributes.Set( "EnableClipPlane", true );
		_targetDimension.SceneCamera.Attributes.Set( "ClipPlane0", clip );

		Graphics.RenderTarget = rt;
		Graphics.Clear( false, false );

		Graphics.RenderToTexture( _targetDimension.SceneCamera, rt.ColorTarget );

		Graphics.RenderTarget = null;

		var attributes = new RenderAttributes();
		attributes.SetCombo( "D_FRONT", flip );
		attributes.Set( "ColorBuffer", rt.ColorTarget );

		Graphics.Draw( vertices, vertices.Length, PortalBlit, attributes );

		_targetDimension.SceneCamera.Attributes.Clear();
	}
}
