
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NodeConnection))]
public class NodeConnectionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var targetType = property.FindPropertyRelative("targetType");
        var targetNode = property.FindPropertyRelative("targetNode");
        var targetModule = property.FindPropertyRelative("targetModule");
        var connectionLabel = property.FindPropertyRelative("label");
        var interaction = property.FindPropertyRelative("interactionObject");

        float h = EditorGUIUtility.singleLineHeight;
        float s = EditorGUIUtility.standardVerticalSpacing;

        Rect r1 = new Rect(position.x, position.y, position.width, h);
        Rect r2 = new Rect(position.x, position.y + h + s, position.width, h);
        Rect r3 = new Rect(position.x, position.y + (h + s) * 2, position.width, h);
        Rect r4 = new Rect(position.x, position.y + (h + s) * 3, position.width, h);

        EditorGUI.PropertyField(r1, targetType);

        if (targetType.enumValueIndex == (int)ConnectionTargetType.Node)
            EditorGUI.PropertyField(r2, targetNode, new GUIContent("Target Node"));
        else
            EditorGUI.PropertyField(r2, targetModule, new GUIContent("Target Module"));

        EditorGUI.PropertyField(r3, connectionLabel);
        EditorGUI.PropertyField(r4, interaction);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4 + 10;
    }
}