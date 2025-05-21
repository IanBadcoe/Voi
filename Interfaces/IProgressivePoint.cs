using Voi.Geom.Interfaces;

using Godot;

using Geom_Util.Immutable;

namespace Voi.Interfaces;

public interface IProgressivePoint
{
    bool Exists { get; }            // we need something to return, even if we have no point at this point
    ImVec3 Position { get; }
    ImVec3Int Cell { get; }           // even if we have no point, this is filled in with the centre of the cell asked about
    IProgressiveVoronoi.Solidity Solidity { get; }
    IPolyhedron Polyhedron { get; }
    Geom.Face FaceWithNeighbour(IProgressivePoint neighbour);
    Mesh Mesh { get; }
    IPolyhedron.MeshType MeshType { get; }
    Material Material { get; }
}