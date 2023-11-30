using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceTest;

public class AlternateDimension : RenderHook
{
	SceneWorld World;
	SceneCamera SceneCamera;
	SpaceSky Sky;
	Material BlitMat;

	List<SceneObject> CustomObjects = new();

	public const float BaseSize = 75.0f;
	public const float MaxSize = BaseSize * 3.0f;

	public AlternateDimension()
	{
		World = new SceneWorld();
		Sky = new SpaceSky( World );
		SceneCamera = new SceneCamera( "Alternate Dimension" );
		SceneCamera.EnablePostProcessing = false;
		SceneCamera.World = World;
		BlitMat = Material.Load( "materials/portalblit.vmat" );
	}

	public void Register(SceneObject obj) => CustomObjects.Add( obj );

	public override void OnFrame( SceneCamera target )
	{
		SceneCamera.Position = Camera.Main.Position;
		SceneCamera.Rotation = Camera.Main.Rotation;
		SceneCamera.FieldOfView = Camera.Main.FieldOfView;
		SceneCamera.ZFar = Camera.Main.ZFar;
		SceneCamera.ZNear = Camera.Main.ZNear;
	}

	// this hook is attached to the main camera 
	public override void OnStage( SceneCamera target, Stage renderStage )
	{
		if ( renderStage == Stage.AfterTransparent ) // as a test
		{
			float rate = 0.1f;
			float portalSize = MathF.Sin( Time.Now / 1.25f ) + 2.0f;
			var portalOrigin = new Vector3( 100.0f, 0.0f, 0.0f );
			var portalNormal = new Vector3( MathF.Sin( Time.Now * rate ), MathF.Cos( Time.Now * rate ), 0.0f ).Normal;
			//var portalNormal = Camera.Rotation.Forward;

			var portalPlane = new Plane(
				portalOrigin,
				portalNormal
			);

			var portalRotation = Rotation.LookAt( portalPlane.Normal, Vector3.Up );

			var locations = new Vector3[]
			{
				portalRotation.Right  + portalRotation.Up,
				portalRotation.Left   + portalRotation.Up,
				portalRotation.Left   + portalRotation.Down,

				portalRotation.Left   + portalRotation.Down,
				portalRotation.Right  + portalRotation.Down,
				portalRotation.Right  + portalRotation.Up,
			};

			var vertices = locations.Select( v => new Vertex( portalOrigin + v * MaxSize, Vector4.Zero, Color32.White ) ).ToArray();

			var relative = SceneCamera.Position - portalOrigin;
			var dot = relative.Dot( portalPlane.Normal );

			Vector4 clip = new Vector4( portalPlane.Normal, portalPlane.Distance );
			if ( dot > 0.0f )
				clip = -clip;

			// DebugOverlay.Sphere( portalOrigin, 5.0f, Color.Red, depthTest: false );
			// DebugOverlay.Line( Vector3.Zero, portalOrigin, Color.Red, depthTest: false );

			Graphics.Attributes.SetCombo( "D_ENABLE_USER_CLIP_PLANE", true );
			Graphics.Attributes.Set( "EnableClipPlane", true );
			Graphics.Attributes.Set( "ClipPlane0", clip );

			using var renderTarget = RenderTarget.GetTemporary();

			Graphics.RenderTarget = renderTarget;
			Graphics.Clear( false, true );
			/*
			foreach ( var ent in Entity.All )
			{
				if ( ent is not ModelEntity modelEnt ) continue;
				if ( !modelEnt.EnableDrawing ) continue;
				var so = modelEnt.SceneObject;

				if ( !so.IsValid() ) continue;

				Graphics.Render( so );
			}

			foreach ( var so in CustomObjects )
			{
				if ( !so.IsValid() ) continue;

				Graphics.Render( so );
			}*/

			// draw skybox last to avoid overdraw
			Graphics.RenderToTexture( SceneCamera, renderTarget.ColorTarget );
			//Graphics.Render( Sky.SkyBox );

			Graphics.RenderTarget = null;

			var recty = new Rect( Vector2.Zero, new Vector2( 1.0f, -1.0f ) );
			var materialAttributes = new RenderAttributes();
			Graphics.Attributes.Set( "ColorBuffer", renderTarget.ColorTarget );
			Graphics.Draw( vertices, vertices.Length, BlitMat, materialAttributes );
		}
	}

	private void SetClipPlane( Plane clipPlane, ref RenderAttributes attributes )
	{
		var vecPlane = new Vector4( in clipPlane.Normal, clipPlane.Distance );

		attributes.Set( "EnableClipPlane", true );
		attributes.Set( "ClipPlane0", vecPlane );
	}
}
