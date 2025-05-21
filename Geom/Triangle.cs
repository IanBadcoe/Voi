using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Geom_Util.Immutable;

namespace Voi.Geom;

[DebuggerDisplay("({V1.X}, {V1.Y}, {V1.Z}) ({V2.X}, {V2.Y}, {V2.Z}) ({V3.X}, {V3.Y}, {V3.Z})")]
public class Triangle
{
    public Triangle(ImVec3 p1, ImVec3 p2, ImVec3 p3)
    {
        V1 = p1;
        V2 = p2;
        V3 = p3;
    }

    public readonly ImVec3 V1;
    public readonly ImVec3 V2;
    public readonly ImVec3 V3;

    public IEnumerable<ImVec3> Verts
    {
        get
        {
            yield return V1;
            yield return V2;
            yield return V3;
        }
    }

    public ImVec3 Centre => Verts.Aggregate((v1, v2) => v1 + v2) / 3;

    public Geom.Face ToFace(ImVec3 normal)
    {
        return new Geom.Face([.. Verts], normal);
    }
}
