using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpellTech.SceneSwitcher
{
    #region Settings Management
    public static class SceneSwitcherSettings
    {
        public enum LayoutMode { Responsive, FixedVertical, FixedHorizontal }

        private const string FixedLayoutKey = "SceneSwitcher.Layout.FixedLayoutMode";
        private const string DebugLogKey = "SceneSwitcher.DebugLogEnabled";
        private const string ToolbarKey = "SceneSwitcher.ToolbarEnabled";

        public static event Action OnSettingsChanged;


        public static LayoutMode FixedLayout
        {
            get => (LayoutMode)EditorPrefs.GetInt(FixedLayoutKey, (int)LayoutMode.FixedVertical);
            set
            {
                EditorPrefs.SetInt(FixedLayoutKey, (int)value);
                OnSettingsChanged?.Invoke();
            }
        }

        public static bool IsDebugLoggingEnabled
        {
            get => EditorPrefs.GetBool(DebugLogKey, false);
            set
            {
                EditorPrefs.SetBool(DebugLogKey, value);
                OnSettingsChanged?.Invoke();
            }
        }

        public static bool IsToolbarShortcutEnabled
        {
            get => EditorPrefs.GetBool(ToolbarKey, true);
            set
            {
                EditorPrefs.SetBool(ToolbarKey, value);
                OnSettingsChanged?.Invoke();
            }
        }
    }
    #endregion

    #region Settings Window
    public class SettingsWindow : EditorWindow
    {

        private const float RecompileDelay = 10.0f; // Countdown duration in seconds
        private bool isRecompileScheduled = false;
        private double recompileTriggerTime;
        private int lastDisplayedSecond = -1;

        private VisualElement recompileContainer;
        private Label countdownLabel;
        private Button cancelButton;

        public static void ShowWindow()
        {
            SettingsWindow wnd = GetWindow<SettingsWindow>(true, "Scene Switcher Settings", true);
            wnd.minSize = new Vector2(350, 240); // Increased height to fit the new UI
            wnd.maxSize = new Vector2(350, 240);
        }


        private void OnEnable()
        {

            EditorApplication.update -= UpdateCountdown;
        }

        private void OnDisable()
        {
            CancelRecompile();
        }


        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;

            var titleLabel = new Label("Scene Switcher Settings")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
            };
            root.Add(titleLabel);


            var fixedLayoutDropdown = new EnumField("Fixed Layout", SceneSwitcherSettings.FixedLayout)
            {
                tooltip = "Force a specific layout, ignoring window width."
            };


            fixedLayoutDropdown.RegisterValueChangedCallback(evt =>
            {
                SceneSwitcherSettings.FixedLayout = (SceneSwitcherSettings.LayoutMode)evt.newValue;
                SceneSwitcherToolWindow.RepaintWindow();
            });


            root.Add(fixedLayoutDropdown);

       
            var debugToggle = new Toggle("Enable Debug Logging")
            {
                value = SceneSwitcherSettings.IsDebugLoggingEnabled,
                tooltip = "Print detailed logs to the console for troubleshooting."
            };
            debugToggle.RegisterValueChangedCallback(evt => SceneSwitcherSettings.IsDebugLoggingEnabled = evt.newValue);
            root.Add(debugToggle);

            var toolbarToggle = new Toggle("Enable Toolbar Shortcut")
            {
                value = SceneSwitcherSettings.IsToolbarShortcutEnabled,
                tooltip = "Show the bookmarked scenes dropdown in the main editor toolbar."
            };


            toolbarToggle.RegisterValueChangedCallback(evt =>
            {

                SceneSwitcherSettings.IsToolbarShortcutEnabled = evt.newValue;
                ScheduleRecompile();
            });
            root.Add(toolbarToggle);

            var infoLabel = new Label("Toolbar changes require an editor restart or script recompile to take effect.")
            {
                style = { fontSize = 9, unityFontStyleAndWeight = FontStyle.Italic, whiteSpace = WhiteSpace.Normal, marginTop = 4 }
            };
            root.Add(infoLabel);

            recompileContainer = new VisualElement()
            {
                style =
            {
                marginTop = 12,
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                display = DisplayStyle.None
            }
            };

            countdownLabel = new Label();
            cancelButton = new Button(CancelRecompile) { text = "Cancel" };

            recompileContainer.Add(countdownLabel);
            recompileContainer.Add(cancelButton);
            root.Add(recompileContainer);
        }

        private void ScheduleRecompile()
        {
            if (isRecompileScheduled) return; // Don't start a new countdown if one is already running

            LogDebug($"Scheduling script recompile in {RecompileDelay} seconds.");
            isRecompileScheduled = true;
            recompileTriggerTime = EditorApplication.timeSinceStartup + RecompileDelay;

           
            recompileContainer.style.display = DisplayStyle.Flex;


            EditorApplication.update += UpdateCountdown;
            lastDisplayedSecond = -1;
            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (!isRecompileScheduled) return;

            double timeRemaining = recompileTriggerTime - EditorApplication.timeSinceStartup;
            int secondsRemaining = Mathf.CeilToInt((float)timeRemaining);

            // To avoid updating the UI text every single frame, we only update it when the second changes.
            if (secondsRemaining != lastDisplayedSecond)
            {
                countdownLabel.text = $"Recompiling in {secondsRemaining}...";
                lastDisplayedSecond = secondsRemaining;
            }

            if (timeRemaining <= 0)
            {
                ForceRecompile();
            }
        }

        private void CancelRecompile()
        {
            if (!isRecompileScheduled) return;

            LogDebug("Script recompile cancelled by user.");
            isRecompileScheduled = false;

            // Hide the countdown UI
            recompileContainer.style.display = DisplayStyle.None;

            // Unsubscribe from the update loop. This is critical!
            EditorApplication.update -= UpdateCountdown;
        }

        private void ForceRecompile()
        {
            LogDebug("Forcing script recompile now.");

            CancelRecompile();


            CompilationPipeline.RequestScriptCompilation();


            this.Close();
        }

        void LogDebug(string message)
        {
            if (SceneSwitcherSettings.IsDebugLoggingEnabled)
            {
                Debug.Log($"Scene Switcher: {message}");
            }
        }
    }
}
#endregion