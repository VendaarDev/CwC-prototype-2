using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class treedistance : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Terrain.activeTerrain.treeDistance = 200;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
