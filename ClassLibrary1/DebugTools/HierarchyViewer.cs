﻿using UnityEngine;
using UnityEngine.SceneManagement;

namespace ONI_MP.DebugTools
{
    public class HierarchyViewer : MonoBehaviour
    {
        private bool showWindow = false;
        private Vector2 scrollPos = Vector2.zero;

        public void Toggle()
        {
            showWindow = !showWindow;
        }

        void OnGUI()
        {
            if (!showWindow) return;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                padding = new RectOffset(8, 8, 8, 8)
            };

            GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20), "Hierarchy", boxStyle);
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, true);

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                DrawGameObjectRecursive(root, 0);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawGameObjectRecursive(GameObject obj, int indent)
        {
            Component[] components = obj.GetComponents<Component>();
            string typeInfo = "GameObject";

            foreach (Component comp in components)
            {
                if (!(comp is Transform))
                {
                    typeInfo = comp.GetType().Name;
                    break;
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(indent * 16);

            // Create a label style for measuring
            GUIContent labelContent = new GUIContent(obj.name);
            GUIStyle labelStyle = GUI.skin.label;
            Vector2 labelSize = labelStyle.CalcSize(labelContent);

            Rect labelRect = GUILayoutUtility.GetRect(labelContent, labelStyle);
            GUI.Label(labelRect, labelContent); // Draw the GameObject name

            // Check mouse hover
            if (labelRect.Contains(Event.current.mousePosition))
            {
                GUIStyle typeStyle = new GUIStyle(GUI.skin.label);
                typeStyle.normal.textColor = Color.cyan;

                GUIContent typeContent = new GUIContent($" [{typeInfo}]");
                Vector2 typeSize = typeStyle.CalcSize(typeContent);

                Rect typeRect = new Rect(labelRect.xMax, labelRect.y, typeSize.x, typeSize.y);
                GUI.Label(typeRect, typeContent, typeStyle);
            }

            GUILayout.EndHorizontal();

            foreach (Transform child in obj.transform)
            {
                DrawGameObjectRecursive(child.gameObject, indent + 1);
            }
        }


    }
}
