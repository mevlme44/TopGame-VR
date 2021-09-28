using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace JamSuite.Generative
{
    [ExecuteInEditMode]
    public class LSystemBuilder : MonoBehaviour
    {
        [Range(0, 10)]
        public int iterations = 5;

        public string axiom;

        [System.Serializable]
        public struct Rule { public string from, to; }
        public Rule[] rules;

        public List<string> steps;


        public enum Command
        {
            Spawn,
            Move,
            Turn,
            Push,
            Pop,
        }

        [System.Serializable]
        public class Instruction
        {
            public Command cmd;
            public Vector3 arg;
        }

        [System.Serializable]
        public class Procedure : IEnumerable<Instruction>
        {
            public char code = 't';
            public List<Instruction> instructions;

            public Procedure(char code) {
                this.code = code;
                this.instructions = new List<Instruction>();
            }

            public void Add(Command cmd, float arg0 = 0, float arg1 = 0, float arg2 = 0) {
                Add(cmd, new Vector3(arg0, arg1, arg2));
            }

            public void Add(Command cmd, Vector3 arg) {
                instructions.Add(new Instruction { cmd = cmd, arg = arg });
            }

            public IEnumerator<Instruction> GetEnumerator() {
                return instructions.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        public Procedure[] procedures = new[] {
            new Procedure('[') { Command.Push },
            new Procedure(']') { Command.Pop },
            new Procedure('<') { { Command.Turn, 0, 0, 30 } },
            new Procedure('>') { { Command.Turn, 0, 0, -30 } },
            new Procedure('^') { { Command.Move, 0, 1, 0 } },
            new Procedure('v') { { Command.Move, 0, -1, 0 } },
            new Procedure('@') { { Command.Spawn, 0 } }
        };

        public Transform[] templates;

        public HierarchyRebuildMode rebuildMode = HierarchyRebuildMode.Awake;

        protected Transform parent, lastSpawn;


        public void Rebuild() {
            for (int i = transform.childCount; i-- > 0; ) {
                var child = transform.GetChild(i);
                if (System.Array.IndexOf(templates, child) == -1)
                    DestroyImmediate(child.gameObject);
            }
            Build();
        }


        protected struct State
        {
            public Quaternion rot;
            public Vector3 pos;
        }

        protected State state;
        protected Stack<State> stack = new Stack<State>();

        protected virtual void Build() {
            var expr = axiom;
            steps.Clear();

            for (int i = 0; i < iterations; ++i) {
                foreach (var rule in rules)
                    if (!string.IsNullOrEmpty(rule.from))
                        expr = expr.Replace(rule.from, rule.to);

                steps.Add(expr.Length > 512 ? expr.Remove(512) : expr);
            }

            state = new State {
                rot = transform.rotation,
                pos = transform.position
            };
            stack.Clear();

            foreach (var code in expr) {
                if ('1' <= code && code <= '9') {
                    Spawn(code - '1');
                    continue;
                }
                foreach (var proc in procedures) {
                    if (code != proc.code) continue;
                    foreach (var op in proc) Execute(op);
                    break;
                }
            }
        }

        protected virtual void Execute(Instruction op) {
            switch (op.cmd) {
            case Command.Push: stack.Push(state); break;
            case Command.Pop: if (stack.Count > 0) state = stack.Pop(); break;
            case Command.Turn: state.rot *= Quaternion.Euler(op.arg); break;
            case Command.Move: state.pos += state.rot * op.arg; break;
            case Command.Spawn: Spawn(Mathf.RoundToInt(op.arg[0])); break;
            }
        }


        protected Transform Spawn(int templateIndex) {
            if (0 > templateIndex || templateIndex >= templates.Length) return null;

            lastSpawn = (Transform) Instantiate(templates[templateIndex], state.pos, state.rot);
            lastSpawn.SetParent(parent ? parent : transform, true);
            lastSpawn.gameObject.SetActive(true);

            return lastSpawn;
        }

        protected virtual void Awake() {
            if (!Application.isPlaying) return;
            if ((rebuildMode & HierarchyRebuildMode.Awake) != 0) Rebuild();
        }

        protected virtual void Update() {
            var modeBit = Application.isPlaying
                ? HierarchyRebuildMode.RuntimeUpdate
                : HierarchyRebuildMode.EditorUpdate;

            if ((rebuildMode & modeBit) != 0) Rebuild();
        }
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(LSystemBuilder.Procedure))]
    public class LSystemProcedureDrawer : PropertyDrawer
    {
        private Dictionary<string, ReorderableList> listCache;

        private ReorderableList GetList(SerializedProperty property) {
            if (listCache == null)
                listCache = new Dictionary<string, ReorderableList>();

            ReorderableList list;
            if (listCache.TryGetValue(property.propertyPath, out list)) return list;

            var codeProp = property.FindPropertyRelative("code");
            var listProp = property.FindPropertyRelative("instructions");

            list = new ReorderableList(listProp.serializedObject, listProp, true, true, true, true);

            list.drawElementCallback = (rect, index, active, focused) => {
                EditorGUI.PropertyField(rect, listProp.GetArrayElementAtIndex(index));
            };
            list.drawHeaderCallback = rect => {
                EditorGUI.PropertyField(rect.WithX(rect.x - 6).WithWidth(20), codeProp, GUIContent.none);
            };
            listCache.Add(property.propertyPath, list);

            return list;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            GetList(property).DoList(position);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return GetList(property).GetHeight();
        }
    }

    [CustomPropertyDrawer(typeof(LSystemBuilder.Instruction))]
    public class LSystemInstructionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var cmdProp = property.FindPropertyRelative("cmd");
            var argProp = property.FindPropertyRelative("arg");

            var rect = position.WithXMax(80);
            EditorGUI.PropertyField(rect, cmdProp, GUIContent.none, true);

            rect = position.WithXMin(90);

            switch ((LSystemBuilder.Command) cmdProp.enumValueIndex) {
            case LSystemBuilder.Command.Push:
            case LSystemBuilder.Command.Pop:
                break;

            case LSystemBuilder.Command.Spawn:
                rect.height = 16;
                EditorGUI.PropertyField(rect, argProp.FindPropertyRelative("x"), new GUIContent("Template Index"));
                break;

            default:
                EditorGUI.PropertyField(rect, argProp, GUIContent.none, true);
                break;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(LSystemBuilder.Rule))]
    public class LSystemRuleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position.WithXMin(0), label, property);

            var toRect = position.WithXMin(EditorGUIUtility.labelWidth);
            var arrowRect = toRect.WithXMinMax(toRect.xMin, toRect.xMin + 15);
            var fromRect = position.WithXMax(arrowRect.xMin);

            EditorGUI.PropertyField(fromRect, property.FindPropertyRelative("from"), GUIContent.none, true);
            EditorGUI.HandlePrefixLabel(position, arrowRect, new GUIContent("→"));
            EditorGUI.PropertyField(toRect, property.FindPropertyRelative("to"), GUIContent.none, true);

            EditorGUI.EndProperty();
        }
    }
#endif
}
