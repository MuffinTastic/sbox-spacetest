using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;

public partial class DimensionManager : Entity
{
	public static DimensionManager Instance => SpaceGame.Current.DimensionManager;

	[Net] public IList<Dimension> Dimensions { get; set; }

	public bool Ready { get; private set; }

	public DimensionManager()
	{
	}

	public override void Spawn()
	{
		Transmit = TransmitType.Always;

		if ( Game.IsServer )
		{
			Ready = true;
		}
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		if ( Game.IsClient )
		{
			Ready = true;
		}
	}

	public Dimension NewDimension()
	{
		var dimension = new Dimension();
		Dimensions.Add( dimension );
		return dimension;
	}

	public DimensionalPortal NewPortal( Dimension dimension1, Dimension dimension2 )
	{
		var portal = new DimensionalPortal( dimension1, dimension2 );

		dimension1.Entities.Add( portal );
		dimension2.Entities.Add( portal );

		return portal;
	}

	public static async Task WaitForDimensions()
	{
		RealTimeSince start = 0.0f;

		await Util.AwaitCondition( () => SpaceGame.Current is not null );
		await Util.AwaitCondition( () => Instance is not null );
		await Util.AwaitCondition( () => Instance.Ready );

		var nulltasks = Instance.Dimensions.Select(
			d => Util.AwaitCondition( () => d is not null )
		);

		await GameTask.WhenAll( nulltasks );

		var tasks = Instance.Dimensions.Select( d => d.AwaitReady() );

		await GameTask.WhenAll( tasks );

		var realm = Game.IsServer ? "server" : "client";
		Log.Info( $"Waited {start} on {realm}" );
	}
}
