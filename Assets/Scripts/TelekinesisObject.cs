using UnityEngine;

public class TelekinesisObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _weight;
    [SerializeField] private Rigidbody _rb;

    public Rigidbody Rigibody => _rb;
    public float Weight => _weight;

    public void OnGrab()
    {
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void OnRelease()
    {
        _rb.isKinematic = false;
    }
}
