using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEditor.TerrainTools;
using UnityEditor.SearchService;
using Unity.VisualScripting;

namespace SpellTech.SceneSwitcher
{
    #region Main Tool Window
    public class SceneSwitcherToolWindow : EditorWindow
    {
        private List<SceneAsset> bookmarkedScenes = new List<SceneAsset>();
        private List<EditorBuildSettingsScene> scenesInBuild = new List<EditorBuildSettingsScene>();

        // UI Elements
        internal VisualElement root;
        private VisualElement mainContainer;
        private VisualElement listContainer;
        private ListView sceneListView;
        private DropdownField modeDropdown;
        private TextField searchField;

        // Buttons
        private Button loadButton;
        private Button additiveButton;
        private Button pingInFolderButton;
        private Button addCurrentBookmarkButton;
        private Button removeBookmarkButton;
        private Button removeAllBookmarksButton;
        private Button settingsButton;
        private Button creditsButton;

        private VisualElement emptyListHint;

        public StyleSheet toolStyleSheet;

        private int selectedMode = 0;
        private const string BookmarkedScenesKey = "BookmarkedScenes";
        private const string DragHoverClassName = "drag-hover-active";

        internal static string scriptFolder;

        private const float MinWidthForHorizontalLayout = 480f;
        private bool isHorizontalLayout = true;

        [MenuItem("Tools/SpellTech/Scene Switcher Tool")]
        public static void ShowWindow()
        {
            SceneSwitcherToolWindow wnd = GetWindow<SceneSwitcherToolWindow>();
            wnd.titleContent = new GUIContent("Scene Switcher");
            wnd.minSize = new Vector2(280, 300);
        }
        public static void RepaintWindow()
        {
            var window = GetWindow<SceneSwitcherToolWindow>();
            if (window != null)
            {   
                LoadBookmarkedScenesFromPrefs();
                window.Repaint();
            }
        }
        public void OnEnable()
        {
            bookmarkedScenes = LoadBookmarkedScenesFromPrefs();
            scenesInBuild = EditorBuildSettings.scenes.ToList();
        }

        public void CreateGUI()
        {
            root = rootVisualElement;

            MonoScript thisScript = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(thisScript);
            scriptFolder = Path.GetDirectoryName(scriptPath);
            string uxmlPath = Path.Combine(scriptFolder, "SceneSwitcherTool.uxml");
            string ussPath = Path.Combine(scriptFolder, "SceneSwitcherTool.uss");

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTree == null)
            {
                root.Add(new Label("Could not load SceneSwitcherTool.uxml. Make sure it's in the same folder as the script."));
                return;
            }
            visualTree.CloneTree(root);

            toolStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (toolStyleSheet != null)
            {
                root.styleSheets.Add(toolStyleSheet);
            }

            // Query UI elements
            mainContainer = root.Q<VisualElement>("main-container-scene-switch");
            listContainer = root.Q<VisualElement>("list-container");
            sceneListView = root.Q<ListView>("scene-list");
            modeDropdown = root.Q<DropdownField>("mode-dropdown");
            searchField = root.Q<TextField>("search-field");
            loadButton = root.Q<Button>("load-button");
            additiveButton = root.Q<Button>("additive-button");
            pingInFolderButton = root.Q<Button>("ping-in-folder-button");
            addCurrentBookmarkButton = root.Q<Button>("add-current-bookmark-button");
            removeBookmarkButton = root.Q<Button>("remove-bookmark-button");
            removeAllBookmarksButton = root.Q<Button>("remove-all-bookmarks-button");
            creditsButton = root.Q<Button>("credits-button");
            settingsButton = root.Q<Button>("settings-button");
            emptyListHint = root.Q<VisualElement>("empty-list-hint");

            SetupIcons(root);
            SetupSceneListView();
            SetupModeDropdown();
            RegisterCallbacks();

            root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);

            UpdateLayoutClasses();
            RefreshSceneList();
            UpdateButtonsState();
        }

        public void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            ForceLayoutUpdate(evt.newRect.width);
        }

        public void ForceLayoutUpdate(float currentWidth)
        {
            
            if (mainContainer == null) return;

            
            bool newIsHorizontal = currentWidth > MinWidthForHorizontalLayout;
            //Debug.Log($"ForceLayoutUpdate called. currentWidth = {currentWidth}, newIsHorizontal = {newIsHorizontal}, previous isHorizontalLayout = {isHorizontalLayout}");
            if (newIsHorizontal != isHorizontalLayout)
            {
                isHorizontalLayout = newIsHorizontal;
                UpdateLayoutClasses();
            }
        }

        private void UpdateLayoutClasses()
        {
            if (mainContainer == null) return;
            
            switch (SceneSwitcherSettings.FixedLayout)
            {
                case SceneSwitcherSettings.LayoutMode.FixedHorizontal:
                    isHorizontalLayout = true;
                    break;
                case SceneSwitcherSettings.LayoutMode.FixedVertical:
                    isHorizontalLayout = false;
                    break;
                default:
                    break;
            }
            mainContainer.EnableInClassList("wide-layout", isHorizontalLayout);
            mainContainer.EnableInClassList("narrow-layout", !isHorizontalLayout);
        }


        private void SetupIcons(VisualElement root)
        {
            const float iconSize = 16f; // Define a constant size for all icons

            root.Q<Image>("search-icon").image = EditorGUIUtility.IconContent("d_Search Icon").image;
            root.Q<Image>("search-icon").style.width = iconSize;
            root.Q<Image>("search-icon").style.height = iconSize;

            root.Q<Image>("load-icon").image = EditorGUIUtility.IconContent("d_PlayButton").image;
            root.Q<Image>("load-icon").style.width = iconSize;
            root.Q<Image>("load-icon").style.height = iconSize;

            root.Q<Image>("additive-icon").image = EditorGUIUtility.IconContent("d_Toolbar Plus").image;
            root.Q<Image>("additive-icon").style.width = iconSize;
            root.Q<Image>("additive-icon").style.height = iconSize;

            root.Q<Image>("ping-icon").image = EditorGUIUtility.IconContent("d_FolderOpened Icon").image;
            root.Q<Image>("ping-icon").style.width = iconSize;
            root.Q<Image>("ping-icon").style.height = iconSize;

            root.Q<Image>("add-current-icon").image = EditorGUIUtility.IconContent("d_Toolbar Plus More").image;
            root.Q<Image>("add-current-icon").style.width = iconSize;
            root.Q<Image>("add-current-icon").style.height = iconSize;

            root.Q<Image>("remove-selected-icon").image = EditorGUIUtility.IconContent("d_Toolbar Minus").image;
            root.Q<Image>("remove-selected-icon").style.width = iconSize;
            root.Q<Image>("remove-selected-icon").style.height = iconSize;

            root.Q<Image>("remove-all-icon").image = EditorGUIUtility.IconContent("d_TreeEditor.Trash").image;
            root.Q<Image>("remove-all-icon").style.width = iconSize;
            root.Q<Image>("remove-all-icon").style.height = iconSize;

            root.Q<Image>("credits-icon").image = EditorGUIUtility.IconContent("d_Help").image;
            root.Q<Image>("credits-icon").style.width = iconSize;
            root.Q<Image>("credits-icon").style.height = iconSize;

            root.Q<Image>("settings-icon").image = EditorGUIUtility.IconContent("d_SettingsIcon").image;
            root.Q<Image>("settings-icon").style.width = iconSize;
            root.Q<Image>("settings-icon").style.height = iconSize;
        }

        public  void RegisterCallbacks()
        {
            searchField.RegisterValueChangedCallback(evt => RefreshSceneList());
            loadButton.clicked += () => LoadSelectedScene(OpenSceneMode.Single);
            additiveButton.clicked += () => LoadSelectedScene(OpenSceneMode.Additive);
            pingInFolderButton.clicked += PingSelectedSceneInFolder;
            addCurrentBookmarkButton.clicked += AddCurrentSceneToBookmarks;
            removeBookmarkButton.clicked += RemoveSelectedBookmark;
            removeAllBookmarksButton.clicked += RemoveAllBookmarkedScenes;
            creditsButton.clicked += () => CreditsWindow.ShowWindow();
            settingsButton.clicked += () => SettingsWindow.ShowWindow();

            // Register drag events on BOTH the container and the list view for robust dropping
            listContainer.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            listContainer.RegisterCallback<DragPerformEvent>(OnDragPerform);
            listContainer.RegisterCallback<DragLeaveEvent>(OnDragLeave);

            sceneListView.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            sceneListView.RegisterCallback<DragPerformEvent>(OnDragPerform);
            sceneListView.RegisterCallback<DragLeaveEvent>(OnDragLeave);
        }

        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (selectedMode != 0)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }

            bool validDrag = DragAndDrop.objectReferences.Any(obj =>
                obj is SceneAsset ||
                (obj is GameObject go && go.scene.IsValid() && !string.IsNullOrEmpty(go.scene.path)));

            if (validDrag)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                listContainer.AddToClassList(DragHoverClassName);
                evt.StopPropagation();
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            listContainer.RemoveFromClassList(DragHoverClassName);
            if (selectedMode != 0) return;

            DragAndDrop.AcceptDrag();
            bool scenesAdded = false;

            foreach (Object draggedObject in DragAndDrop.objectReferences)
            {
                SceneAsset sceneToAdd = null;
                if (draggedObject is SceneAsset sceneAsset) sceneToAdd = sceneAsset;
                else if (draggedObject is GameObject go && go.scene.IsValid() && !string.IsNullOrEmpty(go.scene.path))
                {
                    sceneToAdd = AssetDatabase.LoadAssetAtPath<SceneAsset>(go.scene.path);
                }
                if (sceneToAdd != null && AddSceneToBookmarksInternal(sceneToAdd)) scenesAdded = true;
            }

            if (scenesAdded)
            {
                SaveBookmarkedScenes();
                RefreshSceneList();
            }
            evt.StopPropagation();
        }

        private void OnDragLeave(DragLeaveEvent evt)
        {
            listContainer.RemoveFromClassList(DragHoverClassName);
        }

        private void SetupModeDropdown()
        {
            modeDropdown.choices = new List<string> { "Bookmarks", "Scenes In Build" };
            modeDropdown.index = 0;
            modeDropdown.RegisterValueChangedCallback(evt =>
            {
                selectedMode = modeDropdown.index;
                sceneListView.selectedIndex = -1;
                listContainer.RemoveFromClassList(DragHoverClassName);
                RefreshSceneList();
                UpdateButtonsState();
            });
        }

        public void SetupSceneListView()
        {
            sceneListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            sceneListView.selectionType = SelectionType.Single;
            sceneListView.makeItem = () =>
            {
                var itemRoot = new VisualElement();
                itemRoot.AddToClassList("list-item-container");
                var icon = new Image { name = "list-item-icon", image = EditorGUIUtility.IconContent("d_UnityEditor.GameView").image };
                var label = new Label { name = "list-item-label" };
                itemRoot.Add(icon);
                itemRoot.Add(label);
                return itemRoot;
            };
            sceneListView.bindItem = (element, i) =>
            {
                var label = element.Q<Label>("list-item-label");
                string sceneName = "", scenePath = "";
                if (selectedMode == 0)
                {
                    var scene = (SceneAsset)sceneListView.itemsSource[i];
                    sceneName = scene.name;
                    scenePath = AssetDatabase.GetAssetPath(scene);
                }
                else
                {
                    var scene = (EditorBuildSettingsScene)sceneListView.itemsSource[i];
                    scenePath = scene.path;
                    sceneName = Path.GetFileNameWithoutExtension(scenePath);
                }
                label.text = sceneName;
                label.tooltip = scenePath;
            };
            sceneListView.selectionChanged += (selection) => UpdateButtonsState();
            sceneListView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0) LoadSelectedScene(OpenSceneMode.Single);
            });
        }

        public void RefreshSceneList()
        {
            bookmarkedScenes = bookmarkedScenes.Where(s => s != null).ToList();
            System.Collections.IList itemsSource;
            if (selectedMode == 0) itemsSource = bookmarkedScenes.Where(s => IsSceneVisible(s.name)).ToList();
            else
            {
                scenesInBuild = EditorBuildSettings.scenes.ToList();
                itemsSource = scenesInBuild.Where(s => IsSceneVisible(Path.GetFileNameWithoutExtension(s.path))).ToList();
            }
            sceneListView.itemsSource = itemsSource;
            sceneListView.Rebuild();
            bool showHint = selectedMode == 0 && itemsSource.Count == 0;
            emptyListHint.style.display = showHint ? DisplayStyle.Flex : DisplayStyle.None;
            sceneListView.style.display = showHint ? DisplayStyle.None : DisplayStyle.Flex;
            UpdateButtonsState();
        }

        private void LoadSelectedScene(OpenSceneMode mode)
        {
            if (sceneListView.selectedIndex < 0 || sceneListView.itemsSource == null) return;
            string scenePath = "";
            if (selectedMode == 0)
            {
                var scene = (SceneAsset)sceneListView.itemsSource[sceneListView.selectedIndex];
                scenePath = AssetDatabase.GetAssetPath(scene);
            }
            else
            {
                var scene = (EditorBuildSettingsScene)sceneListView.itemsSource[sceneListView.selectedIndex];
                scenePath = scene.path;
            }
            if (!string.IsNullOrEmpty(scenePath) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath, mode);
            }
        }

        private void PingSelectedSceneInFolder()
        {
            if (sceneListView.selectedIndex < 0 || sceneListView.itemsSource == null) return;
            Object sceneAsset = null;
            if (selectedMode == 0) sceneAsset = (SceneAsset)sceneListView.itemsSource[sceneListView.selectedIndex];
            else
            {
                var scene = (EditorBuildSettingsScene)sceneListView.itemsSource[sceneListView.selectedIndex];
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            }
            if (sceneAsset != null) EditorGUIUtility.PingObject(sceneAsset);
        }

        private bool IsSceneVisible(string sceneName) => string.IsNullOrEmpty(searchField.value) || sceneName.ToLower().Contains(searchField.value.ToLower());

        private void UpdateButtonsState()
        {
            bool isSceneSelected = sceneListView.selectedIndex >= 0;
            bool isBookmarkMode = selectedMode == 0;
            loadButton.SetEnabled(isSceneSelected);
            additiveButton.SetEnabled(isSceneSelected);
            pingInFolderButton.SetEnabled(isSceneSelected);
            addCurrentBookmarkButton.SetEnabled(isBookmarkMode);
            removeBookmarkButton.SetEnabled(isSceneSelected && isBookmarkMode);
            removeAllBookmarksButton.SetEnabled(isBookmarkMode && bookmarkedScenes.Count > 0);
        }

        private void AddCurrentSceneToBookmarks()
        {
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(currentScenePath))
            {
                EditorUtility.DisplayDialog("No Scene", "The current scene has not been saved yet. Please save the scene first.", "OK");
                return;
            }
            AddSceneToBookmarks(AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScenePath));
        }

        private void AddSceneToBookmarks(SceneAsset sceneAsset)
        {
            if (AddSceneToBookmarksInternal(sceneAsset))
            {
                SaveBookmarkedScenes();
                RefreshSceneList();
            }
        }

        private bool AddSceneToBookmarksInternal(SceneAsset sceneAsset)
        {
            if (sceneAsset != null && !bookmarkedScenes.Contains(sceneAsset))
            {
                bookmarkedScenes.Add(sceneAsset);
                return true;
            }
            return false;
        }

        private void RemoveSelectedBookmark()
        {
            if (selectedMode != 0 || sceneListView.selectedIndex < 0 || sceneListView.itemsSource == null) return;
            SceneAsset sceneToRemove = (SceneAsset)sceneListView.itemsSource[sceneListView.selectedIndex];
            if (sceneToRemove != null)
            {
                bookmarkedScenes.Remove(sceneToRemove);
                SaveBookmarkedScenes();
                sceneListView.selectedIndex = -1;
                RefreshSceneList();
            }
        }

        private void RemoveAllBookmarkedScenes()
        {
            if (EditorUtility.DisplayDialog("Remove All Bookmarks?", "Are you sure you want to remove all bookmarked scenes?", "Yes", "No"))
            {
                bookmarkedScenes.Clear();
                SaveBookmarkedScenes();
                RefreshSceneList();
            }
        }

        private void SaveBookmarkedScenes()
        {
            bookmarkedScenes = bookmarkedScenes.Where(s => s != null).Distinct().ToList();
            var guids = bookmarkedScenes.Select(asset => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset))).ToList();
            EditorPrefs.SetString(BookmarkedScenesKey, string.Join(";", guids));
        }

        public static void AddCurrentSceneToBookmarksStatic()
        {
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(currentScenePath))
            {
                EditorUtility.DisplayDialog("Cannot Bookmark Scene", "The current scene is not saved to a file. Please save the scene first.", "OK");
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScenePath);
            if (sceneAsset == null)
            {
                LogDebug("Scene Switcher: Could not load SceneAsset from path: " + currentScenePath);
                return;
            }

            List<SceneAsset> bookmarkedScenes = LoadBookmarkedScenesFromPrefs();

            if (bookmarkedScenes.Contains(sceneAsset))
            {
                LogDebug($"Scene Switcher: Scene '{sceneAsset.name}' is already bookmarked.");
                return;
            }

            bookmarkedScenes.Add(sceneAsset);
            
            // Save logic
            var guids = bookmarkedScenes.Where(s => s != null).Distinct()
                                         .Select(asset => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)))
                                         .ToList();
            EditorPrefs.SetString(BookmarkedScenesKey, string.Join(";", guids));
            LogDebug($"Scene Switcher: Scene '{sceneAsset.name}' has been added to bookmarks.");
        }

        public static List<SceneAsset> LoadBookmarkedScenesFromPrefs()
        {
            List<SceneAsset> scenes = new List<SceneAsset>();
            if (EditorPrefs.HasKey(BookmarkedScenesKey))
            {
                string data = EditorPrefs.GetString(BookmarkedScenesKey);
                if (!string.IsNullOrEmpty(data))
                {
                    string[] guids = data.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                            if (sceneAsset != null) scenes.Add(sceneAsset);
                        }
                    }
                }
            }
            return scenes.Distinct().ToList();
        }

        private static void LogDebug(string message)
        {
            if (SceneSwitcherSettings.IsDebugLoggingEnabled)
            {
                Debug.Log($"Scene Switcher: {message}");
            }
        }
    }
    #endregion

    #region Credits Window
    public class CreditsWindow : EditorWindow
    {
        private string scriptFolder;

        private struct Contributor
        {
            public string Name;
            public string URL;
            public Texture Avatar;
        }

        public static void ShowWindow()
        {
            CreditsWindow wnd = GetWindow<CreditsWindow>(true);
            wnd.titleContent = new GUIContent("Credits");
            wnd.minSize = new Vector2(300, 200);
            wnd.maxSize = new Vector2(300, 200);

        }


        private Texture LoadAvatar(string folderPath, string contributorName)
        {

            if (string.IsNullOrEmpty(folderPath))
            {
                return EditorGUIUtility.IconContent("d_Avatar Icon").image;
            }

            string[] extensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };
            foreach (var ext in extensions)
            {
                string fullPath = Path.Combine(folderPath, contributorName + ext);
                Texture2D avatar = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
                if (avatar != null)
                {
                    return avatar;
                }
            }


            return EditorGUIUtility.IconContent("d_Avatar Icon").image;
        }

        public void CreateGUI()
        {
            scriptFolder = SceneSwitcherToolWindow.scriptFolder;
            var contributors = new List<Contributor>
        {
            new Contributor { Name = "BillTheDev", URL = "https://youtube.com/@billthedev?sub_confirmation=1", Avatar = LoadAvatar(scriptFolder+"/IMAGE", "BillTheDev") },
            new Contributor { Name = "NDDEVGAME", URL = "youtube.com/@nddevgame?sub_confirmation=1", Avatar = LoadAvatar(scriptFolder+"/IMAGE", "NDDEVGAME") },
            new Contributor { Name = "SoraTheDev", URL = "https://www.youtube.com/@sorathedev6739?sub_confirmation=1", Avatar = LoadAvatar(scriptFolder+"/IMAGE", "SoraTheDev") },
        };

            var root = rootVisualElement;
            root.style.backgroundColor = new StyleColor(new Color32(45, 45, 45, 255));
            root.style.paddingLeft = 5;
            root.style.paddingRight = 5;

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            root.Add(scrollView);

            foreach (var contributor in contributors)
            {
                var card = new VisualElement();
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems = Align.Center;
                card.style.paddingTop = 8;
                card.style.paddingBottom = 8;
                card.style.borderBottomWidth = 1;
                card.style.borderBottomColor = new StyleColor(new Color32(35, 35, 35, 255));
                scrollView.Add(card);

                var avatar = new Image
                {
                    image = contributor.Avatar,
                    scaleMode = ScaleMode.ScaleToFit
                };
                avatar.style.width = 40;
                avatar.style.height = 40;
                avatar.style.marginRight = 10;
                card.Add(avatar);

                var nameLabel = new Label(contributor.Name);
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameLabel.style.flexGrow = 1;
                nameLabel.style.color = new StyleColor(Color.white);
                card.Add(nameLabel);

                var urlButton = new Button(() => Application.OpenURL(contributor.URL))
                {
                    text = "Profile"
                };
                urlButton.style.width = 80;
                urlButton.style.height = 24;
                card.Add(urlButton);
            }
        }
    }
    #endregion
}