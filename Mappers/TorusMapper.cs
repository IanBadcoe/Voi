using System;
using System.Collections.Generic;

using Voi.Interfaces;

using Geom_Util.Immutable;

using Godot_Util;

namespace Voi.Mappers
{
    // Y is out from the torus, with a minimum of MinorRadius, and Max of (MajorRadius - MinorRadius) / 2 (to leave space in the middle)
    // X is round the major radius, Z is round the minor radius
    //
    class TorusMapper : IPVMapper
    {
        readonly float MinorRadius;
        readonly float MajorRadius;
        readonly ImVec3Int Cells;
        readonly ImVec3 Perturbation;
        readonly ClRand Random;

        Dictionary<ImVec3, ImVec3Int> ReverseLookup { get; } = new Dictionary<ImVec3, ImVec3Int>();

        public TorusMapper(float maj_radius, float min_radius, ImVec3Int cells, ImVec3 perturbation, ClRand random)
        {
            MinorRadius = min_radius;
            MajorRadius = maj_radius;
            Cells = cells;
            Perturbation = perturbation;
            Random = random;
        }

        #region IPVMapper
        public ImVec3 MakeVertForCell(ImVec3Int cell)
        {
            var perturbed = new ImVec3(
                (cell.X + Random.FloatRange(-Perturbation.X, Perturbation.X) + 0.5f) * MathF.PI * 2 / Cells.X,
                (cell.Y + Random.FloatRange(-Perturbation.Y, Perturbation.Y) + 0.5f) * (MajorRadius - MinorRadius) / 2 / Cells.Y + MinorRadius,
                (cell.Z + Random.FloatRange(-Perturbation.Z, Perturbation.Z) + 0.5f) * MathF.PI * 2 / Cells.Z);

            var sx = MathF.Sin(perturbed.X);
            var cx = MathF.Cos(perturbed.X);

            var sz = MathF.Sin(perturbed.Z);
            var cz = MathF.Cos(perturbed.Z);


            var ret = new ImVec3(
                sx * (MajorRadius + perturbed.Y * sz),
                perturbed.Y * cz,
                cx * (MajorRadius + perturbed.Y * sz));

            ReverseLookup[ret] = cell;

            return ret;
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
            // except on X and Z where we are cyclic
            return cell.X >= 0 && cell.X < Cells.X
                && cell.Y >= 1 && cell.Y < Cells.Y - 1
                && cell.Z >= 0 && cell.Z < Cells.Z;
        }

        public IEnumerable<ImVec3Int> AllGridNeighbours(ImVec3Int pnt, IProgressiveVoronoi.Solidity permitted_for = IProgressiveVoronoi.Solidity.Vacuum)
        {
            foreach (var n in pnt.AllNeighbours)
            {
                var h_n = new ImVec3Int(
                    (n.X + Cells.X) % Cells.X,
                    n.Y,
                    (n.Z + Cells.Z) % Cells.Z);

                if (InRange(h_n, permitted_for))
                {
                    yield return h_n;
                }
            }
        }

        public IEnumerable<ImVec3Int> OrthoGridNeighbours(ImVec3Int pnt, IProgressiveVoronoi.Solidity permitted_for = IProgressiveVoronoi.Solidity.Vacuum)
        {
            foreach (var n in pnt.OrthoNeighbours)
            {
                var h_n = new ImVec3Int(
                    (n.X + Cells.X) % Cells.X,
                    n.Y,
                    (n.Z + Cells.Z) % Cells.Z);

                if (InRange(h_n, permitted_for))
                {
                    yield return h_n;
                }
            }
        }

        public ImVec3Int Vert2Cell(ImVec3 vert)
        {
            ImVec3Int ret = null;

            ReverseLookup.TryGetValue(vert, out ret);

            return ret;
        }

        public ImBounds Bounds()
        {
            float YCombinedRadii = MinorRadius + (MajorRadius - MinorRadius) / 2;
            float XZCombinedRadii = MajorRadius + YCombinedRadii;

            return new ImBounds(
                new ImVec3(XZCombinedRadii, YCombinedRadii, XZCombinedRadii),
                new ImVec3(-XZCombinedRadii, -YCombinedRadii, -XZCombinedRadii));
        }

        public ImVec3Int StepCell(ImVec3Int cell, IPVMapper.CellDir dir, IProgressiveVoronoi.Solidity permitted_for)
        {
            ImVec3Int ret = null;

            switch (dir)
            {
                case IPVMapper.CellDir.PlusX:
                    ret = new ImVec3Int((cell.X + 1) % Cells.X, cell.Y, cell.Z);
                    break;

                case IPVMapper.CellDir.MinusX:
                    ret = new ImVec3Int((cell.X + Cells.X - 1) % Cells.X, cell.Y, cell.Z);
                    break;

                case IPVMapper.CellDir.PlusY:
                    ret = new ImVec3Int(cell.X, cell.Y + 1, cell.Z);
                    break;

                case IPVMapper.CellDir.MinusY:
                    ret = new ImVec3Int(cell.X, cell.Y - 1, cell.Z);
                    break;

                case IPVMapper.CellDir.PlusZ:
                    ret = new ImVec3Int(cell.X, cell.Y, (cell.Z + 1) % Cells.Z);
                    break;


                case IPVMapper.CellDir.MinusZ:
                    ret = new ImVec3Int(cell.X, cell.Y, (cell.Z + Cells.Z - 1) % Cells.Z);
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
