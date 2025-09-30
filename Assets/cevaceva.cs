using UnityEngine;

public class LockColliderCorrection : MonoBehaviour
{
    private Transform initialParent;
    private Vector3 initialPos;
    private Quaternion initialRot;

    void Awake()
    {
        initialParent = transform.parent;
        initialPos = transform.position;
        initialRot = transform.rotation;
    }

    void LateUpdate()
    {
        // Keep hierarchy fixed
        if (transform.parent != initialParent)
            transform.SetParent(initialParent, true);

        // Keep position & rotation fixed
        transform.position = initialPos;
        transform.rotation = initialRot;
    }

    void OnDisable()
    {
        // Immediately re-enable if accidentally disabled
        gameObject.SetActive(true);
    }
}
