//#define PROFILE_ON

using Geom;
using Geom.Interfaces;

using Voi.Interfaces;

using Godot;

using Geom_Util;

namespace Voi
{
    class ProgressivePoint : IProgressivePoint
    {
        public ProgressivePoint(ImVec3 pos,
            ImVec3Int cell,
            ProgressiveVoronoi pv,
            IPolyhedron.MeshType mesh_type,
            Material material)
        {
            Cell = cell;
            Voronoi = pv;
            Material = material;

            PolyhedronRW = new Polyhedron(pos, mesh_type, true);
        }

        public readonly ProgressiveVoronoi Voronoi;

        #region IProgressivePoint
        public bool Exists { get; set; } = false;

        public ImVec3 Position => Polyhedron.Centre;

        public ImVec3Int Cell { get; }

        public IProgressiveVoronoi.Solidity Solidity { get; set; } = IProgressiveVoronoi.Solidity.Unknown;

        public IPolyhedron Polyhedron => PolyhedronRW;

        public Geom.Face FaceWithNeighbour(IProgressivePoint neighbour)
        {
            return PolyhedronRW.GetFaceByKey(neighbour);
        }

        public Mesh Mesh
        {
            get
            {
                // on-demand mesh generation...
                if (Polyhedron == null)
                {
                    // if we had one once, scrap it
                    MeshInner = null;

                    return null;
                }

                if (MeshInner == null)
                {
                    MeshInner = MeshUtil.Polyhedron2Mesh(Polyhedron, Voronoi.Cell2Vert(Cell, IProgressiveVoronoi.CellPosition.Origin));
                }

                return MeshInner;
            }
        }

        public IPolyhedron.MeshType MeshType
        {
            get
            {
                return Polyhedron.Type;
            }

            set
            {
                PolyhedronRW.Type = value;
            }
        }

        public Material Material { get; set; }
        #endregion

        public Polyhedron PolyhedronRW { get; }

        private Mesh MeshInner;
    }
}