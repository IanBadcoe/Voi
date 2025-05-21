using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Geom_Util;

namespace Geom
{
    [DebuggerDisplay("Verts: {Verts.Count}")]
    public class Face : IEquatable<Face>
    {
        // for internal usage when we know the data is good and we do not want any rounding errors
        // messing with the hash when, for example, we Reverse one twice...
        private Face(Face old, bool reverse)
        {
            if (reverse)
            {
                Verts = FixPermute(old.Verts.Reverse().ToList());
                Normal = -old.Normal;
            }
            else
            {
                Verts = old.Verts;
                Normal = old.Normal;
            }
        }

        // approx normal is indicative of the hemisphere in which the real normal lies
        public Face(List<ImVec3> verts, ImVec3 approx_normal)
        {
            ImVec3 actual_normal;
            // fix the rotation direction to match the normal
            switch (CalcRotationDirection(verts, approx_normal, out actual_normal))
            {
                case Face.RotationDirection.Clockwise:
                    break;

                case Face.RotationDirection.Anticlockwise:
                    verts.Reverse();
                    break;

                case Face.RotationDirection.Indeterminate:
                    Godot_Util.Util.Assert(false, "Usually means we got a degenerate or near degenerate face");
                    break;
            }

            Normal = actual_normal;

            Verts = FixPermute(verts);
        }

        private List<ImVec3> FixPermute(List<ImVec3> verts)
        {
            var first_vec = verts.Aggregate(verts.First(), (v1, v2) => v1.IsBefore(v2) ? v1 : v2);

            var ret = new List<ImVec3>();

            int j = verts.IndexOf(first_vec);

            for (int i = 0; i < verts.Count; i++)
            {
                ret.Add(verts[j]);

                j = (j + 1) % verts.Count;
            }

            return ret;
        }

        public IReadOnlyList<ImVec3> Verts { get; }
        public ImVec3 Normal { get; }
        public ImVec3 Centre => Verts.Aggregate((v1, v2) => v1 + v2) / Verts.Count;

        public enum RotationDirection
        {
            Clockwise,
            Anticlockwise,
            Indeterminate
        }

        public static RotationDirection CalcRotationDirection(List<ImVec3> verts, ImVec3 approx_normal, out ImVec3 actual_normal)
        {
            // this works by calculating 2* the signed area of the polygon...
            //
            // running round the polygon, summing v(n - 1).Cross(v(n)) gives us a vector whose length is 2x the area
            // (because the cross product of two vectors is the area of the parallelogram they define, and the summation of those parallelograms
            //  +ve and -ve gives us an area independent of where the coordinate system is centred (because the poly is a closed loop)
            //  so if we imagine the case where the centre is in the middle of the triangle:
            //
            //
            //
            //
            //                   a
            //               .  /|\  .
            //            e    / | \    d
            //            |   /  |  \   |
            //            |  /   |   \  |
            //            | /    o    \ |
            //            |/  .     .  \|
            //            b_____________c
            //                .     .
            //                   f
            //
            // then the hexagon adcgbe has twice the area of the triangle abc (and moving o does not change that, although when o leaves
            // the triangle some of the areas go -ve...
            //
            // HOWEVER to make the calculatio easier, we move o on top of our first point, this makes one of the
            // vectors zero length, so two of the cross-products disappear
            //
            // ALSO, by making all the vectors relative, we avoid any precision problems with cross-products of very long vectors
            // that produce very large results which we have to subract from each other and arrive at a relatively very small number...

            var prev = verts[1] - verts[0];

            // as we are relative to Vert[0], all cross-products involving that disappear, so we start with
            // Verts[2] x Verts[1] and go up to Verts[N] x Verts[N-1]
            // e.g. for a triangle the only one left is 2,1 (losing 1,0 and 0,2)
            // and for a square re have 2,1 and 3,1
            ImVec3 accum = new ImVec3();
            for (int i = 2; i < verts.Count; i++)
            {
                var here = verts[i] - verts[0];
                accum += prev.Cross(here);

                prev = here;
            }

            // however, it is not the magnitude (area) of accum we want, but its direction
            //
            // accum is normal to the face such that viewing _down_ it the face looks anticlockwise

            // so if we get the projection on to that of a vector from our centre to any point in the face
            // that will be -ve for anticlockwise, positive for clockwise and close to zero if something is wrong
            // like a degenerate face or the centre being in the plane of the face
            ImVec3 accum_normalised = accum.Normalised();

            var prod = approx_normal.Dot(accum_normalised);

            if (prod > 1e-6f)
            {
                actual_normal = accum_normalised;
                return RotationDirection.Clockwise;
            }

            if (prod < -1e-6f)
            {
                actual_normal = -accum_normalised;
                return RotationDirection.Anticlockwise;
            }

            actual_normal = null;
            return RotationDirection.Indeterminate;
        }

        public bool IsVertInside(ImVec3 vert, float tolerance)
        {
            ImVec3 delta = vert - Verts[0];

            float dot = delta.Dot(Normal);

            // normals point OUT, we are testing if we are IN
            // meaning in || out by less than tolerance
            return dot < tolerance;
        }

        public Face Reversed()
        {
            return new Face(this, true);
        }

        public override int GetHashCode()
        {
            // do not use normal in hash code as it can contain rounding errors
            int ret = 0;

            foreach (var vert in Verts)
            {
                ret = ret * 13 + vert.GetHashCode();
            }

            return ret;
        }

        public bool Equals(Face other)
        {
            // there can be rounding errors on Normals, but
            // if our verts are the same, then our normal should be the same
            if (Verts.Count != other.Verts.Count)
            {
                return false;
            }

            for (int i = 0; i < Verts.Count; i++)
            {
                if (!Verts[i].Equals(other.Verts[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
