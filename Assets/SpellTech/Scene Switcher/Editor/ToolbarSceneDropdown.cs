using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

// This namespace is required for EditorToolbarDropdown in Unity 6
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Toolbars;
#endif

namespace SpellTech.SceneSwitcher
{
#if UNITY_6000_0_OR_NEWER
    
    // =========================================================
    // UNITY 6 IMPLEMENTATION
    // =========================================================

        public static class ToolbarSceneDropdown
    {
        [MainToolbarElement("SpellTech.SceneSwitcher/SceneDropdown", defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement CreateSceneDropdown()
        {
            var icon = EditorGUIUtility.IconContent("d_SceneAsset Icon").image as Texture2D;
            var content = new MainToolbarContent("Scenes", icon, "Quick Scene Switcher");

            return new MainToolbarDropdown(content, ShowDropdown);
        }

        private static void ShowDropdown(Rect rect)
        {
            var menu = new GenericMenu();
            
            List<SceneAsset> bookmarkedScenes = SceneSwitcherToolWindow.LoadBookmarkedScenesFromPrefs();

            if (bookmarkedScenes.Count > 0)
            {
                foreach (SceneAsset scene in bookmarkedScenes)
                {
                    if (scene == null) continue;
                    menu.AddItem(new GUIContent(scene.name), false, () => LoadScene(scene));
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("No Bookmarked Scenes"));
            }
            
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Add Current Scene"), false, () => SceneSwitcherToolWindow.AddCurrentSceneToBookmarksStatic());
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Manage Bookmarks..."), false, () => SceneSwitcherToolWindow.ShowWindow());

            menu.DropDown(rect);
        }

        private static void LoadScene(SceneAsset sceneAsset)
        {
            if (sceneAsset == null) return;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                string path = AssetDatabase.GetAssetPath(sceneAsset);
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }
    }
#else
    
    // =========================================================
    // LEGACY IMPLEMENTATION (Unity 2022 / 2021)
    // =========================================================

    [InitializeOnLoad]
    public static class ToolbarSceneDropdown
    {
        private static readonly Type _toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static VisualElement sceneDropdownContainer;
        
        static ToolbarSceneDropdown()
        {
            EditorApplication.delayCall += InjectButton;
        }

        private static void InjectButton()
        {
            if (!SceneSwitcherSettings.IsToolbarShortcutEnabled) return;
            
            var toolbars = Resources.FindObjectsOfTypeAll(_toolbarType);
            if (toolbars.Length == 0) return;

            var toolbarRoot = (VisualElement)_toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(toolbars[0]);
            if (toolbarRoot == null) return;

            var playModeZone = toolbarRoot.Q(name: "ToolbarZonePlayMode");
            if (playModeZone == null) return;
            
            if (toolbarRoot.Q("scene-switcher-toolbar-button-container") != null) return;

            string scriptFolder = GetScriptFolder();
            if (string.IsNullOrEmpty(scriptFolder))
            {
                LogDebug("Scene Switcher: Could not find the ToolbarSceneDropdown script's folder.");
                return;
            }

            string uxmlPath = Path.Combine(scriptFolder, "ToolbarButton.uxml");
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTree == null)
            {
                LogDebug($"Scene Switcher: ToolbarButton.uxml not found at {uxmlPath}.");
                return;
            }

            var buttonRoot = visualTree.CloneTree();
            sceneDropdownContainer = buttonRoot.Q<VisualElement>("scene-switcher-toolbar-button-container");
            var sceneDropdownLabelButton = buttonRoot.Q<Label>("scene-switcher-toolbar-button");

            string ussPath = Path.Combine(scriptFolder, "ToolbarButton.uss");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet != null)
            {
                sceneDropdownContainer.styleSheets.Add(styleSheet);
                if (SceneSwitcherSettings.IsDebugLoggingEnabled)
                    LogDebug("Scene Switcher: Applied ToolbarButton.uss stylesheet.");
            }
            
            sceneDropdownLabelButton.RegisterCallback<ClickEvent>(evt => ShowSceneDropdown(sceneDropdownLabelButton));

            playModeZone.parent.Insert(playModeZone.parent.IndexOf(playModeZone) + 1, sceneDropdownContainer);
        }

        private static string GetScriptFolder()
        {
            var guids = AssetDatabase.FindAssets("t:script ToolbarSceneDropdown");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return Path.GetDirectoryName(path);
            }
            return null;
        }

        private static void ShowSceneDropdown(VisualElement button)
        {
            var menu = new GenericMenu();
            
            List<SceneAsset> bookmarkedScenes = SceneSwitcherToolWindow.LoadBookmarkedScenesFromPrefs();

            if (bookmarkedScenes.Count > 0)
            {
                foreach (SceneAsset scene in bookmarkedScenes)
                {
                    if (scene == null) continue;
                    menu.AddItem(new GUIContent(scene.name), false, LoadScene, scene);
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("No Bookmarked Scenes"));
            }
            
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Add Current Scene to Bookmarks"), false, SceneSwitcherToolWindow.AddCurrentSceneToBookmarksStatic);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Manage Bookmarks..."), false, () => SceneSwitcherToolWindow.ShowWindow());
            menu.DropDown(button.worldBound);
        }

        private static void LoadScene(object sceneAssetObject)
        {
            var sceneAsset = sceneAssetObject as SceneAsset;
            if (sceneAsset == null) return;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                string path = AssetDatabase.GetAssetPath(sceneAsset);
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }

        private static void LogDebug(string message)
        {
            if (SceneSwitcherSettings.IsDebugLoggingEnabled)
            {
                Debug.Log($"Scene Switcher: {message}");
            }
        }
    }
#endif
}