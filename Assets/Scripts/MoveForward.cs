using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveForward : MonoBehaviour
{
  //public float speed = 40.0f;
  private Rigidbody rb;
  public float speed;


    void Awake()
    {
      rb = this.GetComponent<Rigidbody>();
    }
    // Update is called once per frame
    void Update()
    {
    //  transform.Translate(Vector3.forward * Time.deltaTime * speed, Camera.main.transform);

    rb.AddForce(gameObject.transform.forward * Time.deltaTime * speed, ForceMode.VelocityChange);


    }
}
