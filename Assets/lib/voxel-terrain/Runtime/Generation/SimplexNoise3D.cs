using Unity.Burst;
using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// 3D Simplex Noise generator for procedural terrain generation.
    /// Burst-compatible and SIMD-optimized for high-performance noise generation.
    /// Based on Stefan Gustavson's implementation (public domain).
    ///
    /// Performance targets:
    /// - Single octave: less than 100ns per call
    /// - 4 octaves: less than 300ns per call
    /// </summary>
    [BurstCompile]
    public static class SimplexNoise3D
    {
        /// <summary>
        /// Generate single octave of 3D simplex noise.
        /// </summary>
        /// <param name="x">X coordinate in noise space</param>
        /// <param name="y">Y coordinate in noise space</param>
        /// <param name="z">Z coordinate in noise space</param>
        /// <param name="seed">Random seed for deterministic generation (same seed = same output)</param>
        /// <returns>Noise value in range [-1, 1]</returns>
        [BurstCompile]
        public static float Noise(float x, float y, float z, int seed)
        {
            // Skewing factors for 3D simplex grid
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

            // Determine which simplex we're in (6 possible simplices)
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

            // Offsets for corners in unskewed (x,y,z) space
            float x1 = x0 - i1 + G3;
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;
            float x2 = x0 - i2 + 2.0f * G3;
            float y2 = y0 - j2 + 2.0f * G3;
            float z2 = z0 - k2 + 2.0f * G3;
            float x3 = x0 - 1.0f + 3.0f * G3;
            float y3 = y0 - 1.0f + 3.0f * G3;
            float z3 = z0 - 1.0f + 3.0f * G3;

            // Calculate contributions from each of the four corners
            float n0 = CalculateCornerContribution(i, j, k, x0, y0, z0, seed);
            float n1 = CalculateCornerContribution(i + i1, j + j1, k + k1, x1, y1, z1, seed);
            float n2 = CalculateCornerContribution(i + i2, j + j2, k + k2, x2, y2, z2, seed);
            float n3 = CalculateCornerContribution(i + 1, j + 1, k + 1, x3, y3, z3, seed);

            // Sum and scale to [-1, 1] range
            return 32.0f * (n0 + n1 + n2 + n3);
        }

        /// <summary>
        /// Generate multi-octave fractal noise (fBm - Fractional Brownian Motion).
        /// Combines multiple octaves of simplex noise with decreasing amplitude and increasing frequency.
        /// </summary>
        /// <param name="x">X coordinate in world space</param>
        /// <param name="y">Y coordinate in world space</param>
        /// <param name="z">Z coordinate in world space</param>
        /// <param name="seed">Random seed for deterministic generation</param>
        /// <param name="frequency">Base frequency (scale of noise features). Default: 0.02f</param>
        /// <param name="octaves">Number of noise layers to combine. Default: 4 (ADR-007)</param>
        /// <param name="lacunarity">Frequency multiplier per octave. Default: 2.0f (ADR-007)</param>
        /// <param name="persistence">Amplitude multiplier per octave. Default: 0.5f (ADR-007)</param>
        /// <returns>Noise value in range [-1, 1]</returns>
        [BurstCompile]
        public static float MultiOctave(
            float x, float y, float z,
            int seed,
            float frequency = 0.02f,
            int octaves = 4,
            float lacunarity = 2.0f,
            float persistence = 0.5f)
        {
            float total = 0f;
            float amplitude = 1f;
            float freq = frequency;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                // Sample noise at current frequency and amplitude
                total += Noise(x * freq, y * freq, z * freq, seed + i) * amplitude;

                // Accumulate max value for normalization
                maxValue += amplitude;

                // Update for next octave
                amplitude *= persistence;
                freq *= lacunarity;
            }

            // Normalize to [-1, 1] range
            return total / maxValue;
        }

        /// <summary>
        /// Calculate the contribution of a simplex corner to the noise value.
        /// Uses radial falloff and gradient dot product.
        /// </summary>
        [BurstCompile]
        private static float CalculateCornerContribution(int i, int j, int k, float x, float y, float z, int seed)
        {
            // Radial falloff: t = 0.6 - distance^2
            float t = 0.6f - x * x - y * y - z * z;
            if (t < 0) return 0f;

            // t^4 falloff curve
            t *= t;
            return t * t * Grad(Hash(i, j, k, seed), x, y, z);
        }

        /// <summary>
        /// Deterministic hash function combining seed and 3D grid coordinates.
        /// Same (i, j, k, seed) always produces the same hash value.
        /// </summary>
        [BurstCompile]
        private static int Hash(int i, int j, int k, int seed)
        {
            // Simple multiplicative hash with large primes
            int hash = seed;
            hash = hash * 1619 + i;
            hash = hash * 31337 + j;
            hash = hash * 6971 + k;
            return hash & 0xFF;
        }

        /// <summary>
        /// Gradient calculation using 16 predefined gradient directions.
        /// Maps hash value to gradient vector and computes dot product with distance vector.
        /// </summary>
        [BurstCompile]
        private static float Grad(int hash, float x, float y, float z)
        {
            // Gradient vectors based on edges and diagonals of cube
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
