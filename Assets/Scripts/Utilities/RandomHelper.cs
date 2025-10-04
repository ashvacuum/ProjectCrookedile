using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Utilities
{
    public static class RandomHelper
    {
        public static T WeightedRandom<T>(List<T> items, List<float> weights)
        {
            if (items == null || weights == null || items.Count == 0 || items.Count != weights.Count)
            {
                Debug.LogError("Invalid weighted random parameters");
                return default;
            }

            float totalWeight = 0f;
            foreach (float weight in weights)
            {
                totalWeight += weight;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return items[i];
                }
            }

            return items[items.Count - 1];
        }

        public static bool Chance(float probability)
        {
            return Random.value <= Mathf.Clamp01(probability);
        }

        public static int Range(int min, int max)
        {
            return Random.Range(min, max + 1);
        }

        public static float Range(float min, float max)
        {
            return Random.Range(min, max);
        }

        public static Vector2 RandomPointInCircle(float radius)
        {
            return Random.insideUnitCircle * radius;
        }

        public static Vector3 RandomPointInSphere(float radius)
        {
            return Random.insideUnitSphere * radius;
        }
    }
}
