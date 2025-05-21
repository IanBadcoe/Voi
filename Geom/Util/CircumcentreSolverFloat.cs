using Godot;

using Geom_Util.Immutable;

namespace Geom.Util
{
    /// <summary>
    /// Given four points in 3D space, solves for a sphere such that all four points
    /// lie on the sphere's surface.this
    /// </summary>
    /// <remarks>
    /// Translated from Javascript on http://www.convertalot.com/sphere_solver.html, originally
    /// linked to by http://stackoverflow.com/questions/13600739/calculate-centre-of-sphere-whose-surface-contains-4-points-c.
    /// </remarks>
    public class CircumcentreSolverFloat
    {
        private float m_X0, m_Y0, m_Z0;
        private float m_Radius;
        private ImVec3 Offset;

        /// <summary>
        /// The centre of the resulting sphere.
        /// </summary>
        public ImVec3 Centre
        {
            get { return new ImVec3(m_X0, m_Y0, m_Z0) + Offset; }
        }

        /// <summary>
        /// The radius of the resulting sphere.
        /// </summary>
        public float Radius
        {
            get { return m_Radius; }
        }

        /// <summary>
        /// Whether the result was a valid sphere.
        /// </summary>
        public bool Valid
        {
            get { return m_Radius != 0; }
        }

        /// <summary>
        /// Computes the centre of a sphere such that all four specified points in
        /// 3D space lie on the sphere's surface.
        /// </summary>
        /// <param name="a">The first point (array of 3 floats for X, Y, Z).</param>
        /// <param name="b">The second point (array of 3 floats for X, Y, Z).</param>
        /// <param name="c">The third point (array of 3 floats for X, Y, Z).</param>
        /// <param name="d">The fourth point (array of 3 floats for X, Y, Z).</param>
        public CircumcentreSolverFloat(ImVec3 a, ImVec3 b, ImVec3 c, ImVec3 d)
        {
            Compute(a, b, c, d);
        }

        /// <summary>
        /// Evaluate the determinant.
        /// </summary>
        private void Compute(ImVec3 a, ImVec3 b, ImVec3 c, ImVec3 d)
        {
            // try to minimise the magnitude of the numbers we are pushing around in the matrix
            Offset = (a + b + c + d) / 4;

            a = a - Offset;
            b = b - Offset;
            c = c - Offset;
            d = d - Offset;

            float[,] P =
            {
                { a.X, a.Y, a.Z },
                { b.X, b.Y, b.Z },
                { c.X, c.Y, c.Z },
                { d.X, d.Y, d.Z }
            };

            // Compute result sphere.
            Sphere(P);
        }

        private void Sphere(float[,] P)
        {
            float m11, m12, m13, m14, m15;
            float[,] a =
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
            m11 = Determinant(a, 4);

            // Find minor 1, 2.
            for (int i = 0; i < 4; i++)
            {
                a[i, 0] = P[i, 0] * P[i, 0] + P[i, 1] * P[i, 1] + P[i, 2] * P[i, 2];
                a[i, 1] = P[i, 1];
                a[i, 2] = P[i, 2];
                a[i, 3] = 1;
            }
            m12 = Determinant(a, 4);

            // Find minor 1, 3.
            for (int i = 0; i < 4; i++)
            {
                a[i, 0] = P[i, 0] * P[i, 0] + P[i, 1] * P[i, 1] + P[i, 2] * P[i, 2];
                a[i, 1] = P[i, 0];
                a[i, 2] = P[i, 2];
                a[i, 3] = 1;
            }
            m13 = Determinant(a, 4);

            // Find minor 1, 4.
            for (int i = 0; i < 4; i++)
            {
                a[i, 0] = P[i, 0] * P[i, 0] + P[i, 1] * P[i, 1] + P[i, 2] * P[i, 2];
                a[i, 1] = P[i, 0];
                a[i, 2] = P[i, 1];
                a[i, 3] = 1;
            }
            m14 = Determinant(a, 4);

            // Find minor 1, 5.
            for (int i = 0; i < 4; i++)
            {
                a[i, 0] = P[i, 0] * P[i, 0] + P[i, 1] * P[i, 1] + P[i, 2] * P[i, 2];
                a[i, 1] = P[i, 0];
                a[i, 2] = P[i, 1];
                a[i, 3] = P[i, 2];
            }
            m15 = Determinant(a, 4);

            // Calculate result.
            if (m11 == 0)
            {
                m_X0 = 0;
                m_Y0 = 0;
                m_Z0 = 0;
                m_Radius = 0;
            }
            else
            {
                m_X0 = 0.5f * m12 / m11;
                m_Y0 = -0.5f * m13 / m11;
                m_Z0 = 0.5f * m14 / m11;
                m_Radius = Mathf.Sqrt(m_X0 * m_X0 + m_Y0 * m_Y0 + m_Z0 * m_Z0 - m15 / m11);
            }
        }

        /// <summary>
        /// Recursive definition of determinate using expansion by minors.
        /// </summary>
        private float Determinant(float[,] a, int n)
        {
            int i, j, j1, j2;
            float d = 0;
            float[,] m =
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
                float scale = 1;
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
                    d = d + scale * a[0, j1] * Determinant(m, n - 1);
                    scale = -scale;
                }
            }

            return d;
        }
    }
}
