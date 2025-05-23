//#define PROFILE_ON

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Geom_Util.Immutable;

using Godot_Util;

namespace Voi.Geom;

// a polyhedron made of triangles
[DebuggerDisplay("Trianges: {Triangles.Count}")]
public class TriangularPolyhedron
{
    [DebuggerDisplay("({I1},{I2},{I3})")]
    public struct TriIndex : IEquatable<TriIndex>
    {
        public TriIndex(int i1, int i2, int i3)
        {
            I1 = MathfExtensions.Min(i1, i2, i3);
            I3 = MathfExtensions.Max(i1, i2, i3);
            I2 = i1 + i2 + i3 - I1 - I3;
        }

        public readonly int I1;
        public readonly int I2;
        public readonly int I3;

        public IEnumerable<int> Indices
        {
            get
            {
                yield return I1;
                yield return I2;
                yield return I3;
            }
        }

        public bool Equals(TriIndex other)
        {
            return I1 == other.I1
                && I2 == other.I2
                && I3 == other.I3;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(I1.GetHashCode(), I2.GetHashCode(), I3.GetHashCode());
        }
    }

    public readonly List<TriIndex> Triangles = new List<TriIndex>();

    public IEnumerable<Triangle> TriFaces
    {
        get
        {
            return Triangles.Select(f => new Triangle(Verts[f.I1], Verts[f.I2], Verts[f.I3]));
        }
    }

    public readonly List<ImVec3> Verts = [];

    public TriangularPolyhedron(List<DTetrahedron> tets)
    {
        foreach (var tet in tets)
        {
            AddTetrahedron(tet);
        }
    }

    public void AddTetrahedron(DTetrahedron tet)
    {
        PoorMansProfiler.Start("TriangularPolyhedron.AddTetrahedron");

        foreach (var f in tet.Triangles)
        {
            PoorMansProfiler.Start("TriangularPolyhedron.AddFindVert");
            int[] vert_idxs = [.. f.Verts.Select(v => AddFindVert(v))];
            PoorMansProfiler.End("TriangularPolyhedron.AddFindVert");

            TriIndex tri = new TriIndex(vert_idxs[0], vert_idxs[1], vert_idxs[2]);

            PoorMansProfiler.Start("TriangularPolyhedron.Tri Search");
            // some significant CPU in this search, could store indexed by hash?
            // or indexed by one vert then brute-force the others?
            bool find = Triangles.Contains(tri);
            PoorMansProfiler.End("TriangularPolyhedron.Tri Search");

            // we build ourselves from tets, when a tet we already saw contains the same face
            // as one being added, it means that is becoming an internal face between two tets
            // and we do not want it...
            if (find)
            {
                PoorMansProfiler.Start("TriangularPolyhedron.Tri Remove");
                Triangles.Remove(tri);
                PoorMansProfiler.End("TriangularPolyhedron.Tri Remove");
            }
            else
            {
                Triangles.Add(tri);
            }
        }

        PoorMansProfiler.End("TriangularPolyhedron.AddTetrahedron");
    }

    public int AddFindVert(ImVec3 v)
    {
        int idx = Verts.IndexOf(v);

        if (idx != -1)
        {
            return idx;
        }

        Verts.Add(v);

        return Verts.Count - 1;
    }
}
