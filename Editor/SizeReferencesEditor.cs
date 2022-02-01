using System;
using UnityEditor;
using UnityEngine;
using SakuraTools.Core;
using SakuraTools.Core.Editor;
using UnityEngine.SceneManagement;

namespace SakuraTools.SizeReferences.Editor
{
    [InitializeOnLoad, ExecuteInEditMode]
    public static class SizeReferencesEditor
    {
        //------------------------------------------------------------------------------------//
        /*----------------------------------- FIELDS -----------------------------------------*/
        //------------------------------------------------------------------------------------//

        private const string SHOW_HIDE_CONTROLS_KEY = "ScaleReferencesEditor_ShowHide";
        private const string WINDOW_POSITION_KEY = "ScaleReferencesEditor_WPos";
        private const string SHOW_ROTATION_KEY = "ScaleReferences_Show_Rotation";
        private const float CONTROLS_OVERLAP = 2;
        private const float RECORD_ICON_SCALE = 0.55f;

        private static Vector3 _initialPosition;
        private static Vector2 _dragOffset;
        private static bool _dragging;
        private static bool _updatingPosition;
        private static int _controlId = GUIUtility.GetControlID(FocusType.Passive);

        //------------------------------------------------------------------------------------//
        /*--------------------------------- PROPERTIES ---------------------------------------*/
        //------------------------------------------------------------------------------------//

        private static Vector2 WindowPosition
        {
            get => PlayerPrefsHelper.GetVector2(WINDOW_POSITION_KEY, new Vector2(10, 10));
            set => PlayerPrefsHelper.SetVector2(WINDOW_POSITION_KEY, value);
        }

        private static bool ShowHideControls
        {
            get => PlayerPrefsHelper.GetBool(SHOW_HIDE_CONTROLS_KEY, true);
            set => PlayerPrefsHelper.SetBool(SHOW_HIDE_CONTROLS_KEY, value);
        }

        private static bool ShowRotateGizmo
        {
            get => PlayerPrefsHelper.GetBool(SHOW_ROTATION_KEY, false);
            set => PlayerPrefsHelper.SetBool(SHOW_ROTATION_KEY, value);
        }

        //------------------------------------------------------------------------------------//
        /*---------------------------------- METHODS -----------------------------------------*/
        //------------------------------------------------------------------------------------//

        static SizeReferencesEditor()
        {
            SceneView.duringSceneGui -= OnScene;
            SceneView.duringSceneGui += OnScene;
        }


        private static void OnScene(SceneView sceneView)
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlaying) { return; }

            if (ShowRotateGizmo && ShowHideControls)
            {
                EditorGUI.BeginChangeCheck();
                Quaternion rot = Handles.RotationHandle(SizeReferences.Instance.ReferenceRotation, SizeReferences.Instance.CachedTransform.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(SizeReferences.Instance, "Rotated RotateAt Point");
                    SizeReferences.Instance.ReferenceRotation = rot;
                }
            }
            
            // Ensure the window is always visible to drag.
            Rect paddedVisibleRect = sceneView.camera.pixelRect;
            paddedVisibleRect.width -= 10;
            paddedVisibleRect.height -= 10;
            paddedVisibleRect.x += 5;
            paddedVisibleRect.y += 5;
            if (!paddedVisibleRect.Contains(WindowPosition))
            {
                WindowPosition = new Vector2(10, 10);
            }

            Handles.BeginGUI();
            Vector2 windowPos = WindowPosition;
            Event e = Event.current;

            Rect rect = new Rect(windowPos.x, windowPos.y, 10, 23);

            // Begin Drag
            if (GUI.RepeatButton(rect, String.Empty))
            {
                _dragOffset = new Vector2(rect.x - e.mousePosition.x, rect.y - e.mousePosition.y);
                _dragging = true;
                GUIUtility.hotControl = _controlId;
                if (e.type != EventType.Repaint && e.type != EventType.Layout) { e.Use(); }
            }
            else if (_dragging)
            {
                switch (e.GetTypeForControl(_controlId))
                {
                    // Dragging
                    case EventType.MouseDrag:
                        WindowPosition = e.mousePosition + _dragOffset;
                        sceneView.Repaint();
                        break;

                    // End Drag 
                    case EventType.MouseUp:
                        _dragging = false;
                        break;
                }
            }
            else if (_updatingPosition)
            {
                GUIUtility.hotControl = _controlId;

                switch (e.GetTypeForControl(_controlId))
                {
                    // Mouse Is Moving
                    case EventType.MouseDrag:
                    case EventType.MouseMove:
                        // Upside-down and offset a little because of menus
                        // But do note that Screen.height does not account for DPI scaled displays such as retina displays on OSX, so it might need correcting.
                        Ray ray = sceneView.camera.ScreenPointToRay(new Vector3(e.mousePosition.x, Screen.height - e.mousePosition.y - 36, 0));
                        if (Physics.Raycast(ray, out RaycastHit hit))
                        {
                            SizeReferences.Instance.ReferencePosition = hit.point;
                        }
                        break;

                    // End Position Update
                    case EventType.MouseUp:
                        // You can right click to cancel
                        if (e.button == 1) { SizeReferences.Instance.ReferencePosition = _initialPosition; }
                        _updatingPosition = false;
                        break;
                }

            }

            rect.x += rect.width - CONTROLS_OVERLAP;
            rect.width = 26;

            if (GUI.Button(rect, UnityIcons.Distance))
            {
                ShowHideControls = !ShowHideControls;
            }

            rect.x += rect.width - CONTROLS_OVERLAP;

            // Material Visible On/Off
            if (GUI.Button(rect, SizeReferences.Instance.IsVisible ? UnityIcons.VisibleOn : UnityIcons.VisibleOff))
            {
                SizeReferences.Instance.IsVisible = !SizeReferences.Instance.IsVisible;
            }

            if (ShowHideControls) { DrawControls(rect); }

            Handles.EndGUI();
        }

        private static void DrawControls(Rect rect)
        {
            // Previous Button
            rect.x += rect.width - CONTROLS_OVERLAP;
            rect.width = 19;
            if (GUI.Button(rect, UnityIcons.Back))
            {
                int i = SizeReferences.Instance.SelectedIndex;
                i = i == 0 ? SizeReferences.Instance.TotalReferences - 1 : i - 1;
                SizeReferences.Instance.SelectedIndex = i;
            }

            // Current Item Label
            rect.x += rect.width - CONTROLS_OVERLAP;
            rect.width = 120;
            if (GUI.Button(rect, SizeReferences.Instance.CurrentReferenceName))
            {
                EditorApplication.delayCall += () =>
                {
                    // If you have plugins that edit the scene view, this operation can cause some errors on them if you execute it right away.
                    // This is why it's wrapped like this.

                    GameObject last = Selection.activeGameObject;
                    Selection.activeGameObject = SizeReferences.Instance.CurrentMeshObject;
                    SceneView.FrameLastActiveSceneView();
                    Selection.activeGameObject = last;
                };
            }

            // Next Button
            rect.x += rect.width - CONTROLS_OVERLAP;
            rect.width = 19;
            if (GUI.Button(rect, UnityIcons.Forward))
            {
                int i = SizeReferences.Instance.SelectedIndex;
                i = i == SizeReferences.Instance.TotalReferences - 1 ? 0 : i + 1;
                SizeReferences.Instance.SelectedIndex = i;
            }

            // Record Position
            rect.x += rect.width - CONTROLS_OVERLAP;
            rect.width = 26;
            Rect recordRect = new Rect(rect.x + rect.width * (1f - RECORD_ICON_SCALE), rect.y + rect.height * (1f - RECORD_ICON_SCALE),
                rect.width * RECORD_ICON_SCALE, rect.height * RECORD_ICON_SCALE);
            if (GUI.Button(rect, UnityIcons.Transform))
            {
                if (Event.current.button == 0)
                {
                    _updatingPosition = !_updatingPosition;
                    ShowRotateGizmo = false;
                    _initialPosition = SizeReferences.Instance.ReferencePosition;
                    GUIUtility.hotControl = _controlId;
                    if (Event.current.type != EventType.Repaint) { Event.current.Use(); }
                }
                else if (Event.current.button == 1)
                {
                    _updatingPosition = false;
                    SizeReferences.Instance.ReferencePosition = Vector3.zero;
                }
            }
            if (_updatingPosition) { GUI.Label(recordRect, UnityIcons.Record); }

            // Toggle Rotation Gizmos
            rect.x += rect.width - CONTROLS_OVERLAP;
            recordRect.x += rect.width - CONTROLS_OVERLAP;
            if (GUI.Button(rect, UnityIcons.Rotate))
            {
                if (Event.current.button == 0)
                {
                    ShowRotateGizmo = !ShowRotateGizmo;
                }
                else if (Event.current.button == 1)
                {
                    SizeReferences.Instance.ReferenceRotation = Quaternion.identity;
                    ShowRotateGizmo = false;
                }
            }
            if (ShowRotateGizmo) { GUI.Label(recordRect, UnityIcons.Record); }

            // Material Color Picker
            rect.x += rect.width - CONTROLS_OVERLAP;
            rect.width = 50;
            GUI.Label(rect, string.Empty, new GUIStyle("button")); // Color Picker Background
            float space = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0;
            SizeReferences.Instance.MeshColor = EditorGUI.ColorField(rect, new GUIContent(), SizeReferences.Instance.MeshColor, false, false,
#if ST_URP || ST_HDRP
                true
#else
                false
#endif
                );
            EditorGUIUtility.labelWidth = space;
        }
    }
}