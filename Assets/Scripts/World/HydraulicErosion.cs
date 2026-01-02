using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// Simulates hydraulic erosion using particle-based water droplets
    /// Creates realistic valleys, gullies, and sediment deposits
    /// Based on Sebastian Lague's hydraulic erosion implementation
    /// </summary>
    public static class HydraulicErosion
    {
        /// <summary>
        /// Erode a heightmap using water droplet simulation
        /// </summary>
        /// <param name="heights">2D heightmap array to erode (modified in-place)</param>
        /// <param name="iterations">Number of water droplets to simulate</param>
        /// <param name="erosionStrength">How aggressively terrain is eroded</param>
        /// <param name="sedimentCapacity">Amount of sediment water can carry</param>
        /// <param name="evaporationRate">Water evaporation rate per step</param>
        /// <param name="seed">Random seed for deterministic erosion</param>
        public static void ErodeHeightmap(
            float[,] heights,
            int iterations,
            float erosionStrength,
            float sedimentCapacity,
            float evaporationRate,
            int seed)
        {
            int width = heights.GetLength(0);
            int height = heights.GetLength(1);
            System.Random random = new System.Random(seed);

            // Safety: Don't erode if dimensions are invalid
            if (width < 2 || height < 2)
            {
                Debug.LogWarning("Heightmap too small for erosion, skipping");
                return;
            }

            for (int i = 0; i < iterations; i++)
            {
                // Spawn random droplet
                float x = (float)random.NextDouble() * (width - 1);
                float y = (float)random.NextDouble() * (height - 1);

                SimulateDroplet(heights, x, y, erosionStrength,
                    sedimentCapacity, evaporationRate);
            }
        }

        /// <summary>
        /// Simulate a single water droplet flowing downhill
        /// </summary>
        private static void SimulateDroplet(
            float[,] heights,
            float posX, float posY,
            float erosionStrength,
            float sedimentCapacity,
            float evaporationRate)
        {
            float water = 1f;
            float sediment = 0f;
            float velocity = 1f;

            const int maxLifetime = 30;
            const float gravity = 4f;
            const float depositSpeed = 0.3f;
            const float erodeSpeed = 0.3f;

            for (int lifetime = 0; lifetime < maxLifetime; lifetime++)
            {
                // Get current height using bilinear interpolation
                float currentHeight = GetHeightBilinear(heights, posX, posY);

                // Calculate gradient
                Vector2 gradient = CalculateGradient(heights, posX, posY);

                // Update position (flow downhill)
                posX -= gradient.x;
                posY -= gradient.y;

                // Check bounds
                if (posX < 0 || posX >= heights.GetLength(0) - 1 ||
                    posY < 0 || posY >= heights.GetLength(1) - 1)
                    break;

                // Calculate new height and height difference
                float newHeight = GetHeightBilinear(heights, posX, posY);
                float heightDiff = newHeight - currentHeight;

                // Calculate sediment capacity
                float capacity = Mathf.Max(-heightDiff, 0.01f) * velocity * water * sedimentCapacity;

                // Erode or deposit sediment
                if (sediment > capacity || heightDiff > 0)
                {
                    // Deposit sediment
                    float amountToDeposit = (heightDiff > 0) ?
                        Mathf.Min(heightDiff, sediment) :
                        (sediment - capacity) * depositSpeed;

                    sediment -= amountToDeposit;
                    DepositSediment(heights, posX, posY, amountToDeposit);
                }
                else
                {
                    // Erode terrain
                    float amountToErode = Mathf.Min(
                        (capacity - sediment) * erodeSpeed,
                        -heightDiff
                    ) * erosionStrength;

                    ErodeTerrain(heights, posX, posY, amountToErode);
                    sediment += amountToErode;
                }

                // Update velocity and water
                velocity = Mathf.Sqrt(velocity * velocity + heightDiff * gravity);
                water *= (1 - evaporationRate);
            }
        }

        /// <summary>
        /// Get height at position using bilinear interpolation
        /// </summary>
        private static float GetHeightBilinear(float[,] heights, float x, float y)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            float fx = x - x0;
            float fy = y - y0;

            float h00 = heights[x0, y0];
            float h10 = heights[x1, y0];
            float h01 = heights[x0, y1];
            float h11 = heights[x1, y1];

            return Mathf.Lerp(
                Mathf.Lerp(h00, h10, fx),
                Mathf.Lerp(h01, h11, fx),
                fy
            );
        }

        /// <summary>
        /// Calculate gradient (slope direction) at position
        /// </summary>
        private static Vector2 CalculateGradient(float[,] heights, float x, float y)
        {
            int cellX = Mathf.FloorToInt(x);
            int cellY = Mathf.FloorToInt(y);

            float heightL = heights[Mathf.Max(0, cellX - 1), cellY];
            float heightR = heights[Mathf.Min(heights.GetLength(0) - 1, cellX + 1), cellY];
            float heightD = heights[cellX, Mathf.Max(0, cellY - 1)];
            float heightU = heights[cellX, Mathf.Min(heights.GetLength(1) - 1, cellY + 1)];

            return new Vector2(heightR - heightL, heightU - heightD);
        }

        /// <summary>
        /// Erode terrain at position, distributing to neighboring cells
        /// </summary>
        private static void ErodeTerrain(float[,] heights, float x, float y, float amount)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);

            // Distribute erosion to 4 neighboring cells based on distance
            float fx = x - x0;
            float fy = y - y0;

            heights[x0, y0] -= amount * (1 - fx) * (1 - fy);
            heights[x0 + 1, y0] -= amount * fx * (1 - fy);
            heights[x0, y0 + 1] -= amount * (1 - fx) * fy;
            heights[x0 + 1, y0 + 1] -= amount * fx * fy;
        }

        /// <summary>
        /// Deposit sediment at position, distributing to neighboring cells
        /// </summary>
        private static void DepositSediment(float[,] heights, float x, float y, float amount)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);

            float fx = x - x0;
            float fy = y - y0;

            heights[x0, y0] += amount * (1 - fx) * (1 - fy);
            heights[x0 + 1, y0] += amount * fx * (1 - fy);
            heights[x0, y0 + 1] += amount * (1 - fx) * fy;
            heights[x0 + 1, y0 + 1] += amount * fx * fy;
        }
    }
}
