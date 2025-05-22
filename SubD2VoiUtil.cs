
using System;
using Geom_Util;
using Geom_Util.Immutable;

using System.Linq;
using System.Collections.Generic;

namespace Voi;

using VIdx = Idx<Vert>;
using EIdx = Idx<Edge>;
using FIdx = Idx<Face>;

public static class SubD2VoiUtil
{
    public static Surface Tet2Surf(DTetrahedron tet)
    {
        Geom.Polyhedron g_poly = tet.ToPolyhedron();

        return PolyhedronToSurf(g_poly);
    }

    public static Surface PolyhedronToSurf(Geom.Polyhedron g_poly)
    {
        SpatialDictionary<VIdx, Vert> surf_verts = [];
        SpatialDictionary<EIdx, Edge> surf_edges = [];
        SpatialDictionary<FIdx, Face> surf_faces = [];

        int next_v_idx = 0;
        int next_e_idx = 0;
        int next_f_idx = 0;

        Dictionary<ImVec3, Vert> added_verts = [];
        Dictionary<(Vert, Vert), Edge> added_edges = [];

        foreach (Geom.Face g_face in g_poly.Faces)
        {
            List<Vert> verts = [];
            List<Edge> edges = [];
            List<Edge> forwards_edges = [];
            List<Edge> backwards_edges = [];

            foreach (ImVec3 v in g_face.Verts.Reverse())
            {
                if (!added_verts.ContainsKey(v))
                {
                    Vert vert = new(v.ToVector3());
                    VIdx v_idx = new(next_v_idx++);
                    surf_verts[v_idx] = vert;
                    added_verts[v] = vert;
                }

                verts.Add(added_verts[v]);
            }

            for (int i = 0; i < 3; i++)
            {
                int next_i = (i + 1) % 3;
                Vert vert = verts[i];
                Vert next_vert = verts[next_i];

                Edge edge;

                if (added_edges.ContainsKey((next_vert, vert)))
                {
                    edge = added_edges[(next_vert, vert)];
                    backwards_edges.Add(edge);
                }
                else
                {
                    edge = new(vert, next_vert);
                    forwards_edges.Add(edge);

                    EIdx e_idx = new(next_e_idx++);
                    surf_edges[e_idx] = edge;

                    vert.Edges.Add(edge);
                    next_vert.Edges.Add(edge);

                    added_edges[(vert, next_vert)] = edge;
                }

                edges.Add(edge);
            }

            FIdx f_idx = new(next_f_idx++);
            Face face = new(verts, edges);
            surf_faces[f_idx] = face;

            foreach (Vert vert in face.Verts)
            {
                vert.Faces.Add(face);
            }

            foreach (Edge edge in forwards_edges)
            {
                edge.Forwards = face;
            }

            foreach (Edge edge in backwards_edges)
            {
                edge.Backwards = face;
            }
        }

        foreach (Vert vert in surf_verts.Values)
        {
            VertUtil.SortVertEdgesAndFaces(null, vert, false);
        }

        return new Surface(surf_verts, surf_edges, surf_faces);
    }
}