using System.Collections.Generic;

namespace Geom.Interfaces
{
    public interface IPolyhedronSet
    {
        IEnumerable<IPolyhedron> Polyhedrons { get; }
    }
}
