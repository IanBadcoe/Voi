using System.Collections.Generic;
using System.Diagnostics;

using Geom.Util;

using Geom_Util.Immutable;

namespace Voi.Geom.Util;

[DebuggerDisplay("Centre: ({Centre.X}, {Centre.Y}, {Centre.Z}) Radius: {Radius}")]
public class CircumSphere
{
    public CircumSphere(IReadOnlyList<ImVec3> Verts)
    {
        //var css = new CircumcentreSolver(Verts[0], Verts[1], Verts[2], Verts[3]);  <-- double-precision version
        var css = new CircumcentreSolverFloat(Verts[0], Verts[1], Verts[2], Verts[3]);

        Valid = css.Valid;
        Centre = css.Centre;
        Radius = (float)css.Radius;
        var half_widths = new ImVec3(Radius, Radius, Radius);
        Bounds = new ImBounds(Centre - half_widths, Centre + half_widths);
    }

    public bool Valid { get; }
    public ImVec3 Centre { get; }
    public float Radius { get; }
    public ImBounds Bounds { get; internal set; }

    // we must overlap by more than t to be considered containing,
    // set t -ve if you want to make the test looser
    public bool Contains(ImVec3 p, float t)
    {
        return (Centre - p).Length2() < (Radius - t) * (Radius - t);
    }
}
