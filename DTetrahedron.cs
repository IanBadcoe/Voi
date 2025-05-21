using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Geom;
using Geom.Interfaces;

using Geom_Util;

namespace Voi
{
    [DebuggerDisplay("(({Verts[0].X}, {Verts[0].Y}, {Verts[0].Z}) ({Verts[1].X}, {Verts[1].Y}, {Verts[1].Z}) ({Verts[2].X}, {Verts[2].Y}, {Verts[2].Z}) ({Verts[3].X}, {Verts[3].Y}, {Verts[3].Z}))")]
    public class DTetrahedron : IEquatable<DTetrahedron>
    {
        public DTetrahedron(ImVec3 p0, ImVec3 p1, ImVec3 p2, ImVec3 p3)
        {
            Verts = new List<ImVec3> { p0, p1, p2, p3 };
            Sphere = new CircumSphere(Verts);
        }

        public IReadOnlyList<ImVec3> Verts { get; }
        public CircumSphere Sphere { get; }

        public ImVec3 Centre => Verts.Aggregate((v1, v2) => v1 + v2) / 4;

        public bool Valid
        {
            get
            {
                return Sphere.Valid;
            }
        }

        public IEnumerable<Triangle> Triangles
        {
            get
            {
                // trying to get these rotating the same way
                // but at the moment it doesn't matter
                yield return new Triangle(Verts[0], Verts[1], Verts[2]);
                yield return new Triangle(Verts[0], Verts[3], Verts[1]);
                yield return new Triangle(Verts[0], Verts[2], Verts[3]);
                yield return new Triangle(Verts[2], Verts[1], Verts[3]);
            }
        }

        public bool UsesVert(ImVec3 p)
        {
            return Verts.Contains(p);
        }

        public Polyhedron ToPolyhedron()
        {
            //            var ret = new VPolyhedron(Sphere.Centre);
            // the centre here is the geometric center of the tetrahedron, not the circumcentre
            var ret = new Polyhedron(Verts.Aggregate((x, y) => x + y) * 0.25f, IPolyhedron.MeshType.Faces, true);

            foreach (var tri in Triangles)
            {
                ret.AddFace(null, tri.ToFace((tri.Centre - Centre).Normalised()));
            }

            return ret;
        }

        public bool Equals(DTetrahedron other)
        {
            foreach (var vert in Verts)
            {
                if (!other.Verts.Contains(vert))
                {
                    return false;
                }
            }

            return true;
        }
    }
}


//public enum AdjoinsResult
//{
//    Separate,
//    Point,
//    Edge,
//    Face,
//    Identity
//}

//public AdjoinsResult Adjoins(DTetrahedron tet)
//{
//    int count = 0;

//    foreach (var p in Verts)
//    {
//        if (tet.Verts.Contains(p))
//        {
//            count++;
//        }
//    }

//    switch (count)
//    {
//        case 1:
//            return AdjoinsResult.Point;
//        case 2:
//            return AdjoinsResult.Edge;
//        case 3:
//            return AdjoinsResult.Face;
//        case 4:
//            return AdjoinsResult.Identity;
//    }

//    return AdjoinsResult.Separate;
//}

