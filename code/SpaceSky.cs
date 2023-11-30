using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceTest;

public partial class SpaceSky
{
	public static List<SpaceSky> Skies = new();
	
	public SceneSkyBox SkyBox { get; private set; }


	public SpaceSky( SceneWorld sceneWorld ) 
	{
		var skyMat = Material.Load( "materials/spacesky.vmat" );
		Assert.NotNull( skyMat );

		SkyBox = new SceneSkyBox( sceneWorld, skyMat );

		Randomize();

		Skies.Add( this );
	}

	public void Randomize()
	{
		var seed = Game.Random.Float( -8.0f, 8.0f );
		var skyFogSeed2 = Game.Random.Float( -8.0f, 8.0f );

		var skyHue = Game.Random.Float( 0.0f, 360.0f );
		var skyHsv = new ColorHsv( skyHue, 1.0f, 0.1f );

		var skyFogHue1 = skyHue + 180.0f + Game.Random.Float( -15.0f, 15.0f );
		if ( skyFogHue1 > 360.0f ) skyFogHue1 -= 360.0f;
		if ( skyFogHue1 < 0.0f ) skyFogHue1 += 360.0f;
		var skyFogHsv1 = new ColorHsv( skyFogHue1, 1.0f, 1.0f );

		var skyFogHue2 = skyFogHue1 + Game.Random.Float( -90.0f, 90.0f );
		if ( skyFogHue2 > 360.0f ) skyFogHue2 -= 360.0f;
		if ( skyFogHue2 < 0.0f ) skyFogHue2 += 360.0f;
		var skyFogHsv2 = new ColorHsv( skyFogHue2, 1.0f, 1.0f );

		var fogNoiseDensity1 = 6.0f /* default */ + Game.Random.Float( -1.0f, 1.5f );
		var fogNoiseDensity2 = 6.0f /* default */ + Game.Random.Float( -1.0f, 1.0f );

		var fogNoiseParams = new Vector4
		(
			0.75f  + Game.Random.Float( -0.1f, 0.1f  ),
			6.0f   + Game.Random.Float( -0.5f, 0.5f  ),
			0.795f + Game.Random.Float( -.01f, 0.01f ),
			2.08f  + Game.Random.Float( -0.1f, 0.1f  )
		);

		var fogNoiseMaskParams = new Vector4
		(
			0.33f + Game.Random.Float( -0.1f, 0.1f ),
			6.0f + Game.Random.Float( -0.5f, 0.5f ),
			0.628f + Game.Random.Float( -.01f, 0.01f ),
			2.11f + Game.Random.Float( -0.1f, 0.1f )
		);

		var fogNoiseMaskParams2 = new Vector4
		(
			0.07f + Game.Random.Float( -0.1f, 0.1f ),
			-0.001f + Game.Random.Float( -0.001f, 0.001f ),
			0.628f + Game.Random.Float( -.01f, 0.01f ),
			2.5f + Game.Random.Float( -0.1f, 0.1f )
		);

		SkyBox.Attributes.Set( "Seed", seed );
		SkyBox.Attributes.Set( "SkyColor", skyHsv.ToColor() );
		SkyBox.Attributes.Set( "SkyFogSeed2", skyFogSeed2 );
		SkyBox.Attributes.Set( "SkyFogColor1", skyFogHsv1.ToColor() );
		SkyBox.Attributes.Set( "SkyFogColor2", skyFogHsv2.ToColor() );
		SkyBox.Attributes.Set( "FogNoiseDensity1", fogNoiseDensity1 );
		SkyBox.Attributes.Set( "FogNoiseDensity2", fogNoiseDensity2 );
		SkyBox.Attributes.Set( "FogNoiseParams", fogNoiseParams );
		SkyBox.Attributes.Set( "FogNoiseMaskParams", fogNoiseMaskParams );
		SkyBox.Attributes.Set( "FogNoiseMaskParams2", fogNoiseMaskParams2 );
	}

	[ConCmd.Client]
	public static void RandomizeSkies()
	{
		foreach( var sky in Skies )
		{
			sky.Randomize();
		}
	}
}
