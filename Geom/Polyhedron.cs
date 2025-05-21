using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Voi.Geom.Interfaces;

using Geom_Util.Immutable;

namespace Voi.Geom;

[DebuggerDisplay("Faces: {Faces.Count} Verts {Verts.Count}")]
public class Polyhedron : IPolyhedron
{
    public Polyhedron(ImVec3 centre, IPolyhedron.MeshType type, bool must_be_convex)
    {
        Centre = centre;
        Type = type;
        MustBeConvex = must_be_convex;
    }

    // Voronoi polyhedra must be convex, polyhedra built arbitrarily may not be...
    public bool MustBeConvex;

    // faces can be added with a key or without, our total faces are the union of the two sets
    List<Face> FacesRW = new List<Face>();
    Dictionary<object, Face> FacesMapRW = new Dictionary<object, Face>();

    #region IVPolyhedron
    public IEnumerable<Face> Faces => FacesRW.Concat(FacesMapRW.Values);
    public IEnumerable<ImVec3> Verts => Faces.SelectMany(f => f.Verts).Distinct();
    public ImVec3 Centre { get; }
    public IPolyhedron.MeshType Type { get; set; }

    public Face GetFaceByKey(object key)
    {
        Face ret = null;

        FacesMapRW.TryGetValue(key, out ret);

        return ret;
    }
    #endregion

    public void AddFace(object key, Face face)
    {
        if (MustBeConvex)
        {
            Godot_Util.Util.Assert(face.Normal.Dot((face.Centre - Centre).Normalised()) > 0,
                "Face's idea of its normal not pointing away from polygon centre");
        }

        if (key != null)
        {
            FacesMapRW[key] = face;
        }
        else
        {
            FacesRW.Add(face);
        }
    }

    public static Polyhedron Cube(float size)
    {
        var ret = new Polyhedron(new ImVec3(0, 0, 0), IPolyhedron.MeshType.Faces, true);

        float hs = size / 2;

        ret.AddFace(null, new Face(new List<ImVec3>
        {
            new ImVec3(-hs, -hs, -hs),
            new ImVec3( hs, -hs, -hs),
            new ImVec3( hs,  hs, -hs),
            new ImVec3(-hs,  hs, -hs),
        }, new ImVec3(0, 0, -1)));

        ret.AddFace(null, new Face(new List<ImVec3>
        {
            new ImVec3(-hs, -hs,  hs),
            new ImVec3( hs, -hs,  hs),
            new ImVec3( hs,  hs,  hs),
            new ImVec3(-hs,  hs,  hs),
        }, new ImVec3(0, 0, 1)));

        ret.AddFace(null, new Face(new List<ImVec3>
        {
            new ImVec3(-hs, -hs, -hs),
            new ImVec3( hs, -hs, -hs),
            new ImVec3( hs, -hs,  hs),
            new ImVec3(-hs, -hs,  hs),
        }, new ImVec3(0, -1, 0)));

        ret.AddFace(null, new Face(new List<ImVec3>
        {
            new ImVec3(-hs,  hs, -hs),
            new ImVec3( hs,  hs, -hs),
            new ImVec3( hs,  hs,  hs),
            new ImVec3(-hs,  hs,  hs),
        }, new ImVec3(0, 1, 0)));

        ret.AddFace(null, new Face(new List<ImVec3>
        {
            new ImVec3(-hs, -hs, -hs),
            new ImVec3(-hs,  hs, -hs),
            new ImVec3(-hs,  hs,  hs),
            new ImVec3(-hs, -hs,  hs),
        }, new ImVec3(-1, 0, 0)));

        ret.AddFace(null, new Face(new List<ImVec3>
        {
            new ImVec3( hs, -hs, -hs),
            new ImVec3( hs,  hs, -hs),
            new ImVec3( hs,  hs,  hs),
            new ImVec3( hs, -hs,  hs),
        }, new ImVec3(-1, 0, 0)));

        return ret;
    }
}
