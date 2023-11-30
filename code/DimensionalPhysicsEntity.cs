using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;
public partial class DimensionalPhysicsEntity : DimensionalModelEntity
{
	bool _ignorePortal = false;
	[Net] DimensionalPortal _activeCrossingPortal { get; set; } = null;

	bool _lastInFront = true;
	bool _swapToNewDimension = false;

	public DimensionalPhysicsEntity()
	{

	}

	public DimensionalPhysicsEntity( Dimension dimension ) : base( dimension )
	{

	}

	[Event.Tick.Server]
	public void CheckPortals()
	{
		var pos = Position + Model.RenderBounds.Center * Rotation;

		if ( _activeCrossingPortal is null )
		{
			var connectedPortals = Entity.All.OfType<DimensionalPortal>()
				.Where( p => p.IsConnected( Dimension ) )
				.OrderBy( p => p.Position.Distance( pos ) );

			int i = 0;
			if ( SpaceGame.Debug )
			{
				foreach ( var p in connectedPortals )
				{
					DebugOverlay.Sphere( p.Position, 5.0f, Color.Cyan, depthTest: false );
					DebugOverlay.ScreenText( $"{p}", 15 + i++ );
				}
			}

			var portal = connectedPortals.FirstOrDefault();
			if ( portal is not null )
			{
				if ( SpaceGame.Debug )
				{
					DebugOverlay.ScreenText( $"{portal}", 17 + i++ );
					DebugOverlay.Sphere( portal.Position, 5.0f, Color.Green, depthTest: false );
				}

				CheckTouch( portal, out bool touching, out bool inFront );

				if ( touching )
				{
					_activeCrossingPortal = portal;
					_ignorePortal = !inFront;

					if ( _ignorePortal )
						Log.Info( "Entered from the back, doing nothing..." );
					else
					{
						Log.Info( "Swhoomp" );
						_lastInFront = true;
						_swapToNewDimension = false;
						BeginTransition();
					}
				}
			}
		}
		else
		{
			CheckTouch( _activeCrossingPortal, out bool touching, out bool inFront );

			if ( SpaceGame.Debug )
			{
				DebugOverlay.Sphere( _activeCrossingPortal.Position, 5.0f, Color.Orange, depthTest: false );
			}

			if ( !touching )
			{
				//if ( _ignorePortal )
				//	Log.Info( "Entered from the back, doing nothing..." );
				//else
				{
					//Log.Info( "Swhoomp" );
					EndTransition();
					if ( _swapToNewDimension )
					{
						var destination = _activeCrossingPortal.GetOtherDimension( Dimension );
						Dimension = destination;
					}
				}

				_activeCrossingPortal = null;
			}
			else
			{
				if ( !_ignorePortal )
				{
					var changed = false;

					if ( inFront != _lastInFront )
					{
						_lastInFront = inFront;
						changed = true;
					}

					if ( changed )
					{
						//Log.Info( "Swap?:)" );
						MidTransitionSwap();
						_swapToNewDimension = !_swapToNewDimension;
					}
				}
			}
		}

		if ( SpaceGame.Debug )
		{
			DebugOverlay.ScreenText( $"_activeCrossingPortal {_activeCrossingPortal}", 10 );
			DebugOverlay.ScreenText( $"_ignorePortal {_ignorePortal}", 11 );
			DebugOverlay.ScreenText( $"_lastInFront {_lastInFront}", 12 );
			DebugOverlay.ScreenText( $"_swapToNewDimension {_swapToNewDimension}", 13 );
		}
	}

	[ClientRpc]
	private void BeginTransition()
	{
		var portal = _activeCrossingPortal;
		var destination = portal.GetOtherDimension( Dimension );
		SceneModelManager.BeginTransition( destination );
	}

	[ClientRpc]
	private void MidTransitionSwap()
	{
		SceneModelManager.MidTransitionSwap();
	}

	[ClientRpc]
	private void EndTransition()
	{
		SceneModelManager.EndTransition();
	}


	private void CheckTouch( DimensionalPortal portal, out bool touching, out bool inFront )
	{
		var pos = Position + Model.RenderBounds.Center * Rotation;

		var inv = portal.Rotation.Inverse;

		var corners = Model.RenderBounds.Corners.Select( v => (v * Rotation + Position - portal.Position) * inv ).ToList();
		var aabb = corners.GetAABB();

		var a = new Vector3( -1.0f, -portal.Size.x, -portal.Size.y );
		var b = new Vector3( 1.0f, portal.Size.x, portal.Size.y );

		var forwardContained = corners.Any( c => c.y > a.y && c.y < b.y && c.z > a.z && c.z < b.z );
		var sideContained = aabb.Mins.x < 0.0f && aabb.Maxs.x > 0.0f;


		touching = forwardContained && sideContained;
		inFront = portal.IsInFront( Dimension, pos );

		if ( SpaceGame.Debug )
		{
			if ( touching )
			{
				DebugOverlay.ScreenText( "Touching!", 6 );
			}

			if ( inFront )
			{
				DebugOverlay.Line( pos, portal.Position, depthTest: false );
				DebugOverlay.ScreenText( "Front!", 8 );
			}

			DebugOverlay.ScreenText( $"{aabb.Center}", 4 );
			DebugOverlay.Box( portal.Position + aabb.Mins, portal.Position + aabb.Maxs, Color.Red, depthTest: false );
			DebugOverlay.Box( portal.Position + a, portal.Position + b, Color.Orange, depthTest: false );
		}
	}
}
