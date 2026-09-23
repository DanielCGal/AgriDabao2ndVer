using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    /// <summary>
    /// Draws each row of the Area Selection Builder district list with the
    /// district's own name on the header, and a count of how many of its three
    /// pictures are still empty.
    ///
    /// Without this the rows read "Element 0" to "Element 5", which makes it easy
    /// to drop one district's art into another district's row - a mistake that
    /// does not fail, it just shows the wrong map.
    /// </summary>
    [CustomPropertyDrawer(typeof(DistrictMapArt))]
    public class DistrictMapArtDrawer : PropertyDrawer
    {
        private static readonly string[] SlotNames =
        {
            "districtHighlight",
            "barangayHighlight",
            "agriculturalAreaSpot"
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float pad = EditorGUIUtility.standardVerticalSpacing;

            Rect header = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, HeaderFor(property), true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = position.y + line + pad;

                y = DrawRow(property, "districtName", position, y, line, pad);

                for (int i = 0; i < SlotNames.Length; i++)
                    y = DrawRow(property, SlotNames[i], position, y, line, pad);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static float DrawRow(
            SerializedProperty property, string relativeName, Rect position, float y, float line, float pad)
        {
            SerializedProperty child = property.FindPropertyRelative(relativeName);
            if (child == null)
                return y;

            EditorGUI.PropertyField(new Rect(position.x, y, position.width, line), child);
            return y + line + pad;
        }

        private static GUIContent HeaderFor(SerializedProperty property)
        {
            SerializedProperty nameProperty = property.FindPropertyRelative("districtName");

            string title = nameProperty == null || string.IsNullOrWhiteSpace(nameProperty.stringValue)
                ? "(unnamed district)"
                : nameProperty.stringValue;

            int missing = 0;
            for (int i = 0; i < SlotNames.Length; i++)
            {
                SerializedProperty slot = property.FindPropertyRelative(SlotNames[i]);
                if (slot == null || slot.objectReferenceValue == null)
                    missing++;
            }

            if (missing == 0)
                return new GUIContent(title + "  -  ready");

            return new GUIContent(title + "  -  " + missing + " of 3 pictures missing");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float pad = EditorGUIUtility.standardVerticalSpacing;

            if (!property.isExpanded)
                return line;

            // Header plus the name field plus the three sprite slots.
            return (line + pad) * (SlotNames.Length + 2);
        }
    }
}
