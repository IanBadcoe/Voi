using System.Collections.Generic;

using Godot_Util;

using Geom_Util.Immutable;

using Voi.Interfaces;

namespace Voi.Mappers
{
    class CubeMapper : IPVMapper
    {
        readonly int Size;
        readonly float Perturbation;
        readonly ClRand Random;

        public CubeMapper(int size, ClRand random, float perturbation)
        {
            Size = size;
            Random = random;
            Perturbation = perturbation;
        }

        #region IPVMapper
        public ImVec3 MakeVertForCell(ImVec3Int cell)
        {
            // our point is in the centre of the cell +/- a randomisation
            return new ImVec3(
                cell.X + Random.FloatRange(-Perturbation, Perturbation) + 0.5f,
                cell.Y + Random.FloatRange(-Perturbation, Perturbation) + 0.5f,
                cell.Z + Random.FloatRange(-Perturbation, Perturbation) + 0.5f);
        }

        public bool InRange(ImVec3Int cell, IProgressiveVoronoi.Solidity permitted_for)
        {
            Util.Assert(permitted_for != IProgressiveVoronoi.Solidity.Unknown, "Asking InRange question about unknown solidity");

            // vacuum points are allowed all the way to the edge
            if (permitted_for == IProgressiveVoronoi.Solidity.Vacuum)
            {
                return cell.X >= 0 && cell.X < Size
                    && cell.Y >= 0 && cell.Y < Size
                    && cell.Z >= 0 && cell.Z < Size;
            }

            // solid points must have room for a vacuum point next to them...
            return cell.X >= 1 && cell.X < Size - 1
                && cell.Y >= 1 && cell.Y < Size - 1
                && cell.Z >= 1 && cell.Z < Size - 1;
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
            return vert.ToImVec3Int();
        }

        public ImBounds Bounds()
        {
            return new ImBounds(new ImVec3(0, 0, 0), new ImVec3(Size, Size, Size));
        }
        #endregion

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
    }
}
