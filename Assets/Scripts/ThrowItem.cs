using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowItem : MonoBehaviour
{
  public GameObject thePlayer;
  public GameObject theCan;
  public GameObject objectSpawn;
  public AudioSource throwSound;

  public float flySpeed = 40.0f;
  public bool isThrowing = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
     if (Input.GetButtonDown("Fire1"))
      {
        if (isThrowing == false)
        {
          StartCoroutine(ThrowCan());
        }

    }
  }
    IEnumerator ThrowCan()
    {
      isThrowing = true;
      thePlayer.GetComponent<Animator>().Play("GrenadeThrow");
      yield return new WaitForSeconds(1);
      Instantiate(theCan, objectSpawn.transform.position, objectSpawn.transform.rotation);
      thePlayer.GetComponent<Animator>().Play("Grounded");
      isThrowing = false;
    }
}
