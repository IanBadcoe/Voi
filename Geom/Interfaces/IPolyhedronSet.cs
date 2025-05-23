using System.Collections.Generic;

namespace Voi.Geom.Interfaces;

public interface IPolyhedronSet
{
    IEnumerable<IPolyhedron> Polyhedra { get; }
}
