using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieWar.EditorTools.UI
{
    // Small authoring helpers shared by the UI builders. Nothing here runs in a build.
    public static class UiBuildUtility
    {
        private const string LogPrefix = "[UI Build]";

        public static RectTransform CreateNode(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        public static Image AddImage(RectTransform rect, Sprite sprite, Color color, Image.Type type, bool raycastTarget)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = raycastTarget;
            if (type == Image.Type.Sliced)
            {
                image.pixelsPerUnitMultiplier = 1f;
            }

            return image;
        }

        public static TextMeshProUGUI AddText(RectTransform rect, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.text = text;
            return label;
        }

        public static Button AddButton(RectTransform rect, Image targetGraphic)
        {
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = targetGraphic;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = UiSkin.Paper;
            colors.highlightedColor = UiSkin.Paper;
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = UiSkin.Paper;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            return button;
        }

        public static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // Full-width row pinned to one horizontal edge of its parent.
        public static void StretchRow(RectTransform rect, float anchorY, float pivotY, float left, float right, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, anchorY);
            rect.anchorMax = new Vector2(1f, anchorY);
            rect.pivot = new Vector2(0.5f, pivotY);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-right, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }

        // Writes private [SerializeField] members without opening them up to runtime code.
        public static void Bind(Object target, params object[] fieldValuePairs)
        {
            if (target == null)
            {
                Debug.LogError($"{LogPrefix} Bind called with no target.");
                return;
            }

            var serialized = new SerializedObject(target);
            for (int i = 0; i + 1 < fieldValuePairs.Length; i += 2)
            {
                string field = (string)fieldValuePairs[i];
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"{LogPrefix} {target.GetType().Name} has no serialized field '{field}'.", target);
                    continue;
                }

                Assign(property, fieldValuePairs[i + 1], target, field);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Assign(SerializedProperty property, object value, Object target, string field)
        {
            if (property.isArray && property.propertyType == SerializedPropertyType.Generic && value is IList list)
            {
                property.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = (Object)list[i];
                }

                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = (Object)value;
                    return;
                case SerializedPropertyType.Float:
                    property.floatValue = System.Convert.ToSingle(value);
                    return;
                case SerializedPropertyType.Integer:
                    property.intValue = System.Convert.ToInt32(value);
                    return;
                case SerializedPropertyType.Boolean:
                    property.boolValue = (bool)value;
                    return;
                case SerializedPropertyType.String:
                    property.stringValue = (string)value;
                    return;
                case SerializedPropertyType.Color:
                    property.colorValue = (Color)value;
                    return;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = (Vector2)value;
                    return;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = (Vector3)value;
                    return;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = System.Convert.ToInt32(value);
                    return;
                default:
                    Debug.LogError($"{LogPrefix} field '{field}' on {target.GetType().Name} has unsupported type {property.propertyType}.", target);
                    return;
            }
        }

        public static void DestroyChildren(Transform parent, params string[] keepNames)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (ShouldKeep(child.name, keepNames))
                {
                    continue;
                }

                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static bool ShouldKeep(string name, string[] keepNames)
        {
            for (int i = 0; i < keepNames.Length; i++)
            {
                if (keepNames[i] == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
