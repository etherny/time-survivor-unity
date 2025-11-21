using NUnit.Framework;
using UnityEngine;
using System.Diagnostics;

namespace TimeSurvivor.Voxel.Terrain.Tests
{
    /// <summary>
    /// Unit tests for SimplexNoise3D ensuring correctness, determinism, performance, and continuity.
    /// Tests validate ADR-007 requirements and noise algorithm properties.
    /// </summary>
    [TestFixture]
    public class SimplexNoise3DTests
    {
        // Epsilon for float comparison (accounting for floating-point precision)
        private const float FloatEpsilon = 1e-6f;

        // Threshold for continuity test (adjacent noise values should be similar)
        private const float ContinuityThreshold = 0.3f;

        // Small delta for testing continuity between nearby points
        private const float SmallDelta = 0.01f;

        /// <summary>
        /// Test 1: Determinism - Same seed and coordinates must produce identical results.
        /// This ensures reproducibility for procedural generation (critical for multiplayer/save systems).
        /// </summary>
        [Test]
        public void Test_Determinism()
        {
            // Test single octave Noise() determinism
            const float x = 10.5f;
            const float y = 20.3f;
            const float z = 15.7f;
            const int seed = 12345;

            float result1 = SimplexNoise3D.Noise(x, y, z, seed);
            float result2 = SimplexNoise3D.Noise(x, y, z, seed);
            float result3 = SimplexNoise3D.Noise(x, y, z, seed);

            Assert.AreEqual(result1, result2, FloatEpsilon,
                "Noise() produced different results for same input (determinism failure)");
            Assert.AreEqual(result2, result3, FloatEpsilon,
                "Noise() produced different results for same input (determinism failure)");

            // Test multi-octave MultiOctave() determinism
            const float frequency = 0.02f;
            const int octaves = 4;
            const float lacunarity = 2.0f;
            const float persistence = 0.5f;

            float multiResult1 = SimplexNoise3D.MultiOctave(x, y, z, seed, frequency, octaves, lacunarity, persistence);
            float multiResult2 = SimplexNoise3D.MultiOctave(x, y, z, seed, frequency, octaves, lacunarity, persistence);
            float multiResult3 = SimplexNoise3D.MultiOctave(x, y, z, seed, frequency, octaves, lacunarity, persistence);

            Assert.AreEqual(multiResult1, multiResult2, FloatEpsilon,
                "MultiOctave() produced different results for same input (determinism failure)");
            Assert.AreEqual(multiResult2, multiResult3, FloatEpsilon,
                "MultiOctave() produced different results for same input (determinism failure)");
        }

        /// <summary>
        /// Test 2: Range - All noise values must be within [-1.0, 1.0] range.
        /// This is a mathematical guarantee of simplex noise and critical for terrain height calculations.
        /// </summary>
        [Test]
        public void Test_Range()
        {
            // Test multiple positions and seeds to ensure range validity
            int[] seeds = { 0, 12345, -999, 42, 99999 };
            float[] testCoords = { 0f, 1f, -10f, 100.5f, -50.3f, 999.99f };

            foreach (int seed in seeds)
            {
                foreach (float x in testCoords)
                {
                    foreach (float y in testCoords)
                    {
                        foreach (float z in testCoords)
                        {
                            // Test single octave Noise()
                            float noiseValue = SimplexNoise3D.Noise(x, y, z, seed);
                            Assert.GreaterOrEqual(noiseValue, -1.0f,
                                $"Noise() returned value < -1.0 at ({x}, {y}, {z}) with seed {seed}");
                            Assert.LessOrEqual(noiseValue, 1.0f,
                                $"Noise() returned value > 1.0 at ({x}, {y}, {z}) with seed {seed}");

                            // Test multi-octave MultiOctave() with default parameters
                            float multiValue = SimplexNoise3D.MultiOctave(x, y, z, seed);
                            Assert.GreaterOrEqual(multiValue, -1.0f,
                                $"MultiOctave() returned value < -1.0 at ({x}, {y}, {z}) with seed {seed}");
                            Assert.LessOrEqual(multiValue, 1.0f,
                                $"MultiOctave() returned value > 1.0 at ({x}, {y}, {z}) with seed {seed}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Test 3: Performance - MultiOctave with 4 octaves should execute in less than 300ns.
        /// NOTE: This test measures non-Burst performance in Unity Editor Play Mode tests.
        /// Burst compilation is only active in builds, so this test may fail in Editor.
        /// Expected: ~1000-2000ns in Editor without Burst, <300ns in builds with Burst.
        /// If this test fails, it's expected behavior in Editor mode.
        /// </summary>
        [Test]
        public void Test_Performance()
        {
            const int warmupIterations = 100;
            const int measurementIterations = 10000;
            const float x = 50.5f;
            const float y = 30.2f;
            const float z = 70.8f;
            const int seed = 54321;
            const int octaves = 4; // ADR-007 default

            // Warmup: ensure JIT compilation and cache warming
            for (int i = 0; i < warmupIterations; i++)
            {
                SimplexNoise3D.MultiOctave(x + i, y, z, seed, octaves: octaves);
            }

            // Measure performance
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < measurementIterations; i++)
            {
                SimplexNoise3D.MultiOctave(x + i, y, z, seed, octaves: octaves);
            }
            stopwatch.Stop();

            double averageNanoseconds = (stopwatch.Elapsed.TotalMilliseconds * 1_000_000.0) / measurementIterations;

            // Log performance results for visibility
            UnityEngine.Debug.Log($"SimplexNoise3D.MultiOctave (4 octaves) average execution time: {averageNanoseconds:F2} ns");
            UnityEngine.Debug.Log($"Target: <300ns (Burst-compiled build), Measured: {averageNanoseconds:F2} ns");

            // NOTE: This assertion will likely fail in Unity Editor without Burst.
            // In a Burst-compiled build, performance should meet the <300ns target.
            // For Editor tests, we use a relaxed threshold of 5000ns (5 microseconds).
            const double editorThresholdNs = 5000.0; // Relaxed threshold for non-Burst Editor
            Assert.LessOrEqual(averageNanoseconds, editorThresholdNs,
                $"MultiOctave (4 octaves) exceeded Editor performance threshold. " +
                $"Measured: {averageNanoseconds:F2}ns, Editor threshold: {editorThresholdNs}ns. " +
                $"Note: Burst-compiled builds should achieve <300ns.");
        }

        /// <summary>
        /// Test 4: Continuity - Noise should be continuous (no sudden jumps).
        /// Adjacent points in noise space should produce similar values (smooth gradient).
        /// This ensures terrain looks natural without discontinuities.
        /// </summary>
        [Test]
        public void Test_Continuity()
        {
            const int seed = 99999;
            const float delta = SmallDelta; // Small step in noise space

            // Test continuity at various positions
            float[] testPositions = { 0f, 10f, -5.5f, 100.123f, -50.789f };

            foreach (float baseX in testPositions)
            {
                foreach (float baseY in testPositions)
                {
                    foreach (float baseZ in testPositions)
                    {
                        // Test single octave Noise() continuity
                        float noiseBase = SimplexNoise3D.Noise(baseX, baseY, baseZ, seed);
                        float noiseXDelta = SimplexNoise3D.Noise(baseX + delta, baseY, baseZ, seed);
                        float noiseYDelta = SimplexNoise3D.Noise(baseX, baseY + delta, baseZ, seed);
                        float noiseZDelta = SimplexNoise3D.Noise(baseX, baseY, baseZ + delta, seed);

                        float diffX = Mathf.Abs(noiseBase - noiseXDelta);
                        float diffY = Mathf.Abs(noiseBase - noiseYDelta);
                        float diffZ = Mathf.Abs(noiseBase - noiseZDelta);

                        Assert.LessOrEqual(diffX, ContinuityThreshold,
                            $"Noise() discontinuity in X direction at ({baseX}, {baseY}, {baseZ}): diff = {diffX}");
                        Assert.LessOrEqual(diffY, ContinuityThreshold,
                            $"Noise() discontinuity in Y direction at ({baseX}, {baseY}, {baseZ}): diff = {diffY}");
                        Assert.LessOrEqual(diffZ, ContinuityThreshold,
                            $"Noise() discontinuity in Z direction at ({baseX}, {baseY}, {baseZ}): diff = {diffZ}");

                        // Test multi-octave MultiOctave() continuity
                        float multiBase = SimplexNoise3D.MultiOctave(baseX, baseY, baseZ, seed);
                        float multiXDelta = SimplexNoise3D.MultiOctave(baseX + delta, baseY, baseZ, seed);
                        float multiYDelta = SimplexNoise3D.MultiOctave(baseX, baseY + delta, baseZ, seed);
                        float multiZDelta = SimplexNoise3D.MultiOctave(baseX, baseY, baseZ + delta, seed);

                        float multiDiffX = Mathf.Abs(multiBase - multiXDelta);
                        float multiDiffY = Mathf.Abs(multiBase - multiYDelta);
                        float multiDiffZ = Mathf.Abs(multiBase - multiZDelta);

                        Assert.LessOrEqual(multiDiffX, ContinuityThreshold,
                            $"MultiOctave() discontinuity in X direction at ({baseX}, {baseY}, {baseZ}): diff = {multiDiffX}");
                        Assert.LessOrEqual(multiDiffY, ContinuityThreshold,
                            $"MultiOctave() discontinuity in Y direction at ({baseX}, {baseY}, {baseZ}): diff = {multiDiffY}");
                        Assert.LessOrEqual(multiDiffZ, ContinuityThreshold,
                            $"MultiOctave() discontinuity in Z direction at ({baseX}, {baseY}, {baseZ}): diff = {multiDiffZ}");
                    }
                }
            }
        }
    }
}
