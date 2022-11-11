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
				Graphics.RenderToTexture( dimension.SceneCamera, renderTarget.ColorTarget );
			}

			RenderAttributes attributes = new();

			Graphics.RenderTarget = null;

			attributes.Set( "ColorBuffer", renderTarget.ColorTarget );

			Graphics.Blit( ScreenBlit, attributes );
		}
	}
}
