// using System;
// using System.Collections.Generic;
// using System.Linq;

// using Geom.Interfaces;
// using Geom;

// using SubD;

// using VIdx = SubD.Idx<SubD.Vert>;
// using EIdx = SubD.Idx<SubD.Edge>;
// using FIdx = SubD.Idx<SubD.Face>;
// using System.Numerics;

// namespace Geom.Util
// {
//     public class BuildSurfaceFromPolyhedron
//     {
//         int NextVertIdx;
//         int NextEdgeIdx;
//         int NextPolyIdx;

//         BidirectionalDictionary<VIdx, Vert> Verts;
//         BidirectionalDictionary<EIdx, Edge> Edges;
//         // confusingly "Poly" and "Polys" refers to polygons
//         // but that is the naming I used in Godot_SubD, so keep it here, "polyhedron" will have to be
//         // written out in full each time
//         Dictionary<FIdx, Poly> Polys;

//         public Surface BuildFromPolyhedra(IEnumerable<IPolyhedron> polyhedra)
//         {
//             Reset();

//             // adjoining polyhedra cancel their common face, this list is what's left after doing that
//             HashSet<Face> real_faces = [];

//             foreach(var polyhedron in polyhedra)
//             {
//                 foreach(var face in polyhedron.Faces)
//                 {
//                     // should see each face forwards once and backwards once
//                     Godot_Util.Util.Assert(!real_faces.Contains(face));

//                     Face rev_face = face.Reversed();
//                     if (real_faces.Contains(rev_face))
//                     {
//                         real_faces.Remove(rev_face);
//                     }
//                     else
//                     {
//                         real_faces.Add(face);
//                     }
//                 }
//             }

//             foreach(var face in real_faces)
//             {
//                 IEnumerable<VIdx> v_idxs = AddFindVerts(face.Verts);
//             }
//         }

//         private IEnumerable<VIdx> AddFindVerts(IReadOnlyList<ImVec3> verts)
//         {
//             List<VIdx> v_idxs = [];

//             foreach(var vec in verts)
//             {
//                 Vert vert = new Vert(vec.ToVector3());

//                 VIdx v_idx;

//                 if (Verts.Contains(vert))
//                 {
//                     v_idx = Verts[vert];
//                 }
//                 else
//                 {
//                     v_idx = new(NextVertIdx++);

//                     Verts[v_idx] = vert;
//                 }

//                 v_idxs.Add(v_idx);
//             }

//             return v_idxs;
//         }


//         private void Reset()
//         {
//             NextVertIdx = 0;
//             NextEdgeIdx = 0;
//             NextPolyIdx = 0;

//             Verts = [];
//             Edges = [];
//             Polys = [];
//         }


//         public void AddPolyHedron(IPolyhedron polyhedron)
//         {

//         }
//         public static Surface Polyhedron2Surface(IPolyhedron poly, ImVec3 origin)
//         {
//             Godot_Util.Util.Assert(poly.Type != IPolyhedron.MeshType.Unknown,
//                 "Trying to make a surface of unknown type...");

//             if (poly.Type == IPolyhedron.MeshType.Smooth)
//             {
//                 return SmoothSurface(poly, origin);
//             }

//             return FaceSurface(poly, origin);
//         }

//         static Surface SmoothSurface(IPolyhedron poly, ImVec3 origin)
//         {
//             int NextVertIdx = 0;
//             int NextEdgeIdx = 0;
//             int NextPolyIdx = 0;

//             BidirectionalDictionary<VIdx, Vert> Verts = [];
//             BidirectionalDictionary<EIdx, Edge> Edges = [];
//             Dictionary<FIdx, Poly> Polys = [];



//             var surf = new Surface();

//             Vector3[] verts = poly.Verts.Select(v => (v - origin).ToVector3()).ToArray();
//             mesh.vertices = verts;

//             List<int> tris = new List<int>();

//             var v3_verts = poly.Verts.ToArray();

//             foreach (var face in poly.Faces)
//             {
//                 List<int> vert_idxs = face.Verts.Select(v => Array.IndexOf(v3_verts, v)).ToList();

//                 for (int i = 1; i < vert_idxs.Count - 1; i++)
//                 {
//                     tris.AddRange(new int[] { vert_idxs[0], vert_idxs[i], vert_idxs[i + 1] });
//                 }
//             }

//             mesh.triangles = tris.ToArray();
//             return mesh;
//         }

//         static Mesh FaceMesh(IPolyhedron poly, Vec3 origin)
//         {
//             var mesh = new Mesh();

//             Vector3[] verts = poly.Verts.Select(v => (v - origin).ToVector3()).ToArray();
//             mesh.vertices = verts;

//             List<int> tris = new List<int>();

//             var v3_verts = poly.Verts.ToArray();

//             List<Vector3> duplicated_verts = new List<Vector3>();
//             List<Vector3> normals = new List<Vector3>();

//             foreach (var face in poly.Faces)
//             {
//                 int vert_base = duplicated_verts.Count;

//                 int num_verts = face.Verts.Count;
//                 normals.AddRange(Enumerable.Repeat(face.Normal.ToVector3(), num_verts));
//                 duplicated_verts.AddRange(face.Verts.Select(v => (v - origin).ToVector3()));

//                 for (int i = 1; i < num_verts - 1; i++)
//                 {
//                     tris.AddRange(new int[] { vert_base, vert_base + i, vert_base + i + 1 });
//                 }
//             }

//             mesh.vertices = duplicated_verts.ToArray();
//             mesh.normals = normals.ToArray();
//             mesh.triangles = tris.ToArray();

//             return mesh;
//         }
//     }
// }
