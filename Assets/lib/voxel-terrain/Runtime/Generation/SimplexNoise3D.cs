using Unity.Burst;
using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// 3D Simplex Noise implementation for procedural terrain generation.
    /// Burst-compatible for use in Jobs.
    /// Based on Stefan Gustavson's implementation.
    /// </summary>
    [BurstCompile]
    public struct SimplexNoise3D
    {
        private readonly int _seed;
        private readonly float _frequency;
        private readonly int _octaves;
        private readonly float _lacunarity;
        private readonly float _persistence;

        public SimplexNoise3D(int seed, float frequency, int octaves, float lacunarity = 2.0f, float persistence = 0.5f)
        {
            _seed = seed;
            _frequency = frequency;
            _octaves = octaves;
            _lacunarity = lacunarity;
            _persistence = persistence;
        }

        /// <summary>
        /// Get fractal noise value at 3D position.
        /// Returns value in range [-1, 1] (approximately).
        /// </summary>
        public float GetNoise(float x, float y, float z)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = _frequency;
            float maxValue = 0f;

            for (int i = 0; i < _octaves; i++)
            {
                total += GetSimplexNoise(x * frequency, y * frequency, z * frequency) * amplitude;

                maxValue += amplitude;
                amplitude *= _persistence;
                frequency *= _lacunarity;
            }

            return total / maxValue;
        }

        /// <summary>
        /// Single octave of 3D simplex noise.
        /// </summary>
        private float GetSimplexNoise(float x, float y, float z)
        {
            // Skewing factors
            const float F3 = 1.0f / 3.0f;
            const float G3 = 1.0f / 6.0f;

            // Skew input space to determine which simplex cell we're in
            float s = (x + y + z) * F3;
            int i = (int)math.floor(x + s);
            int j = (int)math.floor(y + s);
            int k = (int)math.floor(z + s);

            float t = (i + j + k) * G3;
            float X0 = i - t;
            float Y0 = j - t;
            float Z0 = k - t;

            // Distances from cell origin
            float x0 = x - X0;
            float y0 = y - Y0;
            float z0 = z - Z0;

            // Determine which simplex we're in
            int i1, j1, k1; // Offsets for second corner
            int i2, j2, k2; // Offsets for third corner

            if (x0 >= y0)
            {
                if (y0 >= z0)
                { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
                else if (x0 >= z0)
                { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; }
                else
                { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; }
            }
            else
            {
                if (y0 < z0)
                { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; }
                else if (x0 < z0)
                { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; }
                else
                { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
            }

            // Offsets for corners
            float x1 = x0 - i1 + G3;
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;
            float x2 = x0 - i2 + 2.0f * G3;
            float y2 = y0 - j2 + 2.0f * G3;
            float z2 = z0 - k2 + 2.0f * G3;
            float x3 = x0 - 1.0f + 3.0f * G3;
            float y3 = y0 - 1.0f + 3.0f * G3;
            float z3 = z0 - 1.0f + 3.0f * G3;

            // Calculate contributions from each corner
            float n0 = CalculateCornerContribution(i, j, k, x0, y0, z0);
            float n1 = CalculateCornerContribution(i + i1, j + j1, k + k1, x1, y1, z1);
            float n2 = CalculateCornerContribution(i + i2, j + j2, k + k2, x2, y2, z2);
            float n3 = CalculateCornerContribution(i + 1, j + 1, k + 1, x3, y3, z3);

            // Sum and scale to [-1, 1]
            return 32.0f * (n0 + n1 + n2 + n3);
        }

        private float CalculateCornerContribution(int i, int j, int k, float x, float y, float z)
        {
            float t = 0.6f - x * x - y * y - z * z;
            if (t < 0) return 0f;

            t *= t;
            return t * t * Grad(Hash(i, j, k), x, y, z);
        }

        private int Hash(int i, int j, int k)
        {
            // Simple hash combining seed and coordinates
            int hash = _seed;
            hash = hash * 1619 + i;
            hash = hash * 31337 + j;
            hash = hash * 6971 + k;
            return hash & 0xFF;
        }

        private float Grad(int hash, float x, float y, float z)
        {
            // Gradient vectors
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
