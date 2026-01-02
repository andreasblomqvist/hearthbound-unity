using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Custom editor for TerrainGenerator with interactive river path point selection
    /// </summary>
    [CustomEditor(typeof(TerrainGenerator))]
    [CanEditMultipleObjects]
    public class TerrainGeneratorEditor : UnityEditor.Editor
    {
        private enum SelectionMode
        {
            None,
            SelectingRiverSource,
            SelectingLakeCenter,
            AddingCustomPathPoints
        }

        private SelectionMode currentMode = SelectionMode.None;
        private TerrainGenerator terrainGenerator;

        private void OnEnable()
        {
            terrainGenerator = (TerrainGenerator)target;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Terrain Generation", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate Terrain Now", GUILayout.Height(40)))
            {
                // Get seed from WorldSeedManager if available
                WorldSeedManager seedManager = FindObjectOfType<WorldSeedManager>();
                int seed = seedManager != null ? seedManager.CurrentSeed : 12345;

                terrainGenerator.GenerateTerrain(seed);
                Debug.Log($"✅ Terrain generated with seed: {seed}");
            }

            if (GUILayout.Button("Clear Terrain", GUILayout.Height(40)))
            {
                terrainGenerator.ClearTerrain();
                Debug.Log("✅ Terrain cleared");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("River Path Selection", EditorStyles.boldLabel);

            // Show current values
            SerializedProperty riverPathModeProp = serializedObject.FindProperty("riverPathMode");
            SerializedProperty manualRiverSourceProp = serializedObject.FindProperty("manualRiverSource");
            SerializedProperty manualLakeCenterProp = serializedObject.FindProperty("manualLakeCenter");

            if (riverPathModeProp.enumValueIndex == (int)RiverPathMode.Manual)
            {
                EditorGUILayout.HelpBox("Click buttons below to select points on terrain in Scene view", MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Select River Source", GUILayout.Height(30)))
                {
                    currentMode = SelectionMode.SelectingRiverSource;
                    SceneView.FocusWindowIfItsOpen<SceneView>();
                    Debug.Log("Click on terrain in Scene view to set River Source");
                }

                if (GUILayout.Button("Select Lake Center", GUILayout.Height(30)))
                {
                    currentMode = SelectionMode.SelectingLakeCenter;
                    SceneView.FocusWindowIfItsOpen<SceneView>();
                    Debug.Log("Click on terrain in Scene view to set Lake Center");
                }

                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                
                SerializedProperty customPathProp = serializedObject.FindProperty("customRiverPath");
                
                if (GUILayout.Button("Add Custom Path Point", GUILayout.Height(30)))
                {
                    currentMode = SelectionMode.AddingCustomPathPoints;
                    SceneView.FocusWindowIfItsOpen<SceneView>();
                    Debug.Log("Click on terrain in Scene view to add points to custom river path");
                }

                if (customPathProp != null && customPathProp.arraySize > 0)
                {
                    if (GUILayout.Button("Clear Custom Path", GUILayout.Height(30)))
                    {
                        customPathProp.ClearArray();
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(terrainGenerator);
                        Debug.Log("Cleared custom river path");
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Clear Selection Mode", GUILayout.Height(25)))
                {
                    currentMode = SelectionMode.None;
                }

                // Display current values
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Current Values:", EditorStyles.boldLabel);
                
                Vector2 source = manualRiverSourceProp.vector2Value;
                Vector2 lake = manualLakeCenterProp.vector2Value;
                
                EditorGUILayout.LabelField($"River Source: ({source.x:F1}, {source.y:F1})");
                EditorGUILayout.LabelField($"Lake Center: ({lake.x:F1}, {lake.y:F1})");
                
                // Display custom path points (reuse the same customPathProp variable)
                if (customPathProp != null)
                {
                    EditorGUILayout.LabelField($"Custom Path Points: {customPathProp.arraySize}");
                    if (customPathProp.arraySize > 0)
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < customPathProp.arraySize && i < 10; i++) // Show first 10 points
                        {
                            SerializedProperty pointProp = customPathProp.GetArrayElementAtIndex(i);
                            Vector2 point = pointProp.vector2Value;
                            EditorGUILayout.LabelField($"  Point {i}: ({point.x:F1}, {point.y:F1})");
                        }
                        if (customPathProp.arraySize > 10)
                        {
                            EditorGUILayout.LabelField($"  ... and {customPathProp.arraySize - 10} more");
                        }
                        EditorGUI.indentLevel--;
                        
                        EditorGUILayout.HelpBox($"Custom path will be used with {customPathProp.arraySize} points", MessageType.Info);
                    }
                    else if (source != Vector2.zero && lake != Vector2.zero)
                    {
                        EditorGUILayout.HelpBox($"River will flow from ({source.x:F1}, {source.y:F1}) to ({lake.x:F1}, {lake.y:F1})", MessageType.Info);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Set 'River Path Mode' to 'Manual' to enable point selection", MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (terrainGenerator == null || currentMode == SelectionMode.None)
                return;

            // Handle mouse events
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                // Raycast to terrain
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Terrain hitTerrain = hit.collider.GetComponent<Terrain>();
                    
                    if (hitTerrain != null && hitTerrain.GetComponent<TerrainGenerator>() == terrainGenerator)
                    {
                        // Get world position on terrain
                        Vector3 worldPos = hit.point;
                        
                        // Convert to terrain-local coordinates (relative to terrain position)
                        Vector3 terrainWorldPos = hitTerrain.transform.position;
                        Vector3 terrainLocalPos = worldPos - terrainWorldPos;
                        
                        // Create Vector2 with X and Z (Y is height, not needed for path)
                        Vector2 pathPoint = new Vector2(terrainLocalPos.x, terrainLocalPos.z);

                        // Update the appropriate property
                        serializedObject.Update();

                        if (currentMode == SelectionMode.SelectingRiverSource)
                        {
                            SerializedProperty property = serializedObject.FindProperty("manualRiverSource");
                            property.vector2Value = pathPoint;
                            Debug.Log($"✅ River Source set to: ({pathPoint.x:F1}, {pathPoint.y:F1})");
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(terrainGenerator);
                            currentMode = SelectionMode.None; // Reset after single point selection
                        }
                        else if (currentMode == SelectionMode.SelectingLakeCenter)
                        {
                            SerializedProperty property = serializedObject.FindProperty("manualLakeCenter");
                            property.vector2Value = pathPoint;
                            Debug.Log($"✅ Lake Center set to: ({pathPoint.x:F1}, {pathPoint.y:F1})");
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(terrainGenerator);
                            currentMode = SelectionMode.None; // Reset after single point selection
                        }
                        else if (currentMode == SelectionMode.AddingCustomPathPoints)
                        {
                            SerializedProperty customPathProp = serializedObject.FindProperty("customRiverPath");
                            int arraySize = customPathProp.arraySize;
                            customPathProp.arraySize = arraySize + 1;
                            SerializedProperty newPointProp = customPathProp.GetArrayElementAtIndex(arraySize);
                            newPointProp.vector2Value = pathPoint;
                            Debug.Log($"✅ Added custom path point {arraySize + 1}: ({pathPoint.x:F1}, {pathPoint.y:F1})");
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(terrainGenerator);
                            // Don't reset mode - allow multiple clicks to add more points
                        }
                        
                        e.Use();
                        Repaint();
                    }
                }
            }

            // Draw visual feedback for current mode
            if (currentMode != SelectionMode.None)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(10, 10, 350, 80));
                GUIStyle style = new GUIStyle(GUI.skin.box);
                style.normal.textColor = Color.white;
                style.fontSize = 12;
                style.fontStyle = FontStyle.Bold;
                
                string message = "";
                if (currentMode == SelectionMode.SelectingRiverSource)
                    message = "Click on terrain to set River Source";
                else if (currentMode == SelectionMode.SelectingLakeCenter)
                    message = "Click on terrain to set Lake Center";
                else if (currentMode == SelectionMode.AddingCustomPathPoints)
                {
                    SerializedObject so = new SerializedObject(terrainGenerator);
                    SerializedProperty customPathProp = so.FindProperty("customRiverPath");
                    int pointCount = customPathProp != null ? customPathProp.arraySize : 0;
                    message = $"Click on terrain to add path point ({pointCount} points so far)\nRight-click or 'Clear Selection Mode' to finish";
                }
                
                GUILayout.Label(message, style);
                GUILayout.EndArea();
                Handles.EndGUI();

                SceneView.RepaintAll();
            }
            
            // Handle right-click to exit custom path point mode
            if (currentMode == SelectionMode.AddingCustomPathPoints && e.type == EventType.MouseDown && e.button == 1)
            {
                currentMode = SelectionMode.None;
                e.Use();
                Repaint();
            }

            // Draw markers for selected points
            DrawPathPointMarkers();
        }

        private void DrawPathPointMarkers()
        {
            SerializedObject so = new SerializedObject(terrainGenerator);
            SerializedProperty sourceProp = so.FindProperty("manualRiverSource");
            SerializedProperty lakeProp = so.FindProperty("manualLakeCenter");

            Vector2 source = sourceProp.vector2Value;
            Vector2 lake = lakeProp.vector2Value;

            Terrain terrain = terrainGenerator.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
                return;

            Vector3 terrainWorldPos = terrain.transform.position;

            // Draw river source marker
            if (source != Vector2.zero)
            {
                Vector3 localPos = new Vector3(source.x, 0, source.y);
                float height = terrain.SampleHeight(terrainWorldPos + localPos);
                Vector3 worldPos = terrainWorldPos + localPos + Vector3.up * height;
                
                Handles.color = Color.red;
                Handles.DrawWireDisc(worldPos, Vector3.up, 10f);
                Handles.DrawLine(worldPos, worldPos + Vector3.up * 20f);
                
                Handles.Label(worldPos + Vector3.up * 25f, "River Source", EditorStyles.boldLabel);
            }

            // Draw lake center marker
            if (lake != Vector2.zero)
            {
                Vector3 localPos = new Vector3(lake.x, 0, lake.y);
                float height = terrain.SampleHeight(terrainWorldPos + localPos);
                Vector3 worldPos = terrainWorldPos + localPos + Vector3.up * height;
                
                Handles.color = Color.blue;
                Handles.DrawWireDisc(worldPos, Vector3.up, 10f);
                Handles.DrawLine(worldPos, worldPos + Vector3.up * 20f);
                
                Handles.Label(worldPos + Vector3.up * 25f, "Lake Center", EditorStyles.boldLabel);
            }

            // Draw line between source and lake if both are set (only if no custom path)
            SerializedProperty customPathProp = so.FindProperty("customRiverPath");
            if (customPathProp == null || customPathProp.arraySize == 0)
            {
                if (source != Vector2.zero && lake != Vector2.zero)
                {
                    Vector3 sourceLocal = new Vector3(source.x, 0, source.y);
                    Vector3 lakeLocal = new Vector3(lake.x, 0, lake.y);
                    float sourceHeight = terrain.SampleHeight(terrainWorldPos + sourceLocal);
                    float lakeHeight = terrain.SampleHeight(terrainWorldPos + lakeLocal);
                    
                    Vector3 sourceWorld = terrainWorldPos + sourceLocal + Vector3.up * (sourceHeight + 5f);
                    Vector3 lakeWorld = terrainWorldPos + lakeLocal + Vector3.up * (lakeHeight + 5f);
                    
                    Handles.color = Color.cyan;
                    Handles.DrawLine(sourceWorld, lakeWorld);
                }
            }
            
            // Draw custom path points
            if (customPathProp != null && customPathProp.arraySize > 0)
            {
                Handles.color = Color.green;
                for (int i = 0; i < customPathProp.arraySize; i++)
                {
                    SerializedProperty pointProp = customPathProp.GetArrayElementAtIndex(i);
                    Vector2 point = pointProp.vector2Value;
                    
                    Vector3 localPos = new Vector3(point.x, 0, point.y);
                    float height = terrain.SampleHeight(terrainWorldPos + localPos);
                    Vector3 worldPos = terrainWorldPos + localPos + Vector3.up * height;
                    
                    // Draw marker
                    Handles.DrawWireDisc(worldPos, Vector3.up, 8f);
                    Handles.DrawLine(worldPos, worldPos + Vector3.up * 15f);
                    Handles.Label(worldPos + Vector3.up * 20f, $"P{i}", EditorStyles.boldLabel);
                    
                    // Draw line to next point
                    if (i < customPathProp.arraySize - 1)
                    {
                        SerializedProperty nextPointProp = customPathProp.GetArrayElementAtIndex(i + 1);
                        Vector2 nextPoint = nextPointProp.vector2Value;
                        Vector3 nextLocalPos = new Vector3(nextPoint.x, 0, nextPoint.y);
                        float nextHeight = terrain.SampleHeight(terrainWorldPos + nextLocalPos);
                        Vector3 nextWorldPos = terrainWorldPos + nextLocalPos + Vector3.up * (nextHeight + 3f);
                        Handles.DrawLine(worldPos + Vector3.up * 3f, nextWorldPos);
                    }
                }
            }
        }
    }
}

