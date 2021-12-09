using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    [Header("Logs config")]
    public int logsInTreatyZone = 0;
    public int maxLogsInTreatyZone = 60;

    [Header("Planks treaty config")]
    public int planksInTreatyZone = 0;
    public int maxPlanksInTreatyZone = 400;

    [Header("Planks mounting config")]
    public int planksInMountingZone = 0;
    public int maxPlanksInMountingZone = 400;

    [Header("Bananas config")]
    public int bananasAmt = 2;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 45;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float PlanksInTreatyZoneRatio()
    {
        return planksInTreatyZone / (float)maxPlanksInTreatyZone;
    }

    public float PlanksInMountingZoneRatio()
    {
        return planksInMountingZone / (float)maxPlanksInMountingZone;
    }
}
