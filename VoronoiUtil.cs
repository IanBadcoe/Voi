
using Voi.Interfaces;

using Geom_Util;

using Godot_Util;

namespace Voi
{

    public static class VoronoiUtil
    {
        public static IDelaunay CreateDelaunay(ImVec3[] verts)
        {
            return CreateDelaunayInternal(verts);
        }

        static Delaunay CreateDelaunayInternal(ImVec3[] verts)
        {
            var d = new Delaunay(1e-3f);

            d.InitialiseWithVerts(verts);

            return d;
        }

        public static IProgressiveVoronoi CreateProgressiveVoronoi(int size, float tolerance, float perturbation, ClRand random)
        {
            return new ProgressiveVoronoi(size, tolerance, perturbation, random);
        }
    }
}
