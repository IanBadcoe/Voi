using System.Collections.Generic;

using Voi.Geom.Interfaces;

using Geom_Util.Immutable;

namespace Voi.Interfaces;

public interface IVoronoi : IPolyhedronSet
{
    IEnumerable<Geom.Face> Faces { get; }
    IEnumerable<ImVec3> Verts { get; }
    IDelaunay Delaunay { get; }
    float Tolerance { get; }
}
