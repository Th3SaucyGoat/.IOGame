/*
KINEMATICSOUP CONFIDENTIAL
 Copyright(c) 2014-2022 KinematicSoup Technologies Incorporated 
 All Rights Reserved.
NOTICE:  All information contained herein is, and remains the property of 
KinematicSoup Technologies Incorporated and its suppliers, if any. The 
intellectual and technical concepts contained herein are proprietary to 
KinematicSoup Technologies Incorporated and its suppliers and may be covered by
U.S. and Foreign Patents, patents in process, and are protected by trade secret
or copyright law. Dissemination of this information or reproduction of this
material is strictly forbidden unless prior written permission is obtained from
KinematicSoup Technologies Incorporated.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KS.Unity.Editor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Custom property drawer for <see cref="ksSerializableDictionary{Key, Value}"/></summary>
    [CustomPropertyDrawer(typeof(ksSerializableDictionary<,>))]
    public class ksDictionaryDrawer : PropertyDrawer
    {
        private const float LINE_HEIGHT = 18f;      // The height of a line.
        private const float PADDING = 2f;           // The padding between lines.
        private const float SIZE_WIDTH = 48f;       // The width of the dictionary size field.
        private const float BUTTON_WIDTH = 20f;     // The width of the + and - buttons.
        private const float SLIDER_HEIGHT = 2f;     // The height of the column width slider.
        private const float SLIDER_WIDTH_PAD = 5f;  // The amount to expand the slider width on each side.
        private const float INDENT = 10f;           // Indent spacing.

        /// <summary>Per-property state that persists between Unity serializations.</summary>
        [Serializable]
        private struct State
        {
            /// <summary>The default state.</summary>
            public static State Default = new State(false, .5f);
            /// <summary>Is the property expanded?</summary>
            public bool Expanded;
            /// <summary>
            /// The fraction of the total width used by the key column. The value column uses the remaining width. The
            /// user can drag a slider to change the width of the columns.
            /// </summary>
            public float KeyColumnRatio;

            /// <summary>Constructor</summary>
            /// <param name="expanded">Is the property expanded?</param>
            /// <param name="keyColumnRatio">The fraction of the total width used by the key column.</param>
            public State(bool expanded, float keyColumnRatio)
            {
                Expanded = expanded;
                KeyColumnRatio = keyColumnRatio;
            }

            /// <summary>Checks if two states are equal.</summary>
            /// <param name="lhs"></param>
            /// <param name="rhs"></param>
            /// <returns>True if the states are equal.</returns>
            public static bool operator ==(State lhs, State rhs)
            {
                return lhs.Expanded == rhs.Expanded && lhs.KeyColumnRatio == rhs.KeyColumnRatio;
            }

            /// <summary>Checks if two states are not equal.</summary>
            /// <param name="lhs"></param>
            /// <param name="rhs"></param>
            /// <returns>True if the states are not equal.</returns>
            public static bool operator !=(State lhs, State rhs)
            {
                return !(lhs == rhs);
            }

            /// <summary>Checks if this state is equal to an object.</summary>
            /// <param name="obj"></param>
            /// <returns>True if the object is the same as this state.</returns>
            public override bool Equals(object obj)
            {
                return obj is State && this == (State)obj;
            }

            /// <summary>Gets the hash code for this state.</summary>
            /// <returns>Hash code</returns>
            public override int GetHashCode()
            {
                return KeyColumnRatio.GetHashCode() + 7 * Expanded.GetHashCode();
            }
        }

        /// <summary>Stores property state.</summary>
        private class StateMap : ksInspectorStateMap<State, StateMap>
        {
            /// <summary>The default property state.</summary>
            public override State DefaultState
            {
                get { return State.Default; }
            }
        }

        /// <summary>Draws the property.</summary>
        /// <param name="position">Position to draw at.</param>
        /// <param name="property">Property to draw.</param>
        /// <param name="label">Label for the property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get state associated with the property.
            State state = StateMap.Get().Get(property);

            // Draw the foldout.
            position.height = LINE_HEIGHT;
            EditorGUI.BeginProperty(position, label, property);
            float fullWidth = position.width;
            position.width -= SIZE_WIDTH;
            state.Expanded = EditorGUI.Foldout(position, state.Expanded, label, true);

            position.width = SIZE_WIDTH;
            position.x += fullWidth - SIZE_WIDTH;
            SerializedProperty listProp = property.FindPropertyRelative("m_list");
            SerializedProperty sizeProp = listProp.Copy();
            sizeProp.NextVisible(true);
            EditorGUI.showMixedValue = sizeProp.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int size = EditorGUI.IntField(position, listProp.arraySize);
            EditorGUI.showMixedValue = false;
            // You can't give a tooltip to something without a label, so create an empty label over the int field to
            // hold a tooltip.
            EditorGUI.LabelField(position, new GUIContent("", "Dictionary Size"));
            if (EditorGUI.EndChangeCheck())
            {
                int oldSize = listProp.arraySize;
                listProp.arraySize = size;
                for (int i = oldSize; i < size; i++)
                {
                    SerializedProperty prop = listProp.GetArrayElementAtIndex(i);
                    SetToDefaultValue(prop.FindPropertyRelative("Key"));
                }
            }
            position.width = fullWidth;
            position.x -= fullWidth - SIZE_WIDTH;

            if (state.Expanded)
            {
                fullWidth -= INDENT;
                position.x += INDENT;
                position.width -= INDENT;
                position.y += LINE_HEIGHT + PADDING;

                position.y += LINE_HEIGHT / 2f;
                position.x -= SLIDER_WIDTH_PAD;
                position.width += SLIDER_WIDTH_PAD * 2f;
                state.KeyColumnRatio = GUI.HorizontalSlider(position, state.KeyColumnRatio, 0f, 1f);
                position.x += SLIDER_WIDTH_PAD;
                position.y -= LINE_HEIGHT / 2f;

                float column1 = position.x;
                float column2 = position.x + fullWidth * state.KeyColumnRatio + PADDING / 2f;
                float width1 = fullWidth * state.KeyColumnRatio - PADDING / 2f;
                float width2 = fullWidth * (1 - state.KeyColumnRatio) - PADDING / 2f;

                position.width = width1;
                EditorGUI.LabelField(position, "Keys");
                position.x = column2;
                position.width = width2;
                EditorGUI.LabelField(position, "Values");
                position.y += LINE_HEIGHT + PADDING + SLIDER_HEIGHT;

                ksISerializableDictionary dictionary = 
                    ksEditorUtils.GetPropertyValue<ksISerializableDictionary>(property);
                GUIContent emptyLabel = new GUIContent("");
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    SerializedProperty prop = listProp.GetArrayElementAtIndex(i);
                    prop.NextVisible(true); // key
                    float height1 = GetPropertyHeight(prop);
                    position.x = column1;
                    position.width = width1;
                    position.height = height1;
                    // We use the control name to get the selected index. Prefix key with 'k' and value with 'v'
                    // because control names must be unique.
                    GUI.SetNextControlName("k" + i);
                    EditorGUI.PropertyField(position, prop, emptyLabel, true);
                    if (dictionary != null && dictionary.IsDuplicateKeyAt(i))
                    {
                        EditorGUI.DrawRect(position, new Color(1f, 0f, 0f, .2f));
                    }
                    prop.NextVisible(false); // value
                    float height2 = GetPropertyHeight(prop);
                    position.x = column2;
                    position.width = width2;
                    position.height = height2;
                    GUI.SetNextControlName("v" + i);
                    EditorGUI.PropertyField(position, prop, emptyLabel, true);
                    float height = ksMath.Max(height1, height2, LINE_HEIGHT);
                    position.y += height + PADDING;
                    prop.NextVisible(false);
                }

                // Draw duplicate key message if there are duplicate keys.
                position.width = fullWidth;
                position.height = LINE_HEIGHT;
                position.x = column1;
                if (dictionary != null && dictionary.HasDuplicateKeys())
                {
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.normal.textColor = new Color(1f, .5f, 0f);
                    style.alignment = TextAnchor.MiddleCenter;
                    EditorGUI.LabelField(position, "Warning: Duplicate Keys.", style);
                    position.y += LINE_HEIGHT + PADDING;
                }

                position.x += position.width - BUTTON_WIDTH;
                position.width = BUTTON_WIDTH;
                // Draw the - button to delete an element.
                if (GUI.Button(position, "-"))
                {
                    // Get selected index from control name.
                    int index;
                    string name = GUI.GetNameOfFocusedControl();
                    if (!string.IsNullOrEmpty(name))
                    {
                        // Remove 'k' or 'v'.
                        name = name.Substring(1);
                    }
                    if (!int.TryParse(name, out index))
                    {
                        index = listProp.arraySize - 1;
                    }
                    listProp.DeleteArrayElementAtIndex(index);
                    // Clear focus. This prevents a bug where if you deleted the focused element, it will focus the
                    // next element and show the value of the deleted element.
                    GUI.FocusControl(null);
                }
                position.x -= BUTTON_WIDTH + PADDING;
                // Draw the + button to add a new element.
                if (GUI.Button(position, "+"))
                {
                    listProp.InsertArrayElementAtIndex(listProp.arraySize);
                    SerializedProperty prop = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                    SetToDefaultValue(prop.FindPropertyRelative("Key"));
                }
            }

            // Save state associated with the property.
            StateMap.Get().Set(property, state);
            EditorGUI.EndProperty();
        }

        /// <summary>Gets the height of the property.</summary>
        /// <param name="property">Property to get height for.</param>
        /// <param name="label">Property label.</param>
        /// <returns>The height of the property.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = LINE_HEIGHT;
            if (StateMap.Get().Get(property).Expanded)
            {
                SerializedProperty listProp = property.FindPropertyRelative("m_list");
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    SerializedProperty prop = listProp.GetArrayElementAtIndex(i);
                    height += PADDING + ksMath.Max(
                        EditorGUI.GetPropertyHeight(prop.FindPropertyRelative("Key")),
                        EditorGUI.GetPropertyHeight(prop.FindPropertyRelative("Value")),
                        LINE_HEIGHT);
                }
                height += (LINE_HEIGHT + PADDING) * 2 + SLIDER_HEIGHT;
                ksISerializableDictionary dictionary = 
                    ksEditorUtils.GetPropertyValue<ksISerializableDictionary>(property);
                if (dictionary != null && dictionary.HasDuplicateKeys())
                {
                    height += LINE_HEIGHT + PADDING;
                }
            }
            return height;
        }

        /// <summary>Sets a property to the default value.</summary>
        /// <param name="property">Property to set to the default value.</param>
        public void SetToDefaultValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean: property.boolValue = false; break;
                case SerializedPropertyType.Bounds: property.boundsValue = new Bounds(); break;
                case SerializedPropertyType.BoundsInt: property.boundsIntValue = new BoundsInt(); break;
                case SerializedPropertyType.Character: property.stringValue = " "; break;
                case SerializedPropertyType.Color: property.colorValue = new Color(); break;
                case SerializedPropertyType.Enum: property.enumValueIndex = 0; break;
                case SerializedPropertyType.Float: property.floatValue = 0f; break;
                case SerializedPropertyType.Integer: property.intValue = 0; break;
                case SerializedPropertyType.ObjectReference: property.objectReferenceValue = null; break;
                case SerializedPropertyType.Quaternion: property.quaternionValue = new Quaternion(); break;
                case SerializedPropertyType.Rect: property.rectValue = new Rect(); break;
                case SerializedPropertyType.RectInt: property.rectIntValue = new RectInt(); break;
                case SerializedPropertyType.String: property.stringValue = ""; break;
                case SerializedPropertyType.Vector2: property.vector2Value = new Vector2(); break;
                case SerializedPropertyType.Vector2Int: property.vector2IntValue = new Vector2Int(); break;
                case SerializedPropertyType.Vector3: property.vector3Value = new Vector3(); break;
                case SerializedPropertyType.Vector3Int: property.vector3IntValue = new Vector3Int(); break;
                case SerializedPropertyType.Vector4: property.vector4Value = new Vector4(); break;
            }
        }

        /// <summary>Gets the height of a property.</summary>
        /// <param name="property">Property to get height for.</param>
        /// <returns>The height of the property.</returns>
        public float GetPropertyHeight(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.Color:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.Float:
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.String:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.Vector4:
                    // For single-line fixed-length properties it is faster to return a const than calling
                    // EditorGUI.GetPropertyHeight, so we do this as an optimization. This improves performance when
                    // dictionary is large.
                    return LINE_HEIGHT;
                default:
                    return EditorGUI.GetPropertyHeight(property);
            }
        }
    }
}