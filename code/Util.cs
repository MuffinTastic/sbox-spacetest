using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace SpaceTest;

public static class Vector3Extensions
{
	public static BBox GetAABB( this IEnumerable<Vector3> vectors, Vector3 offset = default )
	{
		var pointsX = vectors.Select( v => v.x );
		var pointsY = vectors.Select( v => v.y );
		var pointsZ = vectors.Select( v => v.z );

		var mins = offset + new Vector3( pointsX.Min(), pointsY.Min(), pointsZ.Min() );
		var maxs = offset + new Vector3( pointsX.Max(), pointsY.Max(), pointsZ.Max() );

		return new BBox( mins, maxs );
	}
}

public static class Util
{
	public static async Task AwaitCondition( Func<bool> func )
	{
		while ( !func() )
		{
			await GameTask.Delay( 50 );
		}
	}
}
