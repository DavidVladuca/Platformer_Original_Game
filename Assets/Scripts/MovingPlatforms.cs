using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public float speed = 2f;
    public int startingPoint = 0;
    public Transform[] points;

    private int i;

    void Start()
    {
        if (points.Length == 0)
        {
            Debug.LogError("No points assigned to moving platform!", this);
            return;
        }

        transform.position = points[startingPoint].position;
        i = startingPoint;
    }

    void Update()
    {
        if (points.Length == 0) return;

        if (Vector2.Distance(transform.position, points[i].position) < 0.02f)
        {
            i++;
            if (i == points.Length)
                i = 0;
        }

        transform.position = Vector2.MoveTowards(transform.position, points[i].position, speed * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Ignore objects on the "GridStatic" layer (ColliderCorrection)
        if (collision.gameObject.layer == LayerMask.NameToLayer("GridStatic"))
            return;

        // Only parent relevant objects
        if (collision.transform != null && gameObject.activeInHierarchy)
        {
            collision.transform.SetParent(transform, true);
        }
    }


    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform != null && collision.transform.parent == transform)
        {
            collision.transform.SetParent(null, true);
        }
    }
}
