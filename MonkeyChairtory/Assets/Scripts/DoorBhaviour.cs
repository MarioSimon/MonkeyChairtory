using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorBhaviour : MonoBehaviour
{
    private bool open;
    private float timePass;
    private Quaternion aux;

    // Start is called before the first frame update
    void Start()
    {
        open = false;
        timePass = 0.0f;
        aux = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        timePass += Time.deltaTime;
        if (open && timePass >= 3.0f)
        {
            transform.rotation = aux;
            open = false;
            GetComponent<BoxCollider>().enabled = true;
        }
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.O))
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + 90, transform.rotation.eulerAngles.z);
            open = true;
            timePass = 0.0f;
            GetComponent<BoxCollider>().enabled = true;
        }
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!open && other.tag == "Human" && this.tag != "BackDoor")
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + 90, transform.rotation.eulerAngles.z);
            open = true;
            timePass = 0.0f;
            GetComponent<BoxCollider>().enabled = true;
        }      
    }
}
