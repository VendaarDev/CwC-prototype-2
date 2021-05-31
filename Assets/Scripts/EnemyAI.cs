using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
  public GameObject enemyDest;
  NavMeshAgent enemyAgent;
  public GameObject Enemy;
  public static bool isStalking = true;

    // Start is called before the first frame update
    void Start()
    {
        enemyAgent = GetComponent<NavMeshAgent>();

    }

    // Update is called once per frame
    void Update()
    {
      if (isStalking == false)
      {
  //      Enemy.GetComponent<Animator>().Play("Idle");
      }
      else
      {
  //     Enemy.GetComponent<Animator>().Play("Run");
       enemyAgent.SetDestination(enemyDest.transform.position);
      }
    }
}
