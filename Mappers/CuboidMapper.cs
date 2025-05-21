using System.Collections.Generic;

using Geom_Util.Immutable;

using Voi.Interfaces;

using Godot_Util;

namespace Voi.Mappers
{
    class CuboidMapper : IPVMapper
    {
        readonly ImVec3 Size;
        readonly ImVec3Int Cells;
        readonly ImVec3 Perturbation;
        readonly ClRand Random;

        public CuboidMapper(ImVec3 size, ImVec3Int cells, ImVec3 perturbation, ClRand random)
        {
            Size = size;
            Cells = cells;
            Perturbation = perturbation;
            Random = random;
        }

        #region IPVMapper
        public ImVec3 MakeVertForCell(ImVec3Int cell)
        {
            // our point is in the centre of the cell +/- a randomisation
            return new ImVec3(
                (cell.X + Random.FloatRange(-Perturbation.X, Perturbation.X) + 0.5f) * Size.X / Cells.X,
                (cell.Y + Random.FloatRange(-Perturbation.Y, Perturbation.Y) + 0.5f) * Size.Y / Cells.Y,
                (cell.Z + Random.FloatRange(-Perturbation.Z, Perturbation.Z) + 0.5f) * Size.Z / Cells.Z);
        }

        public bool InRange(ImVec3Int cell, IProgressiveVoronoi.Solidity permitted_for)
        {
            Util.Assert(permitted_for != IProgressiveVoronoi.Solidity.Unknown, "Asking InRange question about unknown solidity");

            // vacuum points are allowed all the way to the edge
            if (permitted_for == IProgressiveVoronoi.Solidity.Vacuum)
            {
                return cell.X >= 0 && cell.X < Cells.X
                    && cell.Y >= 0 && cell.Y < Cells.Y
                    && cell.Z >= 0 && cell.Z < Cells.Z;
            }

            // solid points must have room for a vacuum point next to them...
            return cell.X >= 1 && cell.X < Cells.X - 1
                && cell.Y >= 1 && cell.Y < Cells.Y - 1
                && cell.Z >= 1 && cell.Z < Cells.Z - 1;
        }

        public IEnumerable<ImVec3Int> AllGridNeighbours(ImVec3Int pnt, IProgressiveVoronoi.Solidity permitted_for = IProgressiveVoronoi.Solidity.Vacuum)
        {
            foreach (var n in pnt.AllNeighbours)
            {
                if (InRange(n, permitted_for))
                {
                    yield return n;
                }
            }
        }

        public IEnumerable<ImVec3Int> OrthoGridNeighbours(ImVec3Int pnt, IProgressiveVoronoi.Solidity permitted_for = IProgressiveVoronoi.Solidity.Vacuum)
        {
            foreach (var n in pnt.OrthoNeighbours)
            {
                if (InRange(n, permitted_for))
                {
                    yield return n;
                }
            }
        }

        public ImVec3Int Vert2Cell(ImVec3 vert)
        {
            return new ImVec3Int(
                (int)(vert.X / Size.X * Cells.X),
                (int)(vert.Y / Size.Y * Cells.Y),
                (int)(vert.Z / Size.Z * Cells.Z));
        }

        public ImBounds Bounds()
        {
            return new ImBounds(new ImVec3(0, 0, 0),
                Size);
        }

        public ImVec3Int StepCell(ImVec3Int cell, IPVMapper.CellDir dir, IProgressiveVoronoi.Solidity permitted_for)
        {
            ImVec3Int ret = null;

            switch (dir)
            {
                case IPVMapper.CellDir.PlusX:
                    ret = new ImVec3Int(cell.X + 1, cell.Y, cell.Z);
                    break;

                case IPVMapper.CellDir.MinusX:
                    ret = new ImVec3Int(cell.X - 1, cell.Y, cell.Z);
                    break;

                case IPVMapper.CellDir.PlusY:
                    ret = new ImVec3Int(cell.X, cell.Y + 1, cell.Z);
                    break;

                case IPVMapper.CellDir.MinusY:
                    ret = new ImVec3Int(cell.X, cell.Y - 1, cell.Z);
                    break;

                case IPVMapper.CellDir.PlusZ:
                    ret = new ImVec3Int(cell.X, cell.Y, cell.Z + 1);
                    break;

                case IPVMapper.CellDir.MinusZ:
                    ret = new ImVec3Int(cell.X, cell.Y, cell.Z - 1);
                    break;
            }

            if (InRange(ret, permitted_for))
            {
                return ret;
            }

            return null;
        }
        #endregion
    }
}
