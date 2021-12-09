using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackDoorBehaviour : MonoBehaviour
{
    private bool open;
    private bool gorillaInPlace;
    private float timePassed;
    private Quaternion aux;

    public float threshold = 15;

    // Start is called before the first frame update
    void Start()
    {
        open = false;
        gorillaInPlace = false;
        timePassed = 0.0f;
        aux = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        //timePassed += Time.deltaTime;
        if (gorillaInPlace || open)
        {
            timePassed += Time.deltaTime;
        }
        if (gorillaInPlace && timePassed >= threshold)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + 90, transform.rotation.eulerAngles.z);
            open = true;
            gorillaInPlace = false;
            timePassed = 0.0f;
        }

        if (open && timePassed >= 10.0f)
        {
            transform.rotation = aux;
            open = false;
            //GetComponent<BoxCollider>().enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "gorilla" && !gorillaInPlace)
        {
            //Debug.Log("Gorilla in place");
            gorillaInPlace = true;
            timePassed = 0.0f;
            //GetComponent<BoxCollider>().enabled = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log("Gorilla left");
        gorillaInPlace = false;
    }
}
