using UnityEngine;

public class TelekinesisObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _weight;
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private int _telekinesisLayerMask;
    [SerializeField] private int _ignorePlayerMask;

    public Rigidbody Rigibody => _rb;
    public float Weight => _weight;

    public void OnGrab()
    {
        
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        gameObject.layer = _ignorePlayerMask;
    }

    public void OnRelease()
    {
        
        _rb.constraints = RigidbodyConstraints.None;
        gameObject.layer = _telekinesisLayerMask;
    }
}
