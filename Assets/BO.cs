// Attach this to ground if you want to be 100% sure nothing moves it
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BO : MonoBehaviour
{
    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        // Remember initial transform
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void LateUpdate()
    {
        // Force back to original
        transform.position = startPos;
        transform.rotation = startRot;
    }
}
