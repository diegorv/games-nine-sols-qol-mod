using UnityEngine;

namespace NineSolsQoL.Core;

internal static class UnityExtensions {
    // "Parent/Child/Object", to identify objects in the log
    public static string HierarchyPath(this Transform transform) {
        var path = transform.name;
        for (var parent = transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
        return path;
    }

    public static string Describe(this Bounds bounds) => $"{bounds.min}..{bounds.max}";
}
