using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GorillaUS : MonoBehaviour
{
    public bool isAngry;
    public bool isTrapped;
    public bool isJailed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TrapGorilla(Transform patroller)
    {
        transform.parent = patroller;
        isTrapped = true;
    }

    public void ReleaseGorilla()
    {
        transform.parent = null;
        isTrapped = false;
        isAngry = false;
        isJailed = true;
    }
}
