using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;

public partial class DimensionalModelEntity : DimensionalEntity
{
	[Net, Change] public Model DimensionModel { get; set; }

	public DimensionalModelEntity()
	{

	}

	public DimensionalModelEntity( Dimension dimension ) : base( dimension )
	{

	}

	public override void Spawn()
	{
		Transmit = TransmitType.Always;

		// We handle all the drawing ourselves with SceneObjects,
		// so we don't need the base ModelEntity stuff.
		// Yes, ModelEntity.SceneObject is going wholly unused.
		EnableDrawing = false;
	}

	// Client only

	public DimensionSceneModelManager SceneModelManager { get; private set; }

	public override void ClientSpawn()
	{
		base.ClientSpawn();
	}

	public override void InitSceneObject()
	{
		SceneModelManager = new DimensionSceneModelManager( this, DimensionModel );
		SceneModelManager.Init( Dimension );
	}

	protected override void OnDestroy()
	{
		if ( IsClient )
		{
			SceneModelManager?.Delete();
			SceneModelManager = null;
		}
	}

	public void SetModel( string model )
	{
		SetModel( Model.Load( model ) );
	}

	public void SetModel( Model model )
	{
		Assert.True( IsAuthority );

		if ( DimensionModel == model )
			return;
		
		DimensionModel = model;

		if ( IsClient )
		{
			if ( SceneModelManager is null )
				return;

			SceneModelManager.SetModel( DimensionModel );
		}
	}

	public Model Model
	{
		get => DimensionModel;
		set
		{
			DimensionModel = value;
			SetModel( value );
		}
	}

	private void OnDimensionModelChanged( Model oldModel, Model newModel )
	{
		if ( SceneModelManager is null ) return;

		SceneModelManager.SetModel( newModel );
	}

	[Event.Frame]
	public void OnFrame()
	{
		if ( SceneModelManager is null ) return;

		SceneModelManager.SetTransform( Transform );
	}
}
