using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inreal
{
    public class VRDebug
    {
        public static bool Enabled = false;
        public static float LineThickness = 0.003f;

        static GameObject EnsureTempObject(Transform parent, string name, Func<GameObject> create = null) {
            if (parent) {
                var child = parent.Find(name);
                if (child) return child.gameObject;
            }
            else {
                var go = GameObject.Find(name);
                if (go) return go;
            }
            var newGo = create != null ? create() : new GameObject();
            newGo.name = name;
            newGo.transform.SetParent(parent, false);
            return newGo;
        }

        static Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

        static Material EnsureMaterial(Color color) {
            if (materials.TryGetValue(color, out var mtl))
                return mtl;

            mtl = new Material(Shader.Find("Unlit/Color"));
            mtl.color = color;
            materials.Add(color, mtl);
            return mtl;

        }

        public static void DrawLine(Transform parent, Vector3 from, Vector3 to, Color color, string name = "_DebugLine") {
            if (!Enabled) return;

            var line = EnsureTempObject(parent, name, () => {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Component.Destroy(cube.GetComponent<BoxCollider>());
                return cube;
            });
            var thickness = parent ? LineThickness / parent.lossyScale.x : LineThickness;
            line.transform.localScale = new Vector3(thickness, thickness, Vector3.Distance(from, to));
            line.transform.localRotation = Quaternion.LookRotation(to - from);
            line.transform.localPosition = 0.5f * (to + from);
            line.GetComponent<Renderer>().sharedMaterial = EnsureMaterial(color);
        }

        static readonly (Vector3 From, Vector3 To, string Name)[] boxLines = new (Vector3, Vector3, string)[] {
            (new Vector3(-1f, -1f, -1f), new Vector3(-1f, 1f, -1f), "BackLeft"),
            (new Vector3(-1f, 1f, -1f), new Vector3(1f, 1f, -1f), "BackTop"),
            (new Vector3(1f, 1f, -1f), new Vector3(1f, -1f, -1f), "BackRight"),
            (new Vector3(1f, -1f, -1f), new Vector3(-1f, -1f, -1f), "BackBottom"),
            (new Vector3(-1f, -1f, 1f), new Vector3(-1f, 1f, 1f), "FrontLeft"),
            (new Vector3(-1f, 1f, 1f), new Vector3(1f, 1f, 1f), "FrontTop"),
            (new Vector3(1f, 1f, 1f), new Vector3(1f, -1f, 1f), "FrontRight"),
            (new Vector3(1f, -1f, 1f), new Vector3(-1f, -1f, 1f), "FrontBottom"),
            (new Vector3(-1f, 1f, -1f), new Vector3(-1f, 1f, 1f), "TopLeft"),
            (new Vector3(1f, 1f, -1f), new Vector3(1f, 1f, 1f), "TopRight"),
            (new Vector3(-1f, -1f, -1f), new Vector3(-1f, -1f, 1f), "BottomLeft"),
            (new Vector3(1f, -1f, -1f), new Vector3(1f, -1f, 1f), "BottmRight"),
        };

        public static void DrawBox(Transform parent, Vector3 center, Vector3 size, Color color, string name = "_DebugBox") {
            if (!Enabled) return;

            var box = EnsureTempObject(parent, name);
            var extent = 0.5f * size;

            foreach (var lineDef in boxLines) {
                var from = center + Vector3.Scale(lineDef.From, extent);
                var to = center + Vector3.Scale(lineDef.To, extent);
                DrawLine(box.transform, from, to, color, lineDef.Name);
            }
        }
    }
}
