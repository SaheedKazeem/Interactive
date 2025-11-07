using UnityEngine;

namespace Interactive.Util
{
    /// <summary>
    /// Helper to find scene objects without using deprecated APIs across Unity versions.
    /// Prefer using cached references; this is mainly for one-off lookups.
    /// </summary>
    public static class SceneObjectFinder
    {
        public static T FindFirst<T>(bool includeInactive = false) where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return includeInactive
                ? Object.FindFirstObjectByType<T>(FindObjectsInactive.Include)
                : Object.FindFirstObjectByType<T>();
#else
            // Older versions: simulate FindFirst via array and pick first
            var arr = Object.FindObjectsOfType<T>(includeInactive);
            return arr != null && arr.Length > 0 ? arr[0] : null;
#endif
        }

        public static T FindAny<T>(bool includeInactive = false) where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return includeInactive
                ? Object.FindAnyObjectByType<T>(FindObjectsInactive.Include)
                : Object.FindAnyObjectByType<T>();
#else
            // Same as FindFirst on older versions
            var arr = Object.FindObjectsOfType<T>(includeInactive);
            return arr != null && arr.Length > 0 ? arr[0] : null;
#endif
        }
    }
}

