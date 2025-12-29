using UnityEngine;
using System.Collections.Generic;

namespace Hearthbound.World
{
    /// <summary>
    /// Generates deterministic river paths that flow from mountains to lakes
    /// Creates a single main river system per terrain
    /// </summary>
    public static class RiverPathGenerator
    {
        /// <summary>
        /// Generate a river path from source (mountains) to destination (lake in plains)
        /// Returns a list of points along the river path in world coordinates
        /// </summary>
        public static List<Vector2> GenerateRiverPath(int seed, float terrainWidth, float terrainLength, 
            System.Func<float, float, float> getContinentalMask)
        {
            List<Vector2> riverPath = new List<Vector2>();
            
            // Use seed to deterministically find lake location (in plains - low continental mask)
            Vector2 lakeCenter = FindLakeLocation(seed, terrainWidth, terrainLength, getContinentalMask);
            
            // Find river source (in mountains - high continental mask)
            Vector2 riverSource = FindRiverSource(seed, terrainWidth, terrainLength, getContinentalMask, lakeCenter);
            
            // Generate meandering path from source to lake
            riverPath = GenerateMeanderingPath(riverSource, lakeCenter, seed);
            
            return riverPath;
        }
        
        /// <summary>
        /// Find a suitable lake location in plains area (low continental mask)
        /// </summary>
        private static Vector2 FindLakeLocation(int seed, float terrainWidth, float terrainLength, 
            System.Func<float, float, float> getContinentalMask)
        {
            // Use seed to pick a deterministic location
            System.Random random = new System.Random(seed + 1000);
            
            // Search for a spot with low continental mask (plains)
            int maxAttempts = 50;
            Vector2 bestLocation = new Vector2(terrainWidth * 0.5f, terrainLength * 0.5f);
            float lowestMask = 1f;
            
            for (int i = 0; i < maxAttempts; i++)
            {
                // Sample random locations, biased toward center
                float x = (float)(random.NextDouble() * 0.6 + 0.2) * terrainWidth; // 20-80% range
                float z = (float)(random.NextDouble() * 0.6 + 0.2) * terrainLength;
                
                float mask = getContinentalMask(x, z);
                
                // Find lowest mask value (flattest plains area)
                if (mask < lowestMask)
                {
                    lowestMask = mask;
                    bestLocation = new Vector2(x, z);
                }
            }
            
            Debug.Log($"🌊 Lake location found at ({bestLocation.x:F1}, {bestLocation.y:F1}) with continental mask {lowestMask:F3}");
            return bestLocation;
        }
        
        /// <summary>
        /// Find river source in mountains (high continental mask), away from lake
        /// </summary>
        private static Vector2 FindRiverSource(int seed, float terrainWidth, float terrainLength, 
            System.Func<float, float, float> getContinentalMask, Vector2 lakeCenter)
        {
            System.Random random = new System.Random(seed + 2000);
            
            int maxAttempts = 50;
            Vector2 bestSource = new Vector2(terrainWidth * 0.5f, terrainLength * 0.5f);
            float highestMask = 0f;
            float minDistanceFromLake = Mathf.Min(terrainWidth, terrainLength) * 0.3f; // At least 30% of terrain size away
            
            for (int i = 0; i < maxAttempts; i++)
            {
                float x = (float)random.NextDouble() * terrainWidth;
                float z = (float)random.NextDouble() * terrainLength;
                
                Vector2 candidate = new Vector2(x, z);
                float distance = Vector2.Distance(candidate, lakeCenter);
                
                // Must be far enough from lake
                if (distance < minDistanceFromLake)
                    continue;
                
                float mask = getContinentalMask(x, z);
                
                // Find highest mask value (mountain area) that's far from lake
                if (mask > highestMask)
                {
                    highestMask = mask;
                    bestSource = candidate;
                }
            }
            
            Debug.Log($"🏔️ River source found at ({bestSource.x:F1}, {bestSource.y:F1}) with continental mask {highestMask:F3}");
            return bestSource;
        }
        
        /// <summary>
        /// Generate a meandering path from source to destination
        /// Uses noise to create natural-looking curves
        /// </summary>
        public static List<Vector2> GenerateMeanderingPath(Vector2 source, Vector2 destination, int seed)
        {
            List<Vector2> path = new List<Vector2>();
            
            // Calculate total distance and number of segments
            float totalDistance = Vector2.Distance(source, destination);
            int numSegments = Mathf.Max(20, (int)(totalDistance / 50f)); // One segment every ~50 units
            
            // Generate path points
            for (int i = 0; i <= numSegments; i++)
            {
                float t = i / (float)numSegments;
                
                // Linear interpolation from source to destination
                Vector2 basePoint = Vector2.Lerp(source, destination, t);
                
                // Add perpendicular meandering using noise
                Vector2 direction = (destination - source).normalized;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                
                // Use noise to create natural meandering
                float noiseValue = Utilities.NoiseGenerator.GetNoise2D(basePoint.x, basePoint.y, seed + 3000, 0.002f);
                float meander = (noiseValue - 0.5f) * 100f; // Meander up to 100 units perpendicular
                
                // Reduce meandering near source and destination for smoother connection
                float fadeIn = Mathf.SmoothStep(0f, 1f, t * 3f); // Fade in over first 33%
                float fadeOut = Mathf.SmoothStep(0f, 1f, (1f - t) * 3f); // Fade out over last 33%
                float fade = Mathf.Min(fadeIn, fadeOut);
                meander *= fade;
                
                Vector2 point = basePoint + perpendicular * meander;
                path.Add(point);
            }
            
            Debug.Log($"🌊 Generated river path with {path.Count} points from ({source.x:F1},{source.y:F1}) to ({destination.x:F1},{destination.y:F1})");
            return path;
        }
        
        /// <summary>
        /// Calculate distance from a point to the nearest point on the river path
        /// Returns distance in world units
        /// </summary>
        public static float DistanceToRiverPath(Vector2 point, List<Vector2> riverPath)
        {
            if (riverPath == null || riverPath.Count == 0)
                return float.MaxValue;
            
            float minDistance = float.MaxValue;
            
            // Check distance to each segment of the path
            for (int i = 0; i < riverPath.Count - 1; i++)
            {
                Vector2 segmentStart = riverPath[i];
                Vector2 segmentEnd = riverPath[i + 1];
                
                float distance = DistanceToLineSegment(point, segmentStart, segmentEnd);
                minDistance = Mathf.Min(minDistance, distance);
            }
            
            return minDistance;
        }
        
        /// <summary>
        /// Calculate distance from point to line segment
        /// </summary>
        private static float DistanceToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 line = lineEnd - lineStart;
            float lineLength = line.magnitude;
            
            if (lineLength < 0.001f)
                return Vector2.Distance(point, lineStart);
            
            Vector2 lineDirection = line / lineLength;
            Vector2 toPoint = point - lineStart;
            
            float projection = Vector2.Dot(toPoint, lineDirection);
            projection = Mathf.Clamp(projection, 0f, lineLength);
            
            Vector2 closestPoint = lineStart + lineDirection * projection;
            return Vector2.Distance(point, closestPoint);
        }
        
        /// <summary>
        /// Get the lake center from the river path (last point)
        /// </summary>
        public static Vector2 GetLakeCenter(List<Vector2> riverPath)
        {
            if (riverPath == null || riverPath.Count == 0)
                return Vector2.zero;
            
            return riverPath[riverPath.Count - 1];
        }
    }
}
