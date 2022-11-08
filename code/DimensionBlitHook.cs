using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;
public class DimensionBlitHook : RenderHook
{
	private static Material PortalBlit = Material.Load( "materials/portalblit.vmat" );
	private static Material ScreenBlit = Material.Load( "materials/screenblit.vmat" );

	public DimensionBlitHook()
	{

	}

	public override void OnFrame( SceneCamera target )
	{
		var dimensions = Entity.All.OfType<Dimension>();
		foreach ( var dimension in dimensions )
			dimension.UpdateSceneCameraFromGlobal();
	}

	private static int LastTick = 0;

	public override void OnStage( SceneCamera target, Stage renderStage )
	{
		if ( renderStage == Stage.AfterTransparent )
		{
			using var renderTarget = RenderTarget.GetTemporary();

			Graphics.RenderTarget = renderTarget;
			Graphics.Clear( Color.Black, false, true );

			foreach ( var portal in Entity.All.OfType<DimensionalPortal>() )
				portal.ResetRendering();

			var dimension = Pawn.LocalDimension;

			if ( dimension is not null )
			{
				//RenderAllPortals( target, dimension );
				Graphics.RenderToTexture( dimension.SceneCamera, renderTarget.ColorTarget );
			}

			RenderAttributes attributes = new();

			Graphics.RenderTarget = null;

			attributes.Set( "ColorBuffer", renderTarget.ColorTarget );

			Graphics.Blit( ScreenBlit, attributes );
		}
	}

	public void RenderAllPortals( SceneCamera target, Dimension mainViewDimension )
	{
		var connectedPortals = Entity.All.OfType<DimensionalPortal>()
			.Where( p => p.IsConnected( mainViewDimension ) )
			.OrderBy( p => p.Position.Distance( mainViewDimension.SceneCamera.Position ) );

		CurrentPortal = 0;

		foreach ( var portal in connectedPortals )
		{
			//var sceneObject = portal.GetScenePortalForDimension( mainViewDimension );
			//RenderPortal( target, mainViewDimension, portal );
			//Graphics.Render( sceneObject );

			if ( CurrentPortal >= 8 ) break;
		}
	}

	public static int CurrentPortal = 0;

	private void RenderPortal( SceneCamera target, Dimension dimension, DimensionalPortal portal )
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

		if ( portal.VertexPositions is null ) return;
		var vertices = portal.VertexPositions.Select( v => new Vertex( portal.Position + v, Vector4.Zero, Color32.White ) ).ToArray();

		var relative = target.Position - portal.Position;
		var dot = relative.Dot( portalPlane.Normal );

		Vector4 clip = new Vector4( portalPlane.Normal, portalPlane.Distance );
		bool flip = portal.IsFirstDimension( dimension );
		float flipdot = flip ? dot : -dot;
		bool front = flipdot > 0.0f;

		if ( !front )
		{
			return;
		}

		if ( flip )
		{
			clip = -clip;
		}

		//DebugOverlay.Sphere( Vector3.Zero, 5.0f, Color.Red, depthTest: false );
		//DebugOverlay.Line( portalPlane.Normal * portalPlane.Distance, portalPlane.Normal * portalPlane.Distance + portalPlane.Normal * 100.0f, Color.Cyan, depthTest: false );

		var otherDimension = portal.GetOtherDimension( dimension );
		var camera = otherDimension.SceneCamera;

		camera.Attributes.SetCombo( "D_ENABLE_USER_CLIP_PLANE", true );
		camera.Attributes.Set( "EnableClipPlane", true );
		camera.Attributes.Set( "ClipPlane0", clip );

		using var renderTarget = RenderTarget.GetTemporary();

		Graphics.Clear( Color.Black, false, true );

		Graphics.RenderToTexture( camera, renderTarget.ColorTarget );

		camera.Attributes.Clear();

		var materialAttributes = new RenderAttributes();
		target.Attributes.Clear();
		target.Attributes.SetCombo( "D_PORTAL_ID", CurrentPortal );
		target.Attributes.Set( $"ColorBuffer{CurrentPortal}", renderTarget.ColorTarget );
		Graphics.Draw( vertices, vertices.Length, PortalBlit, materialAttributes );

		var flipstring = flip ? "First" : "Second";
		Log.Info( $"{flipstring} ColorBuffer{CurrentPortal} {portal} {otherDimension}" );

		CurrentPortal++;
	}
}
