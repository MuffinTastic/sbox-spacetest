using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceTest;

public interface IDimensionalEntity
{
	public Dimension Dimension { get; set; }

	public virtual void InitSceneObject()
	{

	}
}
