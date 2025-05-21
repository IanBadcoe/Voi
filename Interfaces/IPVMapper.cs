//#define PROFILE_ON

using System.Collections.Generic;

using Geom_Util;

namespace Voi.Interfaces
{
    // interface for classes which transform ProgressiveVoronoi cells into points in space
    // and vice-versa
    interface IPVMapper
    {
        enum CellDir
        {
            PlusX,
            MinusX,
            PlusY,
            MinusY,
            PlusZ,
            MinusZ,
        }

        // for simple mappings can do this mathematically, for more complex
        // keep a look-up
        ImVec3Int Vert2Cell(ImVec3 vert);

        // not called Cell2Vert
        // because if we are storing anything this is a create operation
        // ProgressiveVoronoi won't be calling this if it alrady has the vert stored
        ImVec3 MakeVertForCell(ImVec3Int cell);

        // is cell within the grid, solid cells may have a smaller range than vacuum ones, owing to
        // solid cells needing to be surrounded with vacuum ones
        bool InRange(ImVec3Int cell, IProgressiveVoronoi.Solidity permitted_for);

        // permitted_for has same effect as in InRange
        IEnumerable<ImVec3Int> AllGridNeighbours(ImVec3Int pnt, IProgressiveVoronoi.Solidity permitted_for = IProgressiveVoronoi.Solidity.Vacuum);

        // permitted_for has same effect as in InRange
        IEnumerable<ImVec3Int> OrthoGridNeighbours(ImVec3Int pnt, IProgressiveVoronoi.Solidity permitted_for = IProgressiveVoronoi.Solidity.Vacuum);

        ImBounds Bounds();

        ImVec3Int StepCell(ImVec3Int cell, CellDir dir, IProgressiveVoronoi.Solidity permitted_for);
    }
}