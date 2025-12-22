using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class LightSystem : MonoBehaviour
{
    #region Inspector
    
    [Header("Renderers")]
    [SerializeField] private Renderer[] renderers;
    
    [Header("Lights")]
    [SerializeField] private Light[] lights;
    
    [Header("Emission Settings")]
    [SerializeField] private bool startWithEmission = false;
    [SerializeField] private Color emissionColor = Color.white;
    [SerializeField] private float emissionIntensity = 1f;
    
    [Header("Base Color Settings")]
    [SerializeField] private Color baseColorWhenOff = Color.white;
    [SerializeField] private bool controlBaseColor = true;
    
    [Header("Material Property Names")]
    [SerializeField] private string emissionColorProperty = "_EmissionColor";
    [SerializeField] private string baseColorProperty = "_BaseColor";
    
    [Header("Events")]
    [SerializeField] private UnityEvent onLightEnabled;
    [SerializeField] private UnityEvent onLightDisabled;
    
    #endregion
    
    #region Private
    
    private Material[] materials;
    private Color[] originalEmissionColors;
    private bool[] originalEmissionStates;
    private bool isEmissionEnabled = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeMaterials();
        InitializeLights();
        
        if (startWithEmission)
        {
            SetEmissionEnabled(true);
            SetLightsEnabled(true);
        }
        else
        {
            SetEmissionEnabled(false);
            SetLightsEnabled(false);
            UpdateBaseColor();
        }
    }
    
    private void InitializeMaterials()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }
        
        if (renderers.Length == 0) return;
        
        int totalMaterials = 0;
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                totalMaterials += renderer.sharedMaterials.Length;
            }
        }
        
        materials = new Material[totalMaterials];
        originalEmissionColors = new Color[totalMaterials];
        originalEmissionStates = new bool[totalMaterials];
        
        int index = 0;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            
            Material[] sharedMats = renderer.sharedMaterials;
            Material[] instanceMats = new Material[sharedMats.Length];
            
            for (int i = 0; i < sharedMats.Length; i++)
            {
                if (sharedMats[i] != null)
                {
                    instanceMats[i] = new Material(sharedMats[i]);
                    materials[index] = instanceMats[i];
                    
                    if (instanceMats[i].HasProperty(emissionColorProperty))
                    {
                        originalEmissionColors[index] = instanceMats[i].GetColor(emissionColorProperty);
                        originalEmissionStates[index] = instanceMats[i].IsKeywordEnabled("_EMISSION");
                    }
                    else if (instanceMats[i].HasProperty("_EmissionColor"))
                    {
                        originalEmissionColors[index] = instanceMats[i].GetColor("_EmissionColor");
                        originalEmissionStates[index] = instanceMats[i].IsKeywordEnabled("_EMISSION");
                    }
                    
                    index++;
                }
            }
            
            renderer.materials = instanceMats;
        }
    }
    
    private void InitializeLights()
    {
        if (lights == null || lights.Length == 0)
        {
            lights = GetComponentsInChildren<Light>();
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void SetEmissionEnabled(bool enabled)
    {
        if (materials == null || materials.Length == 0) return;
        
        isEmissionEnabled = enabled;
        
        foreach (Material mat in materials)
        {
            if (mat == null) continue;
            
            if (enabled)
            {
                Color finalEmissionColor = emissionColor * emissionIntensity;
                
                if (mat.HasProperty(emissionColorProperty))
                {
                    mat.SetColor(emissionColorProperty, finalEmissionColor);
                }
                else if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", finalEmissionColor);
                }
                
                mat.EnableKeyword("_EMISSION");
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                
                if (controlBaseColor)
                {
                    if (mat.HasProperty(baseColorProperty))
                    {
                        mat.SetColor(baseColorProperty, baseColorWhenOff);
                    }
                    else if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", baseColorWhenOff);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", baseColorWhenOff);
                    }
                }
            }
        }
        
        if (enabled)
        {
            DynamicGI.UpdateEnvironment();
            onLightEnabled?.Invoke();
        }
        else
        {
            onLightDisabled?.Invoke();
        }
    }
    
    public void SetLightsEnabled(bool enabled)
    {
        if (lights == null) return;
        
        foreach (Light light in lights)
        {
            if (light != null)
            {
                light.enabled = enabled;
            }
        }
    }
    
    public void SetLightEnabled(bool enabled)
    {
        SetEmissionEnabled(enabled);
        SetLightsEnabled(enabled);
    }
    
    public void ToggleLight()
    {
        bool newState = !isEmissionEnabled;
        SetLightEnabled(newState);
    }
    
    public void SetEmissionColor(Color color)
    {
        emissionColor = color;
        UpdateEmissionColor();
    }
    
    public void SetEmissionIntensity(float intensity)
    {
        emissionIntensity = Mathf.Max(0f, intensity);
        UpdateEmissionColor();
    }
    
    private void UpdateEmissionColor()
    {
        if (materials == null || materials.Length == 0) return;
        
        if (isEmissionEnabled)
        {
            Color finalEmissionColor = emissionColor * emissionIntensity;
            
            foreach (Material mat in materials)
            {
                if (mat == null) continue;
                
                if (mat.HasProperty(emissionColorProperty))
                {
                    mat.SetColor(emissionColorProperty, finalEmissionColor);
                }
                else if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", finalEmissionColor);
                }
            }
            
            DynamicGI.UpdateEnvironment();
        }
    }
    
    public void SetBaseColorWhenOff(Color color)
    {
        baseColorWhenOff = color;
        
        if (!isEmissionEnabled && controlBaseColor)
        {
            UpdateBaseColor();
        }
    }
    
    private void UpdateBaseColor()
    {
        if (materials == null || materials.Length == 0) return;
        
        if (!isEmissionEnabled && controlBaseColor)
        {
            foreach (Material mat in materials)
            {
                if (mat == null) continue;
                
                if (mat.HasProperty(baseColorProperty))
                {
                    mat.SetColor(baseColorProperty, baseColorWhenOff);
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", baseColorWhenOff);
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", baseColorWhenOff);
                }
            }
        }
    }
    
    public bool IsEmissionEnabled => isEmissionEnabled;
    
    public bool IsLightEnabled => lights != null && lights.Length > 0 && lights[0] != null && lights[0].enabled;
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        if (materials != null)
        {
            foreach (Material mat in materials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }
    
    #endregion
}

