using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeCameraMovement : MonoBehaviour
{
    private float speedH = 2.0f;
    private float speedV = 2.0f;
    private float movSpeed = 10.0f;

    private float hor = 0f;
    private float ver = 0f;

    // Update is called once per frame
    void Update()
    {
        hor += speedH * Input.GetAxis("Mouse X");
        ver -= speedV * Input.GetAxis("Mouse Y");

        transform.eulerAngles = new Vector3(ver, hor, 0f);

        if (Input.GetKey(KeyCode.W))
        {
            transform.position +=  transform.forward * movSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * movSpeed * Time.deltaTime;
        }
    }
}
