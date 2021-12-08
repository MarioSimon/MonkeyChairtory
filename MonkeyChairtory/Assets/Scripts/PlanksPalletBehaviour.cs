using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanksPalletBehaviour : MonoBehaviour
{
    public bool isFromTreatyZone;

    public int planksAmt = 0;
    public int maxPlanksAllowed = 15;

    List<GameObject> planks = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        planks = new List<GameObject>();

        //if(isFromTreatyZone)
            PresetPlanksAmount(FindObjectOfType<GorillaUS>().plankObject, 20);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void PresetPlanksAmount(GameObject plank, int initialAmt)
    {
        for (int i = 0; i < initialAmt; i++)
        {
            IncludePlank(plank);
        }
    }

    public void IncludePlank(GameObject plank)
    {
        Vector3 position = CalculateNextPlankPos();
        var newPlank = Instantiate(plank, position, Quaternion.Euler(0, 0, 90));
        planks.Add(newPlank);
        planksAmt++;
        if (isFromTreatyZone)
            FindObjectOfType<WorldManager>().planksInTreatyZone++;
        else
            FindObjectOfType<WorldManager>().planksInMountingZone++;

    }

    public void RemovePlank()
    {
        GameObject oldPlank = planks[planksAmt - 1];
        planks.RemoveAt(planksAmt - 1);
        Destroy(oldPlank);
        planksAmt--;
        if (isFromTreatyZone)
            FindObjectOfType<WorldManager>().planksInTreatyZone--;
        else
            FindObjectOfType<WorldManager>().planksInMountingZone--;

    }

    Vector3 CalculateNextPlankPos()
    {
        Vector3 vector = transform.position + Vector3.up * (planksAmt + 1);

        return vector;
    }
}