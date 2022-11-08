using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;
public partial class DimensionalLight : DimensionalEntity
{
	[Net, Change] public new Vector3 Position { get; set; }
	[Net, Change] public float Radius { get; set; }
	[Net, Change] public float LinearAttenuation { get; set; }
	[Net, Change] public float QuadraticAttenuation { get; set; }
	[Net, Change] public Color ColorTint { get; set; }

	private SceneLight _sceneLight;

	public DimensionalLight()
	{

	}

	public DimensionalLight( Dimension dimension ) : base( dimension )
	{

	}

	public override void InitSceneObject()
	{
		_sceneLight = new SceneLight( Dimension.SceneWorld );
		_sceneLight.Position = Position;
		_sceneLight.Radius = Radius;
		_sceneLight.LinearAttenuation = LinearAttenuation;
		_sceneLight.QuadraticAttenuation = QuadraticAttenuation;
		_sceneLight.ColorTint = ColorTint;
	}

	public void OnPositionChanged( Vector3 _, Vector3 newPosition )
	{
		if ( _sceneLight is null ) return;
		_sceneLight.Position = newPosition;
	}

	public void OnRadiusChanged( float _, float newRadius )
	{
		if ( _sceneLight is null ) return;
		_sceneLight.Radius = newRadius;
	}

	public void OnLinearAttenuationChanged( float _, float newLinearAttenuation )
	{
		if ( _sceneLight is null ) return;
		_sceneLight.LinearAttenuation = newLinearAttenuation;
	}

	public void OnQuadraticAttenuationChanged( float _, float newQuadraticAttenuation )
	{
		if ( _sceneLight is null ) return;
		_sceneLight.QuadraticAttenuation = newQuadraticAttenuation;
	}

	public void OnColorTintChanged( Color _, Color newColorTint )
	{
		if ( _sceneLight is null ) return;
		_sceneLight.ColorTint = newColorTint;
	}
}
