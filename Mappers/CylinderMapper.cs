using System;
using System.Collections.Generic;

using Voi.Interfaces;

using Geom_Util;

using Godot_Util;

namespace Voi.Mappers
{
    // Y is up the cylinder, X is round it, and Z is out from the centre
    //
    // MinRadius is the internal radius at Z = 0
    // MaxRadius is at Z = Cells.Z - 1
    class CylinderMapper : IPVMapper
    {
        readonly float MinRadius;
        readonly float MaxRadius;
        readonly float Height;
        readonly ImVec3Int Cells;
        readonly ImVec3 Perturbation;
        readonly ClRand Random;

        Dictionary<ImVec3, ImVec3Int> ReverseLookup { get; } = new Dictionary<ImVec3, ImVec3Int>();

        public CylinderMapper(float min_radius, float max_radius, float height, ImVec3Int cells, ImVec3 perturbation, ClRand random)
        {
            MinRadius = min_radius;
            MaxRadius = max_radius;
            Height = height;
            Cells = cells;
            Perturbation = perturbation;
            Random = random;
        }

        #region IPVMapper
        public ImVec3 MakeVertForCell(ImVec3Int cell)
        {
            var perturbed = new ImVec3((cell.X + Random.FloatRange(-Perturbation.X, Perturbation.X) + 0.5f) * MathF.PI * 2 / Cells.X,
                (cell.Y + Random.FloatRange(-Perturbation.Y, Perturbation.Y) + 0.5f) * Height / Cells.Y,
                (cell.Z + Random.FloatRange(-Perturbation.Z, Perturbation.Z) + 0.5f) * (MaxRadius - MinRadius) / Cells.Z + MinRadius);

            var sk = MathF.Sin(perturbed.X);
            var ck = MathF.Cos(perturbed.X);


            var ret = new ImVec3(sk * perturbed.Z, perturbed.Y, ck * perturbed.Z);

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
            // except on X where we are cyclic
            return cell.X >= 0 && cell.X < Cells.X
                && cell.Y >= 1 && cell.Y < Cells.Y - 1
                && cell.Z >= 1 && cell.Z < Cells.Z - 1;
        }

        public IEnumerable<ImVec3Int> AllGridNeighbours(ImVec3Int pnt, IProgressiveVoronoi.Solidity permitted_for = IProgressiveVoronoi.Solidity.Vacuum)
        {
            foreach (var n in pnt.AllNeighbours)
            {
                var h_n = new ImVec3Int(
                    (n.X + Cells.X) % Cells.X,
                    n.Y,
                    n.Z);

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
                    n.Z);

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
            return new ImBounds(new ImVec3(-MaxRadius, 0, -MaxRadius),
                new ImVec3(MaxRadius, Height, MaxRadius));
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
