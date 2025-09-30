using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LateralCollisons : MonoBehaviour
{
    void Start() { }
    void Update() { }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("collision!!!");
        if (collision.gameObject.tag == "enemyCollider")
        {
            PlatformEnemy.turn = true;
            Debug.Log(PlatformEnemy.turn);
        }
    }
}
