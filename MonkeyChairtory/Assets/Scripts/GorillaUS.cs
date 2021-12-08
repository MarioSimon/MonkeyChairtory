using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GorillaUS : MonoBehaviour
{
    public bool isAngry;
    public bool isTrapped;
    public bool isJailed;

    private enum GorillaState { MovingLogs, TreatingLogs, MovingPlanks, MountingChairs, GoingToEat };
    [SerializeField] private GorillaState gorillaState;
    [SerializeField] private float nearDistance = 1.25f;
    [SerializeField] public bool actionEnded;

    [Header("Moving logs config")]
    public Transform logsTransform;
    public GameObject logObject;
    [SerializeField] private GameObject logMoved;

    [Header("Moving planks config")]
    public GameObject plankObject;
    [SerializeField] private GameObject plankMoved;

    [Header("Treaty zone config")]
    [SerializeField] private bool isInTreatyZone;
    [SerializeField] private bool isTreatingALog;
    [SerializeField] private bool logTreated;

    [Header("Mounting zone config")]
    [SerializeField] private bool isInMountingZone;

    [Header("Hunger config")]
    [SerializeField] private float minRandomHunger = 30;
    [SerializeField] private float maxRandomHunger = 40;
    [SerializeField] private float maxSelectedHunger = 0;
    [SerializeField] private float currentHunger = 0;


    private UtilitySystemEngine gorillaCurves;
    private BehaviourTreeEngine hungerBehaviour;
    private NavMeshAgent agent;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        actionEnded = true;

        logsTransform = GameObject.Find("LogsPosition").transform;

        SetRandomHunger();

        gorillaCurves = new UtilitySystemEngine();
        hungerBehaviour = new BehaviourTreeEngine();
        CreateUtilityCurves();
    }

    // Update is called once per frame
    void Update()
    {
        if(actionEnded)
            gorillaCurves.Update();

        PrintUtilityValues();

        SelectAction();

        currentHunger += Time.deltaTime;
    }

    void SetRandomHunger()
    {
        maxSelectedHunger = UnityEngine.Random.Range(minRandomHunger, maxRandomHunger);

        maxSelectedHunger = 40;
    }

    void CreateUtilityCurves()
    {
        var world = FindObjectOfType<WorldManager>();

        Factor leafLogsInTreatyZone = new LeafVariable(() => world.logsInTreatyZone, world.maxLogsInTreatyZone, 0);
        Factor logsInTreatyZone = new LinearCurve(leafLogsInTreatyZone, -1, 1);

        Factor leafPlanksInTreatyZone = new LeafVariable(() => world.planksInTreatyZone, world.maxPlanksInTreatyZone, 0);
        Factor planksInTreatyZone = new LinearCurve(leafPlanksInTreatyZone);

        Factor leafPlanksInMountingZone = new LeafVariable(() => world.planksInMountingZone, world.maxPlanksInMountingZone, 0);
        Factor planksInMountingZone = new LinearCurve(leafPlanksInMountingZone, -1, 1);

        List<Factor> factors = new List<Factor>(){ planksInTreatyZone, planksInMountingZone };
        Factor planksFromTreatingToMounting = new WeightedSumFusion(factors);

        var monkeyTreatyAmt = MonkeysInTreatyZone();
        Factor auxMonkeysInTreatyZone = new LeafVariable(() => monkeyTreatyAmt, 4, 0);
        var monkeyTreatyNeed = (auxMonkeysInTreatyZone.getValue() + planksInTreatyZone.getValue()) / 2.0f;
        Factor leafMonkeysInTreatyZone = new LeafVariable(() => monkeyTreatyNeed, 1, 0);

        var monkeyMountingAmt = MonkeysInMountingZone();
        Factor auxMonkeysInMountingZone = new LeafVariable(() => monkeyMountingAmt, 4, 0);
        var monkeyMountingNeed = (auxMonkeysInMountingZone.getValue() + planksInMountingZone.getValue()) / 2.0f;
        Factor leafMonkeysInMountingZone = new LeafVariable(() => monkeyMountingNeed, 1, 0);

        List<Point2D> monkeyPoints = new List<Point2D>();
        monkeyPoints.Add(new Point2D(0, 1));
        monkeyPoints.Add(new Point2D(0.2f, 0.6f));
        monkeyPoints.Add(new Point2D(0.4f, 0.4f));
        monkeyPoints.Add(new Point2D(1, 0));

        Factor monkeysInTreatyZoneNeed = new LinearPartsCurve(leafMonkeysInTreatyZone, monkeyPoints);
        Factor monkeysInMountingZoneNeed = new LinearPartsCurve(leafMonkeysInMountingZone, monkeyPoints);



        Factor leafTimeWithoutEating = new LeafVariable(() => currentHunger, maxSelectedHunger, 0);

        List<Point2D> hungerGraphPoints = new List<Point2D>();
        hungerGraphPoints.Add(new Point2D(0, 0));
        hungerGraphPoints.Add(new Point2D(0.4f, 0.2f));
        hungerGraphPoints.Add(new Point2D(0.6f, 0.8f));
        hungerGraphPoints.Add(new Point2D(1, 1));

        Factor timeWithoutEating = new LinearPartsCurve(leafTimeWithoutEating, hungerGraphPoints);

        gorillaCurves.CreateUtilityAction("logs to treaty", () => { gorillaState = GorillaState.MovingLogs; actionEnded = false; }, logsInTreatyZone);
        gorillaCurves.CreateUtilityAction("planks to mounting", () => { gorillaState = GorillaState.MovingPlanks; actionEnded = false; }, planksFromTreatingToMounting);
        gorillaCurves.CreateUtilityAction("treating logs", () => { gorillaState = GorillaState.TreatingLogs; actionEnded = false; }, monkeysInTreatyZoneNeed);
        gorillaCurves.CreateUtilityAction("mounting chairs", () => { gorillaState = GorillaState.MountingChairs; actionEnded = false; }, monkeysInMountingZoneNeed);
        gorillaCurves.CreateUtilityAction("go eating", () => { gorillaState = GorillaState.GoingToEat; actionEnded = false; }, timeWithoutEating);
    }

    void CreateBehaviourTree()
    {

    }

    void SelectAction()
    {
        if (!isTrapped) //TODO fix this
        {
            switch (gorillaState)
            {
                case GorillaState.MovingLogs:
                    MoveLogs();
                    break;
                case GorillaState.TreatingLogs:
                    TreateLogs();
                    break;
                case GorillaState.MovingPlanks:
                    MovePlanks();
                    break;
            }

        }
    }

    void MoveLogs()
    {
        Vector3 destination = Vector3.zero;
        LogsPalletBehaviour selectedPallet = null;

        if (logMoved == null)
            destination = logsTransform.position;
        else if(SelectLogPallet() != null)
        {
            selectedPallet = SelectLogPallet();
            destination = selectedPallet.transform.position;
        }

        agent.SetDestination(destination);

        if (FlattenedDistance(transform.position, destination) < nearDistance)
        {
            if (logMoved == null)
            {
                logMoved = Instantiate(logObject, transform.position + Vector3.up * 2, Quaternion.Euler(0, 0, 90));
                logMoved.transform.parent = transform;
            }

            else
            {
                selectedPallet.IncludeLog(logMoved);
                Destroy(logMoved);
                actionEnded = true;
            }
        }

    }

    void TreateLogs()
    {
        Vector3 destination = Vector3.zero;
        LogsPalletBehaviour logsPallet = null;
        LogToPlankTableBehaviour logToPlankTable = null;

        if (logMoved == null)
        {
            logsPallet = SelectLogPallet(false);
            destination = logsPallet.transform.position;
        }
        else if (SelectLogToPlankTable() != null && !isInTreatyZone)
        {
            logToPlankTable = SelectLogToPlankTable();
            destination = logToPlankTable.transform.position;
            
        }
        else if (isInTreatyZone)
        {
            destination = transform.position;
        }
        else
        {
            agent.SetDestination(destination);
            actionEnded = true;
        }

        agent.SetDestination(destination);

        if (FlattenedDistance(transform.position, destination) < nearDistance)
        {
            if (logsPallet != null)
            {
                logsPallet.RemoveLog();
                logMoved = Instantiate(logObject, transform.position + Vector3.up * 2, Quaternion.Euler(0, 0, 90));
                logMoved.transform.parent = transform;
            }
            else if (logToPlankTable != null)
            {
                logToPlankTable.isOccupied = isInTreatyZone = true;
            }
            if (isInTreatyZone)
            {
                if (!isTreatingALog)
                {
                    StartCoroutine(TreatingALog());
                    isTreatingALog = true;
                }
                if (logTreated && logMoved != null)
                {
                    Destroy(logMoved);
                    plankMoved = Instantiate(plankObject, transform.position + Vector3.up * 2, Quaternion.Euler(0, 0, 90));
                }
            }
        }
    }

    void MovePlanks()
    {

    }

    void MountChairs()
    {

    }

    LogsPalletBehaviour SelectLogPallet(bool getMinimum = true)
    {
        var logsPallets = FindObjectsOfType<LogsPalletBehaviour>();

        LogsPalletBehaviour selectedPallet = null;
        var candidatePallets = new List<LogsPalletBehaviour>();

        if (getMinimum)
        {
            int minimum = int.MaxValue;

            foreach (var pallet in logsPallets)
            {
                if (pallet.logsAmt < minimum)
                {
                    candidatePallets.Clear();
                    minimum = pallet.logsAmt;
                    selectedPallet = pallet;
                    candidatePallets.Add(pallet);
                }
                if (pallet.logsAmt == minimum)
                {
                    candidatePallets.Add(pallet);
                }
            }
        }
        else
        {
            int maximum = int.MinValue;

            foreach (var pallet in logsPallets)
            {
                if (pallet.logsAmt > maximum)
                {
                    candidatePallets.Clear();
                    maximum = pallet.logsAmt;
                    selectedPallet = pallet;
                    candidatePallets.Add(pallet);
                }
                if (pallet.logsAmt == maximum)
                {
                    candidatePallets.Add(pallet);
                }
            }
        }

        if (candidatePallets.Count > 1)
        {
            float minDistance = float.MaxValue;

            foreach (var pallet in candidatePallets)
            {
                float dist = FlattenedDistance(transform.position, pallet.transform.position);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    selectedPallet = pallet;
                }
            }
        }

        return selectedPallet;
    }

    LogToPlankTableBehaviour SelectLogToPlankTable()
    {
        var logToPlankTables = FindObjectsOfType<LogToPlankTableBehaviour>();

        LogToPlankTableBehaviour closestTable = null;
        float minDistance = float.MaxValue;

        foreach (var table in logToPlankTables)
        {
            if (table.isOccupied) continue;

            float dist = FlattenedDistance(transform.position, table.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestTable = table;
            }
        }

        return closestTable;
    }

    IEnumerator TreatingALog()
    {
        yield return new WaitForSeconds(2.5f);

        Debug.Log("Log treated!");
        logTreated = true;
    }

    int MonkeysInTreatyZone()
    {
        int monkeyAmt = 0;

        var monkeys = FindObjectsOfType<GorillaUS>();

        foreach (var monkey in monkeys)
        {
            if (monkey.isInTreatyZone) monkeyAmt++;
        }

        return monkeyAmt;
    }

    int MonkeysInMountingZone()
    {
        int monkeyAmt = 0;

        var monkeys = FindObjectsOfType<GorillaUS>();

        foreach (var monkey in monkeys)
        {
            if (monkey.isInMountingZone) monkeyAmt++;
        }

        return monkeyAmt;
    }

    public void TrapGorilla(Transform patroller)
    {
        transform.parent = patroller;
        agent.isStopped = isTrapped = true;
        agent.enabled = false;
    }

    public void ReleaseGorilla()
    {
        transform.parent = null;
        agent.enabled = true;
        agent.isStopped = isTrapped = false;
        isAngry = false;
        isJailed = true;
    }

    float FlattenedDistance(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;

        return Vector3.Distance(a, b);
    }

    void PrintUtilityValues()
    {
        string aux = "[ ";
        foreach (var action in gorillaCurves.actions)
        {
            aux += action.getUtility().ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + " | ";
        }
        aux = aux.Remove(aux.LastIndexOf('|'));

        aux += "]";

        Debug.Log(aux);
    }
}
