using UnityEngine;

public class DebugColliderDestruction : MonoBehaviour
{
    private Transform lastParent;

    void Start()
    {
        lastParent = transform.parent;
    }

    void Update()
    {
        if (transform.parent != lastParent)
        {
            Debug.LogWarning("Parent changed! Old: " + (lastParent ? lastParent.name : "null") +
                             " New: " + (transform.parent ? transform.parent.name : "null") +
                             " at time: " + Time.time, gameObject);
            lastParent = transform.parent;
        }
    }
}
