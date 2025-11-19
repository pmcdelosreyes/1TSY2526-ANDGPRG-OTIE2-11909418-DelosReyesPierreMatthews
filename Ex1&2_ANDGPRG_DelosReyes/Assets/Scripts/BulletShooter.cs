using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BulletShooter : MonoBehaviour
{
    public Transform Collider1;
    public Transform Collider2;
    public Transform Collider3;
    public Transform Collider4;

    public GameObject bulletPrefab1;
    public GameObject bulletPrefab2;
    public GameObject bulletPrefab3;
    public GameObject bulletPrefab4;

    // Start is called before the first frame update
    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
       
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Instantiate(bulletPrefab1, Collider1.transform.position, transform.rotation);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Instantiate(bulletPrefab2, Collider2.transform.position, transform.rotation);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Instantiate(bulletPrefab3, Collider3.transform.position, transform.rotation);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Instantiate(bulletPrefab4, Collider4.transform.position, transform.rotation);
        } 

        Destroy(GameObject.Find("Cube(Clone)"), 2);
        Destroy(GameObject.Find("Sphere(Clone)"), 2);
        Destroy(GameObject.Find("Capsule(Clone)"), 2);
        Destroy(GameObject.Find("Cylinder(Clone)"), 2);
    }
}
