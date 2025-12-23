using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

[CustomEditor(typeof(CollisionManager))]
public class CollisionManagerEditor : Editor
{
    private SerializedProperty effectsProp;

    private void OnEnable()
    {
        effectsProp = serializedObject.FindProperty("effects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Thứ tự thực hiện từ trên xuống dưới", EditorStyles.boldLabel);

        // Hiển thị danh sách các Effect với tên Class
        for (int i = 0; i < effectsProp.arraySize; i++)
        {
            SerializedProperty element = effectsProp.GetArrayElementAtIndex(i);
            
            // Lấy tên Class từ thông tin Managed Reference của Unity
            string fullTypeName = element.managedReferenceFullTypename;
            string typeName = "Empty Effect";
            
            if (!string.IsNullOrEmpty(fullTypeName))
            {
                // Tách lấy phần tên Class sau dấu cuối cùng
                typeName = fullTypeName.Split('.').Last();
                // Thêm khoảng trắng vào trước các chữ viết hoa cho dễ đọc (Ví dụ: ApplyForceEffect -> Apply Force Effect)
                typeName = System.Text.RegularExpressions.Regex.Replace(typeName, "([a-z])([A-Z])", "$1 $2");
            }

            // Vẽ PropertyField với nhãn là tên Class thay vì "Element X"
            EditorGUILayout.PropertyField(element, new GUIContent(typeName), true);
        }

        // Nút xóa phần tử cuối nếu muốn tiện lợi hơn
        if (effectsProp.arraySize > 0 && GUILayout.Button("Xóa Effect cuối cùng"))
        {
            effectsProp.arraySize--;
        }

        EditorGUILayout.Space();

        // Nút thêm Effect mới (Menu chuột phải)
        if (GUILayout.Button("Thêm Effect mới"))
        {
            ShowMenu();
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("OnAllEffectsFinished"));

        serializedObject.ApplyModifiedProperties();
    }

    private void ShowMenu()
    {
        GenericMenu menu = new GenericMenu();
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(ICollisionEffect).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

        foreach (var type in types)
        {
            menu.AddItem(new GUIContent(type.Name), false, () => {
                CollisionManager manager = (CollisionManager)target;
                manager.effects.Add((ICollisionEffect)Activator.CreateInstance(type));
                EditorUtility.SetDirty(manager);
                serializedObject.Update();
            });
        }
        menu.ShowAsContext();
    }
}