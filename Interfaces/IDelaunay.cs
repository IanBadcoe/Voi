﻿using System;
using System.Collections.Generic;

using Voi.Geom.Interfaces;

using Geom_Util.Immutable;

namespace Voi.Interfaces;

public interface IDelaunay : IPolyhedronSet
{
    public IDelaunay Clone();
    public IEnumerable<DTetrahedron> Tets { get; }
    public IEnumerable<DTetrahedron> TetsForVert(ImVec3 vert);
    public IEnumerable<ImVec3> Verts { get; }
    public float Tolerance { get; }
    IPolyhedron OuterSurface();
}
