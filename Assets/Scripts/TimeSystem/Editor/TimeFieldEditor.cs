#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(TimeField))]
public class TimeFieldEditor : Editor
{
    private SerializedProperty timeScaleProp;
    private SerializedProperty affectedLayersProp;
    private SerializedProperty fieldMaterialProp;
    
    private void OnEnable()
    {
        timeScaleProp = serializedObject.FindProperty("timeScale");
        affectedLayersProp = serializedObject.FindProperty("affectedLayers");
        fieldMaterialProp = serializedObject.FindProperty("fieldMaterial");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        TimeField field = (TimeField)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Time Field Settings", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(timeScaleProp);
        
        Rect rect = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.ProgressBar(rect, timeScaleProp.floatValue, 
            GetTimeScaleLabel(timeScaleProp.floatValue));
        
        // Botões rápidos
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Freeze (0)"))
            timeScaleProp.floatValue = 0f;
        if (GUILayout.Button("Half (0.5)"))
            timeScaleProp.floatValue = 0.5f;
        if (GUILayout.Button("Normal (1)"))
            timeScaleProp.floatValue = 1f;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Filter
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Filtering", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(affectedLayersProp);
        
        // Aviso se nenhuma layer selecionada
        if (affectedLayersProp.intValue == 0)
        {
            EditorGUILayout.HelpBox("Nenhuma layer selecionada! O campo não afetará nada.", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Visual (Opcional)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fieldMaterialProp);
        EditorGUILayout.EndVertical();
        
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Current TimeScale: {field.TimeScale:F2}");
            EditorGUILayout.EndVertical();
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
        
        CheckColliderSetup(field);
    }
    
    private string GetTimeScaleLabel(float scale)
    {
        if (scale <= 0f) return "FROZEN (0%)";
        if (scale < 0.3f) return $"Very Slow ({scale * 100:F0}%)";
        if (scale < 0.7f) return $"Slow ({scale * 100:F0}%)";
        if (scale < 1f) return $"Slightly Slow ({scale * 100:F0}%)";
        return "Normal (100%)";
    }
    
    private void CheckColliderSetup(TimeField field)
    {
        Collider col = field.GetComponent<Collider>();
        
        if (col == null)
        {
            EditorGUILayout.HelpBox("TimeField requer um Collider! Adicione um BoxCollider ou SphereCollider.", MessageType.Error);
            
            if (GUILayout.Button("Add Box Collider"))
            {
                Undo.AddComponent<BoxCollider>(field.gameObject);
            }
        }
        else if (!col.isTrigger)
        {
            EditorGUILayout.HelpBox("Collider deve estar marcado como 'Is Trigger'.", MessageType.Warning);
            
            if (GUILayout.Button("Fix: Set Is Trigger = True"))
            {
                Undo.RecordObject(col, "Set Trigger");
                col.isTrigger = true;
            }
        }
    }
}


[CustomEditor(typeof(TimeEntity))]
public class TimeEntityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TimeEntity entity = (TimeEntity)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Time Entity", EditorStyles.boldLabel);
        

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);
            
            float scale = entity.LocalTimeScale;
            EditorGUILayout.LabelField($"Local Time Scale: {scale:F3}");
            EditorGUILayout.LabelField($"Local Delta Time: {entity.LocalDeltaTime:F4}");
            
            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(rect, scale, GetStatusLabel(scale));
            
            EditorGUILayout.EndVertical();
            
            Repaint();
        }
        
        CheckComponents(entity);
    }
    
    private string GetStatusLabel(float scale)
    {
        if (scale <= 0f) return "⏸️ FROZEN";
        if (scale < 0.5f) return "🐌 SLOW";
        if (scale < 1f) return "⏳ SLOWED";
        return "▶️ NORMAL";
    }
    
    private void CheckComponents(TimeEntity entity)
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Detected Components", EditorStyles.boldLabel);
        
        DrawComponentStatus("Rigidbody", entity.GetComponent<Rigidbody>());
        DrawComponentStatus("Animator", entity.GetComponent<Animator>());
        DrawComponentStatus("NavMeshAgent", entity.GetComponent<UnityEngine.AI.NavMeshAgent>());
        
        ParticleSystem[] particles = entity.GetComponentsInChildren<ParticleSystem>();
        DrawComponentStatus($"ParticleSystem ({particles.Length})", particles.Length > 0);
        
        var updatables = entity.GetComponents<ITimeScaledUpdate>();
        DrawComponentStatus($"ITimeScaledUpdate ({updatables.Length})", updatables.Length > 0);
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawComponentStatus(string componentName, bool exists)
    {
        DrawComponentStatus(componentName, exists ? new object() : null);
    }
    
    private void DrawComponentStatus(string componentName, object component)
    {
        EditorGUILayout.BeginHorizontal();
        
        GUIStyle style = new GUIStyle(EditorStyles.label);
        if (component != null)
        {
            style.normal.textColor = new Color(0.3f, 0.8f, 0.3f);
            EditorGUILayout.LabelField("✓", style, GUILayout.Width(20));
        }
        else
        {
            style.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            EditorGUILayout.LabelField("○", style, GUILayout.Width(20));
        }
        
        EditorGUILayout.LabelField(componentName, style);
        EditorGUILayout.EndHorizontal();
    }
}
#endif

