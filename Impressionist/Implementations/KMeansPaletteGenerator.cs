using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Impressionist.Abstractions;

namespace Impressionist.Implementations
{
    // I'm really appreciate wieslawsoltes's PaletteGenerator. Which make this project possible.
    public class KMeansPaletteGenerator : IThemeColorGenrator, IPaletteGenrator
    {
        public Task<ThemeColorResult> CreateThemeColor(
            Dictionary<Vector3, int> sourceColor,
            bool ignoreWhite = false,
            bool toLab = false
        )
        {
            var builder = sourceColor.AsEnumerable();
            if (ignoreWhite && sourceColor.Count > 1)
            {
                builder = builder.Where(t => t.Key.X <= 250 || t.Key.Y <= 250 || t.Key.Z <= 250);
            }
            if (toLab)
            {
                builder = builder.Select(t => new KeyValuePair<Vector3, int>(t.Key.RgbVectorToLabVector(), t.Value));
            }
            var targetColor = builder.ToDictionary(t => t.Key, t => t.Value);
            var clusters = KMeansCluster(targetColor, 1, false);
            var colorVector = clusters.First();
            if (toLab)
            {
                colorVector = clusters.First().LabVectorToRgbVector();
            }
            bool isDark = colorVector.RgbVectorToHsvColor().SRgbColorIsDark();
            return Task.FromResult(new ThemeColorResult(colorVector, isDark));
        }

        public async Task<PaletteResult> CreatePalette(
            Dictionary<Vector3, int> sourceColor,
            int clusterCount,
            bool ignoreWhite = false,
            bool toLab = false,
            bool useKMeansPp = false
        )
        {
            if (sourceColor.Count == 1)
            {
                ignoreWhite = false;
                useKMeansPp = false;
            }
            var colorResult = await CreateThemeColor(sourceColor, ignoreWhite, toLab);
            var builder = sourceColor.AsEnumerable();
            bool colorIsDark = colorResult.ColorIsDark;
            if (colorIsDark)
            {
                builder = builder.Where(t => t.Key.RgbVectorToHsvColor().SRgbColorIsDark());
            }
            else
            {
                if (!ignoreWhite)
                {
                    builder = builder.Where(t => !t.Key.RgbVectorToHsvColor().SRgbColorIsDark());
                }
                else
                {
                    builder = builder.Where(t =>
                        !t.Key.RgbVectorToHsvColor().SRgbColorIsDark()
                        && (t.Key.X <= 250 || t.Key.Y <= 250 || t.Key.Z <= 250)
                    );
                }
            }
            if (toLab)
            {
                builder = builder.Select(t => new KeyValuePair<Vector3, int>(t.Key.RgbVectorToLabVector(), t.Value));
            }
            var targetColors = builder.ToDictionary(t => t.Key, t => t.Value);
            var clusters = KMeansCluster(targetColors, clusterCount, useKMeansPp);
            var dominantColors = new List<Vector3>();
            foreach (var cluster in clusters)
            {
                var representative = cluster;
                if (toLab)
                {
                    representative = representative.LabVectorToRgbVector();
                }
                dominantColors.Add(representative);
            }
            var result = new List<Vector3>();
            int count = dominantColors.Count;
            for (int i = 0; i < clusterCount; i++)
            {
                // You know, it is always hard to fullfill a palette when you have no enough colors. So please forgive me when placing the same color over and over again.
                result.Add(dominantColors[i % count]);
            }
            return new PaletteResult(result, colorIsDark, colorResult);
        }

        private static Vector3[] KMeansCluster(Dictionary<Vector3, int> colors, int numClusters, bool useKMeansPp)
        {
            // Initialize the clusters, reduces the total number when total colors is less than clusters
            int clusterCount = Math.Min(numClusters, colors.Count);
            var clusters = new List<Dictionary<Vector3, int>>();
            for (int i = 0; i < clusterCount; i++)
            {
                clusters.Add(new Dictionary<Vector3, int>());
            }

            // Select the initial cluster centers randomly
            Vector3[] centers = null;
            if (!useKMeansPp)
            {
                centers = colors.Keys.OrderByDescending(t => Guid.NewGuid()).Take(clusterCount).ToArray();
            }
            else
            {
                centers = KMeansPlusPlusCluster(colors, clusterCount).ToArray();
            }
            // Loop until the clusters stabilize
            bool changed = true;
            while (changed)
            {
                changed = false;
                // Assign each color to the nearest cluster center
                foreach (var color in colors.Keys)
                {
                    var nearest = FindNearestCenter(color, centers);
                    int clusterIndex = Array.IndexOf(centers, nearest);
                    clusters[clusterIndex][color] = colors[color];
                }

                // Recompute the cluster centers
                for (int i = 0; i < Math.Min(numClusters, clusterCount); i++)
                {
                    float sumX = 0f;
                    float sumY = 0f;
                    float sumZ = 0f;
                    float count = 0f;
                    foreach (var color in clusters[i].Keys)
                    {
                        sumX += color.X * colors[color];
                        sumY += color.Y * colors[color];
                        sumZ += color.Z * colors[color];
                        count += colors[color];
                    }

                    float x = sumX / count;
                    float y = sumY / count;
                    float z = sumZ / count;
                    var newCenter = new Vector3(x, y, z);
                    if (!newCenter.Equals(centers[i]))
                    {
                        centers[i] = newCenter;
                        changed = true;
                    }
                }
            }

            // Return the clusters
            return centers;
        }

        private static Vector3 FindNearestCenter(Vector3 color, Vector3[] centers)
        {
            var nearest = centers[0];
            float minDist = float.MaxValue;

            foreach (var center in centers)
            {
                float dist = Vector3.Distance(color, center); // The original version implemented a Distance method by wieslawsoltes himself, I changed that to Vector ones.
                if (dist < minDist)
                {
                    nearest = center;
                    minDist = dist;
                }
            }

            return nearest;
        }

        private static List<Vector3> KMeansPlusPlusCluster(Dictionary<Vector3, int> colors, int numClusters)
        {
            var random = new Random();
            int clusterCount = Math.Min(numClusters, colors.Count);
            var clusters = new List<Vector3>();
            var targetColor = colors.Keys.ToList();
            int index = random.Next(targetColor.Count);
            clusters.Add(targetColor[index]);
            for (int i = 1; i < clusterCount; i++)
            {
                float accumulatedDistances = 0f;
                float[] accDistances = new float[targetColor.Count];
                for (int vectorId = 0; vectorId < targetColor.Count; vectorId++)
                {
                    var minDistanceItem = clusters[0];
                    float minDistance = Vector3.Distance(minDistanceItem, targetColor[vectorId]);
                    for (int clusterIdx = 1; clusterIdx < i; clusterIdx++)
                    {
                        float currentDistance = Vector3.Distance(clusters[clusterIdx], targetColor[vectorId]);
                        if (currentDistance < minDistance)
                        {
                            minDistance = currentDistance;
                        }
                        accumulatedDistances += minDistance * minDistance;
                        accDistances[vectorId] = accumulatedDistances;
                    }
                }
                float targetPoint = (float)random.NextDouble() * accumulatedDistances;
                for (int vectorId = 0; vectorId < targetColor.Count; vectorId++)
                {
                    if (accDistances[vectorId] >= targetPoint)
                    {
                        clusters.Add(targetColor[vectorId]);
                        break;
                    }
                }
            }
            return clusters;
        }
    }
}
