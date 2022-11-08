using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;

public partial class DimensionalEntity : Entity, IDimensionalEntity
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
}
