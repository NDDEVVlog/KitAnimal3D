#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace YeSPlugins
{
    public class MultiComponentCopierEditor : EditorWindow
    {
        private MultiColumnListView _copyComponentListView;

        private ScrollView _toComponentsScrollView;

        private ObjectField _copyObjectField;

        private ScrollView _toObjectScrollView;

        private List<Tuple<Component, Toggle>> _copyTuples;

        private List<Tuple<GameObject, List<Tuple<Component, Toggle>>>> _toTuples;

        private int _previousComponentCount;
        private GameObject _copyGameobject;

        [MenuItem("Tools/Multi Component Copier")]
        public static void OpenWindow()
        {
            MultiComponentCopierEditor window = GetWindow<MultiComponentCopierEditor>();
            window.titleContent = new GUIContent("Multi Component Copier");
        }

        private void OnEnable()
        {
            maxSize = new Vector2(800, 500f);
            minSize = maxSize;
            _copyTuples = new List<Tuple<Component, Toggle>>();
            _toTuples = new List<Tuple<GameObject, List<Tuple<Component, Toggle>>>>();
            
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string directory = Path.GetDirectoryName(scriptPath);

            // UXML dosyasının yolunu oluştur
            string uxmlPath = Path.Combine(directory, "Uxml/MultiComponentCopierEditor.uxml");

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            VisualElement labelFromUXML = visualTree.Instantiate();
            root.Add(labelFromUXML);

            string ussPath = Path.Combine(directory, "Uxml/Styles/styles.uss");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            root.styleSheets.Add(styleSheet);

            ObjectField objectField = new ObjectField("Copy From Gameobject")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = true,
            };

            VisualElement visualElement = rootVisualElement.Q<VisualElement>("object-field-container");
            visualElement.Add(objectField);
            _copyObjectField = objectField;

            _copyObjectField.RegisterValueChangedCallback((evt) => { OnCopyObjectValueChanged(evt); });

            ScrollView scrollView = rootVisualElement.Q<ScrollView>("to-object-list-view");
            scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            scrollView.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            scrollView.RegisterCallback<DragPerformEvent>(OnDragPerform);
            scrollView.RegisterCallback<DragLeaveEvent>(OnDragLeave);

            rootVisualElement.Q<Button>("button-add-item").clicked += OnObjectAddScrollViewItemClicked;
            rootVisualElement.Q<Button>("button-remove-item").clicked += OnObjectRemoveScrollViewItemClicked;

            _toObjectScrollView = scrollView;

            _copyComponentListView = rootVisualElement.Q<MultiColumnListView>("copy-multi-column-list");
            _copyComponentListView.fixedItemHeight = 30;

            _toComponentsScrollView = rootVisualElement.Q<ScrollView>("to-components-scroll-view");

            Button button = rootVisualElement.Q<Button>("button-run");
            button.clicked += OnRunButtonClicked;
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            SetScrollTitleState();

            if (!HasObject())
                return;

            foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject gameObject)
                {
                    ObjectAddScrollViewItem(gameObject);
                }
            }

        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            SetScrollTitleState(true);

            if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is GameObject)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }

        }

        private void OnDragLeave(DragLeaveEvent evt)
        {
            SetScrollTitleState();
        }

        private void OnObjectAddScrollViewItemClicked()
        {
            if (!HasObject())
                return;

            ObjectAddScrollViewItem(null);
        }

        private bool HasObject()
        {
            if (_copyObjectField.value == null)
            {
                Debug.Log("Please select the object that will be copied first!");
                return false;
            }

            return true;
        }

        private void OnObjectRemoveScrollViewItemClicked()
        {
            ObjectRemoveScrollViewItem();
        }

        private void ObjectAddScrollViewItem(GameObject gameObject)
        {
            if (_toObjectScrollView.Query<ObjectField>().ToList().Find((x) => x.value == null) != null)
                return;

            ObjectField objectField = new ObjectField();
            objectField.value = gameObject;
            objectField.style.marginTop = 5;
            objectField.RegisterValueChangedCallback((evt) => OnToObjectValueChanged(evt));
            objectField.objectType = typeof(GameObject);

            _toObjectScrollView.Add(objectField);

            ComponentAddScrollViewItem(gameObject);
        }

        private void ComponentAddScrollViewItem(GameObject gameObject)
        {
            AddComponentFoldoutItem(gameObject);
        }

        private void ObjectRemoveScrollViewItem(int index = -1)
        {
            if (index == -1)
            {
                index = _toObjectScrollView.childCount - 1;

                if (index < 0)
                    return;

                _toObjectScrollView.RemoveAt(_toObjectScrollView.childCount - 1);
            }
            else
            {
                _toObjectScrollView.RemoveAt(index);
            }

            ComponentRemoveScrollViewItem(index);
        }

        private void ComponentRemoveScrollViewItem(int index)
        {
            if (_toTuples.Count - 1 >= index && _toComponentsScrollView.Query<Foldout>().ToList().Count != 0)
            {
                _toTuples.RemoveAt(index);
            }

            if (_toComponentsScrollView.childCount > index)
                _toComponentsScrollView.RemoveAt(index);

        }

        private void AddComponentFoldoutItem(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            List<Tuple<Component, Toggle>> components = new List<Tuple<Component, Toggle>>();
            Tuple<GameObject, List<Tuple<Component, Toggle>>> tuple = new Tuple<GameObject, List<Tuple<Component, Toggle>>>(gameObject, components);

            Foldout foldout = new Foldout();
            foldout.userData = gameObject.GetInstanceID();
            foldout.name = "components-scroll-view-foldout";
            foldout.text = gameObject.name;
            foldout.AddToClassList(gameObject.GetInstanceID().ToString());

            foreach (var item in (_copyObjectField.value as GameObject).GetComponents<Component>())
            {
                VisualElement visualElement = new VisualElement();
                visualElement.name = "components-scroll-view-sub-item";

                Toggle toggle = new Toggle();
                Label stateLabel = new Label() { name = "state-label" };
                Label label = new Label();

                SetFoldoutElements(gameObject, item, toggle, stateLabel, label);

                visualElement.Add(toggle);
                visualElement.Add(stateLabel);
                visualElement.Add(label);

                foldout.Add(visualElement);

                components.Add(new Tuple<Component, Toggle>(item, toggle));
            }

            _toTuples.Add(tuple);

            _toComponentsScrollView.Add(foldout);
        }

        private void SetFoldoutElements(GameObject gameObject, Component item, Toggle toggle, Label stateLabel, Label label)
        {
            toggle.style.marginRight = 10;

            //Eğer sol tarafta tikli değilse burada da olmasın
            if (!_copyTuples.Find((x) => x.Item1.GetType() == item.GetType()).Item2.value)
            {
                toggle.value = false;
                toggle.SetEnabled(false);
                stateLabel.style.color = Color.red;
                stateLabel.text = "--";
                stateLabel.style.opacity = 0.6f;
                label.style.opacity = 0.6f;
            }
            else
            {
                toggle.value = true;

                if (gameObject.GetComponent(item.GetType()) != null)
                {
                    stateLabel.style.color = Color.green;
                    stateLabel.text = "Copy";
                }
                else
                {
                    stateLabel.style.color = Color.yellow;
                    stateLabel.text = "Add";
                }
            }

            stateLabel.style.width = 50;
            label.text = item.GetType().Name;
        }

        private void SetScrollTitleState(bool isSelected = false)
        {
            if (isSelected)
            {
                var label = _toObjectScrollView.parent.Q<Label>(null, "scroll-title");

                label.style.backgroundColor = new Color(1, 1, 1, 0.25f);
            }
            else
            {
                var label = _toObjectScrollView.parent.Q<Label>(null, "scroll-title");

                label.style.backgroundColor = new Color(0, 0, 0, 0.1f);
            }

        }

        private void OnToObjectValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            if (evt.previousValue != null)
            {
                int index = _toComponentsScrollView.IndexOf(_toComponentsScrollView.Query<Foldout>().ToList().Find((x) => (int)x.userData == evt.previousValue.GetInstanceID()));
                if (index != -1)
                    ComponentRemoveScrollViewItem(index);
            }

            if (evt.newValue != null)
                ComponentAddScrollViewItem(evt.newValue as GameObject);

            SetScrollTitleState();
        }

        private void OnHierarchyChanged()
        {
            if (_toTuples == null || _toComponentsScrollView == null)
            {
                Debug.LogError("Something went wrong! Please reopen MultiComponentCopier tool.");
                return;
            }

            var allNullTuples = _toTuples.FindAll((x) => x.Item1 == null);

            var allNullElements = _toObjectScrollView.Query<ObjectField>().ToList().FindAll((x) => x.value == null);

            for (int i = 0; i < allNullTuples.Count; i++)
            {
                if (allNullElements != null)
                {
                    var index = _toTuples.FindIndex((x) => x == allNullTuples[i]);

                    _toObjectScrollView.RemoveAt(index);

                    ComponentRemoveScrollViewItem(index);
                }
            }

            //New Componend Added or Removed
            if (_copyObjectField.value != null && (_copyObjectField.value as GameObject).GetComponents<Component>().Length != _previousComponentCount)
                UpdateToObjects();
        }

        private void OnCopyObjectValueChanged(ChangeEvent<UnityEngine.Object> evt = null)
        {
            GameObject gameObject = null;

            if (evt != null)
                gameObject = (GameObject)evt.newValue;
            else
                gameObject = (GameObject)_copyObjectField.value;

            Component[] allComponents = new Component[0];

            if (gameObject == null)
            {
                _copyComponentListView.itemsSource = allComponents;

                ClearAll();
                return;
            }

            allComponents = gameObject.GetComponents<Component>();

            _previousComponentCount = allComponents.Length;

            _copyComponentListView.columns["copy"].makeCell = () => new Toggle() { value = true };
            _copyComponentListView.columns["copy-name"].makeCell = () => new Label();

            _copyComponentListView.columns["copy"].bindCell = (item, index) =>
            {
                Toggle toggle = (Toggle)item;

                item.tooltip = "If component does exist it will be copied, else it will be added and copied: " + index;
                item.name = "copy-toggle";
                item.style.flexGrow = 1;
                item.style.paddingLeft = 40;

                //Eğer daha önce eklenmediyse
                if (_copyTuples.Find((x) => x.Item1.GetType() == allComponents[index].GetType()) == null)
                    _copyTuples.Add(new Tuple<Component, Toggle>(allComponents[index], item as Toggle));

                toggle.UnregisterValueChangedCallback(OnCopyToggleValueChanged);
                toggle.RegisterValueChangedCallback(OnCopyToggleValueChanged);

            };

            _copyComponentListView.columns["copy-name"].bindCell = (item, index) =>
            {
                (item as Label).text = allComponents[index].GetType().Name; // Component ad�n� g�stermek i�in g�ncelledim

                item.style.flexGrow = 1;
                item.parent.name = "copy-name";
                item.AddToClassList(allComponents[index].GetType().Name);

                item.style.unityTextAlign = TextAnchor.MiddleLeft;
                item.RegisterCallback<ClickEvent>(OnItemSelected);
            };

            _copyComponentListView.itemsSource = allComponents;
            _copyGameobject = gameObject;

            UpdateComponentFoldoutItems();
        }

        private void OnCopyToggleValueChanged(ChangeEvent<bool> evt)
        {
            SetGeneralToggleValues();
        }

        private void SetGeneralToggleValues()
        {
            foreach (VisualElement element in _toObjectScrollView.Query<ObjectField>().ToList())
            {
                GameObject gameObject = (element as ObjectField).value as GameObject;

                if (gameObject == null)
                {
                    continue;
                }

                for (int i = 0; i < _toTuples.Count; i++)
                {
                    if (gameObject == _toTuples[i].Item1)
                    {
                        for (int j = 0; j < _toTuples[i].Item2.Count; j++)
                        {
                            Tuple<Component, Toggle> tuple = _copyTuples.Find((X) => X.Item1.GetType() == _toTuples[i].Item2[j].Item1.GetType());

                            VisualElement parent = _toTuples[i].Item2[j].Item2.parent;

                            Label stateLabel = parent.ElementAt(1) as Label;
                            Label label = parent.ElementAt(2) as Label;

                            SetComponentElementState(tuple, _toTuples[i].Item1, _toTuples[i].Item2[j].Item2, stateLabel, label);
                        }
                    }
                }
            }
        }

        private void SetComponentElementState(Tuple<Component, Toggle> copyTuple, GameObject componentObject, Toggle toggle, Label stateLabel, Label componentLabel)
        {
            if (!copyTuple.Item2.value)
            {
                toggle.value = false;
                toggle.SetEnabled(false);

                stateLabel.style.color = Color.red;
                stateLabel.text = "--";
                stateLabel.style.opacity = 0.6f;
                componentLabel.style.opacity = 0.6f;

            }
            else if (!toggle.enabledSelf)
            {
                toggle.SetEnabled(true);
                toggle.value = true;

                if (componentObject.GetComponent(copyTuple.Item1.GetType()))
                {
                    stateLabel.style.color = Color.green;
                    stateLabel.text = "Copy";
                }
                else
                {
                    stateLabel.style.color = Color.yellow;
                    stateLabel.text = "Add";
                }
                stateLabel.style.opacity = 1;
                componentLabel.style.opacity = 1;
            }
        }

        private void OnItemSelected(ClickEvent evt)
        {
            var selectedComponent = _copyComponentListView.selectedItem as Component;
            if (selectedComponent != null)
            {
                int index = Array.IndexOf(_copyComponentListView.itemsSource as Component[], selectedComponent);
                if (index >= 0)
                {
                    int toggleIndex = 0;
                    foreach (var visualElement in _copyComponentListView.Query<Toggle>().ToList())
                    {
                        if (toggleIndex == index)
                        {
                            visualElement.value = !visualElement.value;
                            break;
                        }
                        toggleIndex++;
                    }
                }
            }
        }

        private void UpdateToObjects()
        {
            _toTuples.Clear();

            _copyComponentListView.Clear();

            _copyComponentListView.itemsSource.Clear();

            _toComponentsScrollView.Clear();

            _copyTuples.Clear();

            OnCopyObjectValueChanged();

            UpdateComponentFoldoutItems();
        }

        private void UpdateComponentFoldoutItems()
        {
            int length = _toComponentsScrollView.childCount;

            for (int i = length - 1; i >= 0; i--)
            {
                ComponentRemoveScrollViewItem(i);
            }

            foreach (var item in _toObjectScrollView.Query<ObjectField>().ToList())
            {
                AddComponentFoldoutItem(item.value as GameObject);
            }

        }

        private void ClearAll()
        {
            _toComponentsScrollView.Clear();
            _toTuples.Clear();
            _toObjectScrollView.Clear();
            _copyTuples.Clear();
        }

        private void OnRunButtonClicked()
        {
            if (_toTuples.Count == 0)
            {
                Debug.Log("Add at least one item to Copy-To Gameobjects");
                return;
            }

            for (int i = 0; i < _toTuples.Count; i++)
            {
                GameObject toGameobject = _toTuples[i].Item1;

                if (toGameobject == null)
                {
                    Debug.Log("Null Object! Please close and reopen the window!");
                    continue;
                }

                List<Component> copiedToComponents = new List<Component>();

                for (int j = 0; j < _toTuples[i].Item2.Count; j++)
                {
                    if (_toTuples[i].Item2[j].Item2.value)
                    {
                        Component copyComponent = _toTuples[i].Item2[j].Item1;

                        int index = copiedToComponents.Sum((x)=>x.GetType() == copyComponent.GetType() ? 1 : 0);
                        bool hasComponent = toGameobject.GetComponents(copyComponent.GetType()).Length > index;

                        Component toComponent = (hasComponent) ? toGameobject.GetComponents(copyComponent.GetType())[index] : null;

                        if (hasComponent)
                        {
                            ComponentUtility.CopyComponent(copyComponent);
                            ComponentUtility.PasteComponentValues(toComponent);
                            copiedToComponents.Add(toComponent);
                        }
                        else
                        {
                            Component component1 = Undo.AddComponent(toGameobject, copyComponent.GetType());
                            ComponentUtility.CopyComponent(copyComponent);
                            ComponentUtility.PasteComponentValues(component1);
                            copiedToComponents.Add(component1);
                        }

                    }

                }
            }

            UpdateComponentFoldoutItems();
        }



    }
}
#endif