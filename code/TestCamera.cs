using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceTest;

public class TestCamera : CameraMode
{
	[ConVar.Replicated]
	public static bool athirdperson_orbit { get; set; } = false;

	[ConVar.Replicated]
	public static bool athirdperson_collision { get; set; } = true;

	private Angles orbitAngles;
	private float orbitDistance = 150;
	private float orbitHeight = 0;

	[ConVar.Replicated]
	public static float thefov { get; set; } = 90.0f;

	public override void Update()
	{
		if ( Local.Pawn is not DimensionalModelEntity pawn )
			return;

		Position = pawn.Position;
		Vector3 targetPos;

		var center = pawn.Position + Vector3.Up * 64;

		if ( athirdperson_orbit )
		{
			Position += Vector3.Up * ((pawn.Model.Bounds.Center.z * pawn.Scale) + orbitHeight);
			Rotation = Rotation.From( orbitAngles );

			targetPos = Position + Rotation.Backward * orbitDistance;
		}
		else
		{
			Position = center;
			Rotation = Rotation.FromAxis( Vector3.Up, 4 ) * Input.Rotation;

			float distance = 130.0f * pawn.Scale;
			targetPos = Position + Input.Rotation.Right * ((pawn.Model.Bounds.Maxs.x + 15) * pawn.Scale);
			targetPos += Input.Rotation.Forward * -distance;
		}

		if ( athirdperson_collision )
		{
			var tr = Trace.Ray( Position, targetPos )
				.WithAnyTags( "solid" )
				.Ignore( pawn )
				.Radius( 8 )
				.Run();

			Position = tr.EndPosition;
		}
		else
		{
			Position = targetPos;
		}

		FieldOfView = thefov;

		Viewer = null;
	}

	public override void BuildInput( InputBuilder input )
	{
		if ( athirdperson_orbit && input.Down( InputButton.Walk ) )
		{
			if ( input.Down( InputButton.PrimaryAttack ) )
			{
				orbitDistance += input.AnalogLook.pitch;
				orbitDistance = orbitDistance.Clamp( 0, 1000 );
			}
			else if ( input.Down( InputButton.SecondaryAttack ) )
			{
				orbitHeight += input.AnalogLook.pitch;
				orbitHeight = orbitHeight.Clamp( -1000, 1000 );
			}
			else
			{
				orbitAngles.yaw += input.AnalogLook.yaw;
				orbitAngles.pitch += input.AnalogLook.pitch;
				orbitAngles = orbitAngles.Normal;
				orbitAngles.pitch = orbitAngles.pitch.Clamp( -89, 89 );
			}

			input.AnalogLook = Angles.Zero;

			input.Clear();
			input.StopProcessing = true;
		}

		base.BuildInput( input );
	}
}
