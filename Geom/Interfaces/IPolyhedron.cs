using System.Collections.Generic;

using Geom_Util;

namespace Geom.Interfaces
{
    public interface IPolyhedron
    {
        enum MeshType
        {
            Unknown,
            Smooth,
            Faces,

            // may add more types later...
        }

        IEnumerable<Face> Faces { get; }
        IEnumerable<ImVec3> Verts { get; }
        ImVec3 Centre { get; }
        MeshType Type { get; }

        Face GetFaceByKey(object key);
    }
}
