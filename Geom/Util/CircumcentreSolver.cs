﻿using Geom_Util.Immutable;

namespace Geom.Util
{
    /// <summary>
    /// Given four points in 3D space, solves for a sphere such that all four points
    /// lie on the sphere's surface.
    /// </summary>
    /// <remarks>
    /// Translated from Javascript on http://www.convertalot.com/sphere_solver.html, originally
    /// linked to by http://stackoverflow.com/questions/13600739/calculate-centre-of-sphere-whose-surface-contains-4-points-c.
    /// </remarks>
    public class CircumcentreSolver
    {
        private double m_X0, m_Y0, m_Z0;
        private double m_Radius;

        /// <summary>
        /// The centre of the resulting sphere.
        /// </summary>
        public ImVec3 Centre
        {
            get { return new ImVec3((float)this.m_X0, (float)this.m_Y0, (float)this.m_Z0); }
        }

        /// <summary>
        /// The radius of the resulting sphere.
        /// </summary>
        public double Radius
        {
            get { return this.m_Radius; }
        }

        /// <summary>
        /// Whether the result was a valid sphere.
        /// </summary>
        public bool Valid
        {
            get { return this.m_Radius != 0; }
        }

        /// <summary>
        /// Computes the centre of a sphere such that all four specified points in
        /// 3D space lie on the sphere's surface.
        /// </summary>
        /// <param name="a">The first point (array of 3 doubles for X, Y, Z).</param>
        /// <param name="b">The second point (array of 3 doubles for X, Y, Z).</param>
        /// <param name="c">The third point (array of 3 doubles for X, Y, Z).</param>
        /// <param name="d">The fourth point (array of 3 doubles for X, Y, Z).</param>
        public CircumcentreSolver(ImVec3 a, ImVec3 b, ImVec3 c, ImVec3 d)
        {
            this.Compute(a, b, c, d);
        }

        /// <summary>
        /// Evaluate the determinant.
        /// </summary>
        private void Compute(ImVec3 a, ImVec3 b, ImVec3 c, ImVec3 d)
        {
            double[,] P =
            {
                { a.X, a.Y, a.Z },
                { b.X, b.Y, b.Z },
                { c.X, c.Y, c.Z },
                { d.X, d.Y, d.Z }
            };

            // Compute result sphere.
            this.Sphere(P);
        }

        private void Sphere(double[,] P)
        {
            double m11, m12, m13, m14, m15;
            double[,] a =
                {
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 }
                };

            // Find minor 1, 1.
            for (int i = 0; i < 4; i++)
            {
                a[i, 0] = P[i, 0];
                a[i, 1] = P[i, 1];
                a[i, 2] = P[i, 2];
                a[i, 3] = 1;
            }
            m11 = this.Determinant(a, 4);

            // Find minor 1, 2.
            for (int i = 0; i < 4; i++)
            {
                a[i, 0] = P[i, 0] * P[i, 0] + P[i, 1] * P[i, 1] + P[i, 2] * P[i, 2];
                a[i, 1] = P[i, 1];
                a[i, 2] = P[i, 2];
                a[i, 3] = 1;
            }
            m12 = this.Determinant(a, 4);

            // Find minor 1, 3.
            for (int i = 0; i < 4; i++)
            {
                a[i, 0] = P[i, 0] * P[i, 0] + P[i, 1] * P[i, 1] + P[i, 2] * P[i, 2];
                a[i, 1] = P[i, 0];
                a[i, 2] = P[i, 2];
                a[i, 3] = 1;
            }
            m13 = this.Determinant(a, 4);

            // Find minor 1, 4.
            for (int i = 0; i < 4; i++)
            {
                a[i, 0] = P[i, 0] * P[i, 0] + P[i, 1] * P[i, 1] + P[i, 2] * P[i, 2];
                a[i, 1] = P[i, 0];
                a[i, 2] = P[i, 1];
                a[i, 3] = 1;
            }
            m14 = this.Determinant(a, 4);

            // Find minor 1, 5.
            for (int i = 0; i < 4; i++)
            {
                a[i, 0] = P[i, 0] * P[i, 0] + P[i, 1] * P[i, 1] + P[i, 2] * P[i, 2];
                a[i, 1] = P[i, 0];
                a[i, 2] = P[i, 1];
                a[i, 3] = P[i, 2];
            }
            m15 = this.Determinant(a, 4);

            // Calculate result.
            if (m11 == 0)
            {
                this.m_X0 = 0;
                this.m_Y0 = 0;
                this.m_Z0 = 0;
                this.m_Radius = 0;
            }
            else
            {
                this.m_X0 = 0.5 * m12 / m11;
                this.m_Y0 = -0.5 * m13 / m11;
                this.m_Z0 = 0.5 * m14 / m11;
                this.m_Radius = System.Math.Sqrt(this.m_X0 * this.m_X0 + this.m_Y0 * this.m_Y0 + this.m_Z0 * this.m_Z0 - m15 / m11);
            }
        }

        /// <summary>
        /// Recursive definition of determinate using expansion by minors.
        /// </summary>
        private double Determinant(double[,] a, int n)
        {
            int i, j, j1, j2;
            double d = 0;
            double[,] m =
                    {
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 },
                    { 0, 0, 0, 0 }
                };

            if (n == 2)
            {
                // Terminate recursion.
                d = a[0, 0] * a[1, 1] - a[1, 0] * a[0, 1];
            }
            else
            {
                d = 0;
                double scale = 1;
                for (j1 = 0; j1 < n; j1++) // Do each column.
                {
                    for (i = 1; i < n; i++) // Create minor.
                    {
                        j2 = 0;
                        for (j = 0; j < n; j++)
                        {
                            if (j == j1) continue;
                            m[i - 1, j2] = a[i, j];
                            j2++;
                        }
                    }

                    // Sum (+/-)cofactor * minor.
                    d = d + scale * a[0, j1] * this.Determinant(m, n - 1);
                    scale = -scale;
                }
            }

            return d;
        }
    }
}
