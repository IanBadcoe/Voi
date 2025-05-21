//#define PROFILE_ON

using System;
using System.Collections.Generic;
using System.Linq;

using Geom_Util;

using Voi.Interfaces;

using Geom;
using Geom.Interfaces;

using Godot_Util;

using Godot;

namespace Voi
{
    class ProgressiveVoronoi : IProgressiveVoronoi
    {
        readonly Delaunay DelaunayRW;
        readonly Dictionary<ImVec3Int, ProgressivePoint> Points;
        readonly RTree<ImVec3> PolyVerts;    // these are the polygon/polyhedron verts
                                             // just kept here so we can merge those which are too close together
                                             // to help eliminate degenerate polygons

        readonly IPVMapper Mapper;

        public ProgressiveVoronoi(int size, float tolerance, float perturbation, ClRand random)
            : this(new Mappers.CubeMapper(size, random, perturbation), tolerance)
        {
        }

        public ProgressiveVoronoi(IPVMapper mapper, float tolerance)
        {
            // very basic, and will iterate Random differently between debug and release, but can uncomment if suspicious...
            // (and may inject a vert for cell (0, 0, 0) in debug, but that should be harmless apart from changing
            // Random as discussed)
            Util.Assert(new ImVec3Int(0, 0, 0).Equals(mapper.Vert2Cell(mapper.MakeVertForCell(new ImVec3Int(0, 0, 0)))), "Mapper integrity test failed...");

            Mapper = mapper;
            DelaunayRW = new Delaunay(tolerance);
            Points = new Dictionary<ImVec3Int, ProgressivePoint>();
            PolyVerts = new RTree<ImVec3>();

            InitialiseDelaunay(Mapper.Bounds());
        }

        #region IPolyhedronSet
        public IEnumerable<IPolyhedron> Polyhedrons => Points.Values.Select(pv => pv.Polyhedron).Where(p => p != null);
        #endregion

        #region IProgressiveVoronoi
        public IProgressivePoint AddPoint(ImVec3Int cell,
            IPolyhedron.MeshType mesh_type, Material material)
        {
            PoorMansProfiler.Start("AddPoint");

            if (!InRange(cell, IProgressiveVoronoi.Solidity.Solid))
            {
                throw new ArgumentOutOfRangeException("cell", "solid cells must be 1 cell deep inside the bounds");
            }

            PoorMansProfiler.Start("Adding Points");

            // fill in neighbouring vacuum points, where required, to bound this one...
            // could maybe use only OrthoNeighbour here, but then when adding a diagonal neighbour we
            // might change the shape of this cell, requiring a regeneration of part of it
            // so do this for immutability/simplicity for the moment
            foreach (var pp in this.AllGridNeighbours(cell).Select(pnt => Point(pnt)))
            {
                if (!pp.Exists)
                {
                    AddPointInner(pp.Cell, Mapper.MakeVertForCell(pp.Cell), IProgressiveVoronoi.Solidity.Vacuum,
                        IPolyhedron.MeshType.Unknown, null);
                }
            }

            var npp = AddPointInner(cell, Mapper.MakeVertForCell(cell),
                IProgressiveVoronoi.Solidity.Solid, mesh_type,
                material);

            PoorMansProfiler.End("Adding Points");

            PoorMansProfiler.Start("Generate Polyhedron");
            GeneratePolyhedron(npp);
            PoorMansProfiler.End("Generate Polyhedron");

            PoorMansProfiler.End("AddPoint");

            return npp;
        }

        public IProgressivePoint Point(ImVec3Int cell)
        {
            ProgressivePoint pp;

            if (Points.TryGetValue(cell, out pp))
            {
                return pp;
            }

            // default ProgressivePoint has Exists = false, and Solitity = Unknown...
            return new ProgressivePoint(Cell2Vert(cell, IProgressiveVoronoi.CellPosition.Centre), cell, this, IPolyhedron.MeshType.Unknown, null);
        }

        public IEnumerable<ImVec3Int> AllGridNeighbours(ImVec3Int pnt, IProgressiveVoronoi.Solidity permitted_for = IProgressiveVoronoi.Solidity.Vacuum)
        {
            return Mapper.AllGridNeighbours(pnt, permitted_for);
        }

        public IEnumerable<ImVec3Int> OrthoGridNeighbours(ImVec3Int pnt, IProgressiveVoronoi.Solidity permitted_for = IProgressiveVoronoi.Solidity.Vacuum)
        {
            return Mapper.OrthoGridNeighbours(pnt, permitted_for);
        }

        public bool InRange(ImVec3Int cell, IProgressiveVoronoi.Solidity solid)
        {
            return Mapper.InRange(cell, solid);
        }

        public IEnumerable<IProgressivePoint> AllPoints => Points.Values;

        public ImVec3Int Vert2Cell(ImVec3 vert)
        {
            // unit-scale for the moment...
            return Mapper.Vert2Cell(vert);
        }

        public ImVec3 Cell2Vert(ImVec3Int cell, IProgressiveVoronoi.CellPosition pos)
        {
            // this is giving a "fake" vert, for a cell that doesn't contain one for us to look-up
            // so return the cell centre

            // unit-scale for the moment
            if (pos == IProgressiveVoronoi.CellPosition.Origin)
            {
                return cell.ToImVec3();
            }

            return new ImVec3(cell.X + 0.5f, cell.Y + 0.5f, cell.Z + 0.5f);
        }
        public IDelaunay Delaunay => DelaunayRW;

        #endregion

        private void InitialiseDelaunay(ImBounds bounds)
        {
            float sphere_radius = bounds.Size.Length() / 2;

            // did a whole slew of maths to show Q = 1/6 * sqrt(2/3) is the closest approach (centre) of
            // a tet face to the tet centroid (when the tet edge length is 1)
            float Q = 1f / 6f / Mathf.Sqrt(2f / 3f);

            float tet_size = sphere_radius / Q;

            // tet_size is the edge length of the tet

            // tet corners are:
            //
            // lower face in XZ plane:
            // a = (-1/2,  -Q,             -1/3 sqrt(3/4))
            // b = (+1/2,  -Q,             -1/3 sqrt(3/4))
            // c = ( 0,    -Q,             +2/3 sqrt(3/4))
            //
            // apex on y axis:
            // d = ( 0,     sqrt(2/3) - Q,  0            )
            //
            // (I did a whole slew of trig to show Q = 1/6 * sqrt(2/3) is the closest approach (centre) of
            //  a tet face to the tet centroid (when the tet edge length is 1) see Docs/TetCentreDistanceMaths.jpg)
            //
            // e.g. XZ plane
            //                         ______________________
            //                        c                  ^   ^
            //                       /|\                 |   |
            //                      / | \                |   |
            //                     /  |  \               |   |
            //                    /   |   \          2/3 |   |
            //                   /    |    \             |   |
            //                1 /     |     \ 1          |   | sqrt(3/4)
            //                 /      |      \           v   |
            //   (x = 0) -----/-------d       \ ---------    |
            //               /        |(above) \         ^   |
            //              /         |         \    1/3 |   |
            //             a__________|__________b_______v___v
            //                 1/2    |    1/2
            //                        |
            //                         (z = 0)
            //
            // and then we'll scale all that up by tet_size


            ImVec3 a = new ImVec3(-1f / 2, -Q, -1f / 3 * Mathf.Sqrt(3f / 4));
            ImVec3 b = new ImVec3(+1f / 2, -Q, -1f / 3 * Mathf.Sqrt(3f / 4));
            ImVec3 c = new ImVec3(0, -Q, 2f / 3 * Mathf.Sqrt(3f / 4));
            ImVec3 d = new ImVec3(0, Mathf.Sqrt(2f / 3) - Q, 0);

            a *= tet_size;
            b *= tet_size;
            c *= tet_size;
            d *= tet_size;

            // translate the tet so that our requested cube is located where bounds was
            var offset = bounds.Centre;

            a += offset;
            b += offset;
            c += offset;
            d += offset;

            DelaunayRW.InitialiseWithTet(new ImVec3[] { a, b, c, d });
        }

        private void GeneratePolyhedron(ProgressivePoint pnt)
        {
            // this one should exist...
            Util.Assert(Points.ContainsKey(pnt.Cell), "unknown point");

            foreach (ProgressivePoint neighbour in PointNeighbours(pnt))
            {
                //PoorMansProfiler.Start("FaceWithNeighbour");
                // our neighbour may already have the relevant face...
                // (if it is non-solid)
                Face face = neighbour.FaceWithNeighbour(pnt);
                //PoorMansProfiler.End("FaceWithNeighbour");

                if (face == null)
                {
                    //PoorMansProfiler.Start("TryCreateFace");
                    face = TryCreateFace(neighbour.Position, pnt.Position);
                    //PoorMansProfiler.End("TryCreateFace");

                    if (face != null)
                    {
                        // in here, we are a new face to the neighbour as well...
                        neighbour.PolyhedronRW.AddFace(pnt, face);
                    }
                }

                if (face != null)
                {
                    //PoorMansProfiler.Start("AddFace");
                    // the face our neighbour has is backwards compared to what we want...
                    // and if we just made it, then we made it backwards anyway, because otherwise we end up
                    // reversing it *twice*
                    pnt.PolyhedronRW.AddFace(neighbour, face.Reversed());
                    //PoorMansProfiler.End("AddFace");
                }
            }
        }

        // can return null for a degenerate face
        private Face TryCreateFace(ImVec3 our_point, ImVec3 other_point)
        {
            PoorMansProfiler.Start("FindTets");
            // find all the tets that use this edge
            var edge_tets = Delaunay.TetsForVert(our_point).Where(tet => tet.UsesVert(other_point)).Distinct().ToList();
            PoorMansProfiler.End("FindTets");

            var face_verts = new List<ImVec3>();

            var current_tet = edge_tets[0];

            PoorMansProfiler.Start("FindFromVert");
            // get any one other vert of this tet, this indicates the direction we are
            // "coming from"
            var v_from = current_tet.Verts.Where(v => v != our_point && v != other_point).First();
            PoorMansProfiler.End("FindFromVert");

            PoorMansProfiler.Start("Loop");

            do
            {
                edge_tets.Remove(current_tet);

                face_verts.Add(current_tet.Sphere.Centre);

                // the remaining local vert when we strike off the two common to the edge and the one we came from
                var v_towards = current_tet.Verts.Where(v => v != our_point && v != other_point && v != v_from).First();

                // we move to the tet which shares this face with us...
                var tet_next = edge_tets.Where(tet => tet.UsesVert(our_point) && tet.UsesVert(other_point) && tet.UsesVert(v_towards)).FirstOrDefault();

                current_tet = tet_next;

                // when we move on, we will be moving off using old forwards vert
                v_from = v_towards;
            }
            while (current_tet != null);

            PoorMansProfiler.End("Loop");

            // eliminate any face_verts which are identical (or within a tolerance) of the previous
            // but (i) with randomized seed data we do not expect that to happen much and (ii) all ignoring this does is add degenerate
            // polys/edges to the output, which I do not think will be a problem at the moment

            PoorMansProfiler.Start("MergeVerts");

            ImVec3 prev_vec = AddFindPolyVert(face_verts.Last());

            List<ImVec3> merged_verts = new List<ImVec3>(face_verts.Count);

            // first: AddFind all verts into a set stored on this Voronoi
            for (int i = 0; i < face_verts.Count; i++)
            {
                ImVec3 here_vec = AddFindPolyVert(face_verts[i]);

                if (here_vec != prev_vec)
                {
                    merged_verts.Add(here_vec);
                }

                prev_vec = here_vec;
            }

            PoorMansProfiler.End("MergeVerts");

            // now, input points (Delaunay.Verts) which are neighbours become such by virtue of still having
            // a common face after this process of eliminating tiny edges/degenerate polys,
            // NOT just because the Delaunay said they were
            //
            // (e.g. they are technically Delaunay-neighbours but the contact polygon is of negligeable area
            // so the neighbour-ness can be ignored, it is only as if the two points were minutely further apart
            // in the first place...)
            //
            // we may get tiny cracks between polys as a result, but the polys themselves should still be closed...

            if (merged_verts.Count < 3)
            {
                return null;
            }

            // can still have faces with tiny area, e.g. very long thin triangles with a vert in the middle,
            // but those are hard to eliminate because the middle vert needs to disappear on this poly
            // but probably _not_ on its neighbour
            //
            // and I think all that happens with those is we do not know which way round to draw them,
            // but they are all but invisible anyway...

            PoorMansProfiler.Start("Face ctor");

            var ret = new Face(merged_verts, (other_point - our_point).Normalised());

            PoorMansProfiler.End("Face ctor");

            return ret;
        }

        private ImVec3 AddFindPolyVert(ImVec3 v)
        {
            // take the first existing vert, if any, within tolerance of the new vert
            var tol_vec = new ImVec3(Delaunay.Tolerance, Delaunay.Tolerance, Delaunay.Tolerance);
            var tol_bounds = new ImBounds(v - tol_vec, v + tol_vec);
            var ret = PolyVerts.Search(tol_bounds, IReadOnlyRTree.SearchMode.ContainedWithin).FirstOrDefault();

            if (ret != null)
            {
                return ret;
            }

            PolyVerts.Insert(v);

            return v;
        }

        private IEnumerable<ProgressivePoint> PointNeighbours(IProgressivePoint point)
        {
            return Delaunay.TetsForVert(point.Position)
                .SelectMany(tet => tet.Verts)
                .Where(vert => vert != point.Position)
                .Distinct()
                .Select(vert => Points[Vert2Cell(vert)]);
        }

        private ProgressivePoint AddPointInner(ImVec3Int cell, ImVec3 pnt,
            IProgressiveVoronoi.Solidity solid, IPolyhedron.MeshType mesh_type,
            Material material)
        {
            PoorMansProfiler.Start("AddPointInner");
            Util.Assert(solid != IProgressiveVoronoi.Solidity.Unknown, "Trying to set point with unknown solitity");

            ProgressivePoint pp;

            if (!Points.TryGetValue(cell, out pp))
            {
                pp = new ProgressivePoint(pnt, cell, this, mesh_type, material);

                Points[cell] = pp;

                Util.Assert(Points.ContainsKey(Vert2Cell(pnt)), "Added a point but now cannot find it");

                DelaunayRW.AddVert(pnt);
            }

            pp.Exists = true;
            pp.Solidity = solid;
            pp.Material = material;
            pp.MeshType = mesh_type;
            pp.Material = material;

            PoorMansProfiler.End("AddPointInner");

            return pp;
        }
    }
}