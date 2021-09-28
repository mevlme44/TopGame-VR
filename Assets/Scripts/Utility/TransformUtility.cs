using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class TransformUtility
{
    public static void DestroyChildren(this Transform xf, bool inactiveToo = false) {
        foreach (Transform child in xf) {
            var obj = child.gameObject;
            if (inactiveToo || obj.activeSelf)
                Object.Destroy(obj);
        }
    }

    public static void DestroyChildrenImmediate(this Transform xf, bool inactiveToo = false) {
        for (int i = xf.childCount; i-- > 0;) {
            var child = xf.GetChild(i).gameObject;
            if (inactiveToo || child.activeSelf)
                Object.DestroyImmediate(child);
        }
    }

    public static void DestroyChildrenExcept(this Transform xf, params GameObject[] ignored) {
        foreach (Transform child in xf) {
            var obj = child.gameObject;
            if (Array.IndexOf(ignored, obj) == -1)
                Object.Destroy(obj);
        }
    }

    public static void DestroyChildrenImmediateExcept(this Transform xf, params GameObject[] ignored) {
        for (int i = xf.childCount; i-- > 0;) {
            var child = xf.GetChild(i).gameObject;
            if (Array.IndexOf(ignored, child) == -1)
                Object.DestroyImmediate(child);
        }
    }

    public static void DestroyNextSiblings(this Transform xf) {
        var parent = xf.parent;

        for (int i = xf.GetSiblingIndex() + 1, count = parent.childCount; i < count; ++i)
            Object.Destroy(parent.GetChild(i).gameObject);
    }

    public static void DestroyNextSiblingsImmediate(this Transform xf) {
        var parent = xf.parent;

        for (int i = parent.childCount, stop = xf.GetSiblingIndex(); --i > stop;)
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
    }

    public static void SetAnchor(this RectTransform xf, Vector2 anchor) {
        xf.anchorMin = xf.anchorMax = anchor;
    }

    public static Transform FindInChildren(this Transform xf, string name, bool ignoreCase = false) {
        var comparison = ignoreCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

        foreach (Transform child in xf) {
            if (string.Equals(child.name, name, comparison))
                return child;

            var grandchild = FindInChildren(child, name, ignoreCase);
            if (grandchild)
                return grandchild;
        }
        return null;
    }

    public static string GetPath(this Transform xf) {
        var parts = new List<string>();

        for (; xf; xf = xf.parent)
            parts.Add(xf.name);

        parts.Reverse();
        return string.Join("/", parts);
    }
}
