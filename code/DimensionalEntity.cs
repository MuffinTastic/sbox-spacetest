using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;

public partial class DimensionalEntity : Entity
{
	[Net] public Dimension Dimension { get; set; }

	public DimensionalEntity()
	{

	}

	public DimensionalEntity( Dimension dimension )
	{
		Dimension = dimension;
		Dimension.Entities.Add( this );
	}

	public override async void ClientSpawn()
	{
		await Util.AwaitCondition( () => Dimension is not null );
		await Dimension.AwaitReady();

		InitSceneObject();
	}

	public virtual void InitSceneObject()
	{

	}
}
