//#define PROFILE_ON

using System;
using System.Collections.Generic;
using System.Linq;

using Voi.Interfaces;

using Geom;
using Geom.Interfaces;

using Godot_Util;

using Geom_Util;

namespace Voi
{
    public class Delaunay : IDelaunay
    {
        public Delaunay(float tolerance)
        {
            TetsRW = new HashSet<DTetrahedron>();
            VecToTet = new Dictionary<ImVec3, List<DTetrahedron>>();

            Tolerance = tolerance;
        }

        public Delaunay(Delaunay old)
        {
            // this works because ImVec3, CircumSphere and DTetrahedron are all immutable, so by cloning the current state
            // of old we are good to be valid in that state forever...
            TetsRW = new HashSet<DTetrahedron>(old.TetsRW);
            VecToTet = new Dictionary<ImVec3, List<DTetrahedron>>(old.VecToTet);
            Tolerance = old.Tolerance;
        }

        // HashSet keys off Reference identity, not any sort of geometry,
        // because all we need to do is find items we already have and remove them...
        HashSet<DTetrahedron> TetsRW { get; }
        Dictionary<ImVec3, List<DTetrahedron>> VecToTet { get; }

        #region IDelaunay
        public IEnumerable<DTetrahedron> Tets => TetsRW;
        public IEnumerable<DTetrahedron> TetsForVert(ImVec3 vert)
        {
            if (VecToTet.ContainsKey(vert))
            {
                foreach (var tet in VecToTet[vert])
                {
                    yield return tet;
                }
            }
        }
        public IEnumerable<ImVec3> Verts => VecToTet.Keys;
        public IDelaunay Clone()
        {
            return new Delaunay(this);
        }
        public float Tolerance { get; }

        // not structly the outer hull, if we are not convext
        public IPolyhedron OuterSurface()
        {
            Dictionary<Face, int> face_dictionary = new Dictionary<Face, int>();

            foreach (var poly in Polyhedrons)
            {
                foreach (var face in poly.Faces)
                {
                    Face face_rev = face.Reversed();

                    if (face_dictionary.ContainsKey(face_rev))
                    {
                        face_dictionary[face_rev]--;
                    }
                    else
                    {
                        Util.Assert(!face_dictionary.ContainsKey(face), "duplicate face?");

                        face_dictionary[face] = 1;
                    }
                }
            }

            var faces = face_dictionary.Where(entry => entry.Value == 1).Select(entry => entry.Key).ToList();

            var centre = faces.Aggregate(new ImVec3(), (v, f) => v + f.Centre) / faces.Count;

            Polyhedron ret = new Polyhedron(centre, IPolyhedron.MeshType.Faces, false);

            foreach(var f in faces)
            {
                ret.AddFace(null, f);
            }

            return ret;
        }
        #endregion

        #region IPolyhedronSet
        public IEnumerable<IPolyhedron> Polyhedrons => Tets.Select(tet => tet.ToPolyhedron());
        #endregion

        public void AddTet(DTetrahedron tet)
        {
            TetsRW.Add(tet);

            foreach (var v in tet.Verts)
            {
                List<DTetrahedron> tets;

                if (!VecToTet.TryGetValue(v, out tets))
                {
                    tets = new List<DTetrahedron>();
                    VecToTet[v] = tets;
                }

                tets.Add(tet);
            }
        }

        public bool Validate()
        {
            foreach (var tet in Tets)
            {
                if (!tet.Valid)
                {
                    return false;
                }

                foreach (var p in Verts)
                {
                    if (!tet.Verts.Contains(p))
                    {
                        if (tet.Sphere.Contains(p, Tolerance))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void AddVert(ImVec3 vert)
        {
            PoorMansProfiler.Start("AddVert");
            PoorMansProfiler.Start("Find Tets");
            // SPATIAL SEARCH
            List<DTetrahedron> bad_tets = Tets.Where(tet => tet.Sphere.Contains(vert, 0)).ToList();
            PoorMansProfiler.End("Find Tets");

            PoorMansProfiler.Start("Build Triangular Poly");
            TriangularPolyhedron pt = new TriangularPolyhedron(bad_tets);
            PoorMansProfiler.End("Build Triangular Poly");

            PoorMansProfiler.Start("Remove Tets");
            foreach (var tet in bad_tets)
            {
                RemoveTetInner(tet);
            }
            PoorMansProfiler.End("Remove Tets");

            PoorMansProfiler.Start("Add Tets");
            foreach (var tri in pt.TriFaces)
            {
                PoorMansProfiler.Start("Tet Ctor");
                var tet = new DTetrahedron(vert, tri.V1, tri.V2, tri.V3);
                PoorMansProfiler.End("Tet Ctor");

                PoorMansProfiler.Start("AddTet");
                AddTet(tet);
                PoorMansProfiler.End("AddTet");
            }
            PoorMansProfiler.End("Add Tets");
            PoorMansProfiler.End("AddVert");

            //Util.Assert(Validate(), "Invalid");
        }

        public void RemoveVert(ImVec3 vert)
        {
            var bad_tets = TetsForVert(vert).ToList();

            foreach(var tet in bad_tets)
            {
                RemoveTetInner(tet);
            }

            var redo_verts = bad_tets.SelectMany(tet => tet.Verts).Distinct().Where(v => v != vert);

            Delaunay temp = new Delaunay(Tolerance);

            temp.InitialiseWithVerts(redo_verts);

            Util.Assert(temp.Validate(), "invalid!");

            foreach(var tet in temp.Tets)
            {
                if (tet.Sphere.Contains(vert, 0))
                {
                    if (Tets.FirstOrDefault(t => t.Equals(tet)) == null)
                    {
                        AddTet(tet);
                    }
                    else
                    {
                        Util.Assert(true, "bob");
                    }
                }
                else
                {
                    Util.Assert(true, "bob");
                }
            }
        }

        private void RemoveTetInner(DTetrahedron tet)
        {
            TetsRW.Remove(tet);

            foreach (var v in tet.Verts)
            {
                if (VecToTet[v].Count == 1)
                {
                    VecToTet.Remove(v);
                }
                else
                {
                    VecToTet[v].Remove(tet);
                }
            }
        }

        public void InitialiseWithTet(ImVec3[] verts)
        {
            var bounding_tet = new DTetrahedron(verts[0], verts[1], verts[2], verts[3]);

            AddTet(bounding_tet);
        }

        public void InitialiseWithVerts(IEnumerable<ImVec3> verts)
        {
            DTetrahedron bounding_tet = GetBoundingTet(verts);

            AddTet(bounding_tet);

            // all the corners of the bounding volume we just invented should be within the sphere of this tet...

            //System.Diagnostics.Debug.Assert(Validate());

            foreach (var v in verts)
            {
                AddVert(v);

                //UnityEngine.Debug.Assert(Validate());
            }

            var bounding_tet_verts = bounding_tet.Verts.ToArray();
            var bounding_tet_vert_tets = bounding_tet_verts.SelectMany(vert => TetsForVert(vert)).Distinct().ToList();

            // remove the encapsulating verts we started with, and any associated tets
            foreach (var tet in bounding_tet_vert_tets)
            {
                RemoveTetInner(tet);
            }

            Util.Assert(!Verts.Contains(bounding_tet_verts[0]), "Initial vert not found");
            Util.Assert(!Verts.Contains(bounding_tet_verts[1]), "Initial vert not found");
            Util.Assert(!Verts.Contains(bounding_tet_verts[2]), "Initial vert not found");
            Util.Assert(!Verts.Contains(bounding_tet_verts[3]), "Initial vert not found");
        }

        public DTetrahedron GetBoundingTet(IEnumerable<ImVec3> verts)
        {
            ImBounds b = new();

            ImVec3 c0;
            ImVec3 c1;
            ImVec3 c2;
            ImVec3 c3;

            foreach (var p in verts)
            {
                b = b.Encapsulating(p);
            }

            // looks like in Unit terms, points on the edge occasionally are considered outside the bound?
            b = b.ExpandedBy(0.001f);

            foreach (var c in verts)
            {
                Util.Assert(b.Contains(c), "Constructed vert outside bounds");
            }

            // build an encapsulating tetrahedron, using whichever axis is longest, and padding by 1 on each dimension
            c0 = new ImVec3(b.Min.X - 1, b.Min.Y - 1, b.Min.Z - 1);
            float size = MathfExtensions.Max(b.Size.X, b.Size.Y, b.Size.Z) + 2;

            // a right-angled prism which contains that box has its corners on the axes at 3x the
            // box dimensions
            c1 = c0 + new ImVec3(size * 3, 0, 0);
            c2 = c0 + new ImVec3(0, size * 3, 0);
            c3 = c0 + new ImVec3(0, 0, size * 3);

            //System.Diagnostics.Debug.Assert(bounding_tet.Valid);

            // all the corners of the bounding volume we just invented should be within the sphere of this tet...
            // as should c0 -> c3
            // and the original input points

            var bounding_tet = new DTetrahedron(c0, c1, c2, c3);

            foreach (var c in b.Corners)
            {
                Util.Assert(bounding_tet.Sphere.Contains(c, -Tolerance / 10), "Bounds corner not in circumsphere");
            }
            foreach (var c in new ImVec3[] { c0, c1, c2, c3 })
            {
                Util.Assert(bounding_tet.Sphere.Contains(c, -Tolerance / 10), "Tet corner not in circumsphere");
            }
            foreach (var c in verts)
            {
                Util.Assert(bounding_tet.Sphere.Contains(c, -Tolerance / 10), "Vert not in circumsphere");
            }

            return bounding_tet;
        }
    }
}
