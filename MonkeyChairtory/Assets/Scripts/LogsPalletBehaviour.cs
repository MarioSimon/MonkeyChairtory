using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogsPalletBehaviour : MonoBehaviour
{
    public int logsAmt = 0;
    public int maxLogsAllowed = 15;

    List<GameObject> logs = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        logs = new List<GameObject>();

        var amt = FindObjectOfType<WorldManager>().logsInTreatyZone;

        PresetLogsAmount(FindObjectOfType<GorillaUS>().logObject, amt / 4);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PresetLogsAmount(GameObject log, int initialAmt)
    {
        for(int i = 0; i < initialAmt; i++)
        {
            IncludeLog(log);
        }
    }

    public void IncludeLog(GameObject log)
    {
        Vector3 position = CalculateNextLogPos();
        var newLog = Instantiate(log, position, Quaternion.Euler(0, 0, 90));
        logs.Add(newLog);
        logsAmt++;
        FindObjectOfType<WorldManager>().logsInTreatyZone++;
    }

    public void RemoveLog()
    {
        GameObject oldLog = logs[logsAmt - 1];
        logs.RemoveAt(logsAmt - 1);
        Destroy(oldLog);
        logsAmt--;
        FindObjectOfType<WorldManager>().logsInTreatyZone--;
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
