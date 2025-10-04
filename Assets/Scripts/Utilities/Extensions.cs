using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Utilities
{
    public static class Extensions
    {
        public static T GetRandomElement<T>(this List<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[Random.Range(0, list.Count)];
        }

        public static T GetRandomElement<T>(this T[] array)
        {
            if (array == null || array.Length == 0) return default;
            return array[Random.Range(0, array.Length)];
        }

        public static void Shuffle<T>(this List<T> list)
        {
            if (list == null || list.Count <= 1) return;

            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static void SetAlpha(this SpriteRenderer sprite, float alpha)
        {
            if (sprite == null) return;
            Color color = sprite.color;
            color.a = Mathf.Clamp01(alpha);
            sprite.color = color;
        }

        public static void SetAlpha(this CanvasGroup canvasGroup, float alpha)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        public static void DestroyChildren(this Transform transform)
        {
            if (transform == null) return;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        public static void DestroyChildrenImmediate(this Transform transform)
        {
            if (transform == null) return;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }
}
