using System.Collections.Generic;

using Geom.Interfaces;
using Geom;

using Geom_Util;

namespace Voi.Interfaces
{
    public interface IVoronoi : IPolyhedronSet
    {
        IEnumerable<Geom.Face> Faces { get; }
        IEnumerable<ImVec3> Verts { get; }
        IDelaunay Delaunay { get; }
        float Tolerance { get; }
    }
}
