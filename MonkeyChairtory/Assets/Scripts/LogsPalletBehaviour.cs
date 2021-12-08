using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogsPalletBehaviour : MonoBehaviour
{
    public int logsAmt = 0;
    public int maxLogsAllowed = 15;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void IncludeLog(GameObject log)
    {
        Vector3 position = CalculateNextLogPos();
        Instantiate(log, position, Quaternion.Euler(0, 0, 90));
        logsAmt++;
    }

    Vector3 CalculateNextLogPos()
    {
        Vector3 vector = transform.position + Vector3.up * (1 + logsAmt / 3);

        switch (logsAmt % 3)
        {
            case 1:
                vector += Vector3.back;
                break;
            case 2:
                vector += Vector3.forward;
                break;
        }

        return vector;
    }
}
