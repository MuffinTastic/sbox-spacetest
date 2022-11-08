using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;
public class DimensionSceneModelManager
{
	public DimensionalModelEntity _entity { get; set; }
	public Model _model { get; set; }

	public Dimension _mainDimension { get; set; }
	private Dimension _temporaryDimension { get; set; }

	public SceneObject _main { get; set; }
	public SceneObject _temporary { get; set; }

	public bool EnableDrawing { get; set; }

	public DimensionSceneModelManager( DimensionalModelEntity entity, Model model )
	{
		Host.AssertClient();

		_entity = entity;
		_model = model;

		EnableDrawing = true;
	}

	public void Init( Dimension dimension )
	{
		Host.AssertClient();

		if ( _main is not null )
			throw new InvalidOperationException( "Main sceneobject was not null at initialization" );

		_mainDimension = dimension;
		GenerateSceneModels();
	}

	public void SetModel( Model model )
	{
		_model = model;
		GenerateSceneModels();
	}

	private void GenerateSceneModels()
	{
		if ( _mainDimension is not null )
		{
			if ( _main is not null )
				_main.Delete();

			_main = new SceneModel( _mainDimension.SceneWorld, _model, _entity.Transform );
		}

		if ( _temporaryDimension is not null )
		{
			if ( _temporary is not null )
				_temporary.Delete();

			_temporary = new SceneModel( _temporaryDimension.SceneWorld, _model, _entity.Transform );
		}
	}


	private void Swap()
	{
		Host.AssertClient();

		var swapDimension = _mainDimension;
		_mainDimension = _temporaryDimension;
		_temporaryDimension = swapDimension;

		var swap = _main;
		_main = _temporary;
		_temporary = swap;
	}

	public void BeginTransition( Dimension dimension )
	{
		Host.AssertClient();

		if ( _main is null )
			throw new InvalidOperationException( "Main sceneobject was null at transition start" );
		if ( _temporary is not null )
			throw new InvalidOperationException( "Temporary sceneobject was not null at transition start" );

		_temporaryDimension = dimension;
		_temporary = new SceneModel( _temporaryDimension.SceneWorld, _model, _main.Transform );
	}

	public void MidTransitionSwap()
	{
		Host.AssertClient();

		if ( _main is null )
			throw new InvalidOperationException( "Main sceneobject was null during mid transition swap" );
		if ( _temporary is null )
			throw new InvalidOperationException( "Temporary sceneobject was null during mid transition swap" );

		Swap();
	}

	public void EndTransition()
	{
		Host.AssertClient();

		if ( _main is null )
			throw new InvalidOperationException( "Main sceneobject was null during transition finalization" );
		if ( _temporary is null )
			throw new InvalidOperationException( "Temporary sceneobject was null during transition finalization" );

		_temporaryDimension = null;

		_temporary.Delete();
		_temporary = null;
	}

	public void SetTransform( Transform transform )
	{
		Host.AssertClient();

		if ( _main is not null )
			_main.Transform = transform;

		if ( _temporary is not null )
			_temporary.Transform = transform;
	}
}
