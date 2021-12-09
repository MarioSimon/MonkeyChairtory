using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GorillaUS : MonoBehaviour
{
    public bool isAngry;
    public bool isTrapped;
    public bool isJailed;

    private enum GorillaState { MovingLogs, TreatingLogs, MovingPlanks, MountingChairs, GoingToEat, StillAngry };
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
    public GameObject chairObject;
    [SerializeField] private GameObject chairMoved;
    [SerializeField] private bool isInMountingZone;
    [SerializeField] private bool isMountingAChair;
    [SerializeField] private bool chairMounted;

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
        hungerBehaviour = new BehaviourTreeEngine(BehaviourEngine.IsASubmachine);

        CreateBehaviourTree();
        CreateUtilityCurves();
    }

    // Update is called once per frame
    void Update()
    {
        if (actionEnded)
        {
            actionEnded = false;
            gorillaCurves.Update();
            //if (gorillaCurves.ActiveAction.utilityState.Name == "go eating") gorillaState = GorillaState.GoingToEat;
            hungerBehaviour.Update();
        }

        PrintUtilityValues();

        SelectAction();

        //TODO make hunger more realistic
        UpdateHungerValue();
    }

    void SetRandomHunger()
    {
        maxSelectedHunger = UnityEngine.Random.Range(minRandomHunger, maxRandomHunger);

        //maxSelectedHunger = 40;
    }

    void UpdateHungerValue()
    {
        currentHunger += Time.deltaTime;
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

        Factor leafMonkeysInTreatyZone = new LeafVariable(MonkeysInTreatyNeed, 1, 0);
        Factor leafMonkeysInMountingZone = new LeafVariable(MonkeysInMountingNeed, 1, 0);
        //Factor leafMonkeysInMountingZone = new LeafVariable(() => 0, 1, 0);

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

        gorillaCurves.CreateUtilityAction("logs to treaty", () => { 
            gorillaState = GorillaState.MovingLogs; 
        }, logsInTreatyZone);
        gorillaCurves.CreateUtilityAction("planks to mounting", () => { 
            gorillaState = GorillaState.MovingPlanks; 
        }, planksFromTreatingToMounting);
        gorillaCurves.CreateUtilityAction("treating logs", () => { 
            gorillaState = GorillaState.TreatingLogs; 
        }, monkeysInTreatyZoneNeed);
        gorillaCurves.CreateUtilityAction("mounting chairs", () => { 
            gorillaState = GorillaState.MountingChairs; 
        }, monkeysInMountingZoneNeed);
        gorillaCurves.CreateSubBehaviour("go eating", timeWithoutEating, hungerBehaviour);

        
    }

    void CreateBehaviourTree()
    {
        LeafNode moveToRestAreaNode = hungerBehaviour.CreateLeafNode("move to rest area", MoveToRestArea, IsInRestArea); ;
        LeafNode areBananasLeft = hungerBehaviour.CreateLeafNode("are bananas left", () => { }, AreBananasLeft);
        LeafNode eatingBananas = hungerBehaviour.CreateLeafNode("eating bananas", EatBanana, () => ReturnValues.Succeed);
        SequenceNode areBananasAndEat = hungerBehaviour.CreateSequenceNode("are bananas and eat", false);
        areBananasAndEat.AddChild(areBananasLeft);
        areBananasAndEat.AddChild(eatingBananas);
        LeafNode gettingAngry = hungerBehaviour.CreateLeafNode("getting angry", GetAngry, () => ReturnValues.Succeed);
        SelectorNode eatingOrGettingAngry = hungerBehaviour.CreateSelectorNode("eating or angry");
        eatingOrGettingAngry.AddChild(areBananasAndEat);
        eatingOrGettingAngry.AddChild(gettingAngry);
        SequenceNode rootNode = hungerBehaviour.CreateSequenceNode("root node", false);
        rootNode.AddChild(moveToRestAreaNode);
        rootNode.AddChild(eatingOrGettingAngry);

        Perception exitPerception = hungerBehaviour.CreatePerception<PushPerception>();

        hungerBehaviour.CreateExitTransition("Exit_Transition", moveToRestAreaNode.StateNode, exitPerception, gorillaCurves);

        hungerBehaviour.SetRootNode(rootNode);
    }

    void SelectAction()
    {
        if (!isAngry)
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
                case GorillaState.MountingChairs:
                    MountChairs();
                    break;
                case GorillaState.GoingToEat:
                    GoToEat();
                    break;
                case GorillaState.StillAngry:
                    CalmDown();
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
        PlanksPalletBehaviour planksPallet = null;

        if (logMoved == null && plankMoved == null)
        {
            logsPallet = SelectLogPallet(false);
            destination = logsPallet.transform.position;
        }
        else if (SelectLogToPlankTable() != null && !isInTreatyZone)
        {
            logToPlankTable = SelectLogToPlankTable();
            destination = logToPlankTable.transform.position;
        }
        else if (isInTreatyZone && plankMoved == null)
        {
            destination = transform.position;
        }
        else if (plankMoved != null)
        {
            planksPallet = SelectPlanksPallet(true);
            destination = planksPallet.transform.position;
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
                if (!isTreatingALog && logMoved != null)
                {
                    StartCoroutine(TreatingALog());
                    isTreatingALog = true;
                }
                if (logTreated && logMoved != null)
                {
                    Destroy(logMoved);
                    plankMoved = Instantiate(plankObject, transform.position + Vector3.up * 2, Quaternion.Euler(0, 0, 90));
                    plankMoved.transform.parent = transform;
                    isTreatingALog = false;
                }
                if (planksPallet != null && plankMoved != null)
                {
                    planksPallet.IncludePlank(plankMoved);
                    Destroy(plankMoved);
                    isInTreatyZone = false;
                    isTreatingALog = false;
                    logTreated = false;
                    actionEnded = true;
                }
            }
        }
    }

    void MovePlanks()
    {
        Vector3 destination = Vector3.zero;

        PlanksPalletBehaviour startPlanksPallet = null;
        PlanksPalletBehaviour endPlanksPallet = null;

        if (plankMoved == null)
        {
            startPlanksPallet = SelectPlanksPallet(true, false);
            destination = startPlanksPallet.transform.position;
        }
        else
        {
            endPlanksPallet = SelectPlanksPallet(false);
            destination = endPlanksPallet.transform.position;
        }

        agent.SetDestination(destination);

        if (FlattenedDistance(transform.position, destination) < nearDistance)
        {
            if (startPlanksPallet != null)
            {
                startPlanksPallet.RemovePlank();
                plankMoved = Instantiate(plankObject, transform.position + Vector3.up * 2, Quaternion.Euler(0, 0, 90));
                plankMoved.transform.parent = transform;
            }
            if (endPlanksPallet != null)
            {
                endPlanksPallet.IncludePlank(plankMoved);
                Destroy(plankMoved);
                actionEnded = true;
            }
        }
    }

    void MountChairs()
    {
        Vector3 destination = Vector3.zero;
        PlanksPalletBehaviour planksPallet = null;
        PlankToChairTableBehaviour plankToChairTable = null;

        if (plankMoved == null && chairMoved == null)
        {
            planksPallet = SelectPlanksPallet(false, false);
            destination = planksPallet.transform.position;
        }
        else if (SelectLogToPlankTable() != null && !isInMountingZone)
        {
            plankToChairTable = SelectPlankToChairTable();
            destination = plankToChairTable.transform.position;
        }

        else if (isInMountingZone && chairMoved == null)
        {
            destination = transform.position;
        }
        else if (chairMoved != null)
        {
            //TODO include chair position
            //planksPallet = SelectPlanksPallet(true);
            destination = logsTransform.position;
        }
        else
        {
            agent.SetDestination(destination);
            actionEnded = true;
        }

        agent.SetDestination(destination);

        if (FlattenedDistance(transform.position, destination) < nearDistance)
        {
            if (planksPallet != null)
            {
                planksPallet.RemovePlank();
                plankMoved = Instantiate(plankObject, transform.position + Vector3.up * 2, Quaternion.Euler(0, 0, 90));
                plankMoved.transform.parent = transform;
            }
            else if (plankToChairTable != null)
            {
                plankToChairTable.isOccupied = isInMountingZone = true;
            }
            if (isInMountingZone)
            {
                if (!isMountingAChair && plankMoved != null)
                {
                    StartCoroutine(MountingAChair());
                    isMountingAChair = true;
                }
                if (chairMounted && plankMoved != null)
                {
                    Destroy(plankMoved);
                    chairMoved = Instantiate(chairObject, transform.position + Vector3.up * 2, Quaternion.identity);
                    chairMoved.transform.parent = transform;
                    isMountingAChair = false;
                }
                if (destination == logsTransform.position && chairMoved != null)
                {
                    //planksPallet.IncludePlank(plankMoved);
                    Destroy(chairMoved);
                    isInMountingZone = false;
                    isMountingAChair = false;
                    chairMounted = false;
                    actionEnded = true;
                }
            }
        }
    }

    void GoToEat()
    {
        Debug.Log("Going to eat");
        actionEnded = true;
    }

    void CalmDown()
    {
        StartCoroutine(CalmingDown());
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

    void ReleaseLogToPlankTable()
    {
        var logToPlankTables = FindObjectsOfType<LogToPlankTableBehaviour>();

        LogToPlankTableBehaviour closestTable = null;
        float minDistance = float.MaxValue;

        foreach (var table in logToPlankTables)
        {
            float dist = FlattenedDistance(transform.position, table.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestTable = table;
            }
        }

        closestTable.isOccupied = false;
    }

    void ReleasePlankToChairTable()
    {
        var plankToChairTables = FindObjectsOfType<PlankToChairTableBehaviour>();

        PlankToChairTableBehaviour closestTable = null;
        float minDistance = float.MaxValue;

        foreach (var table in plankToChairTables)
        {
            float dist = FlattenedDistance(transform.position, table.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestTable = table;
            }
        }

        closestTable.isOccupied = false;
    }

    PlanksPalletBehaviour SelectPlanksPallet(bool isFromTreatyZone, bool getMinimum = true)
    {
        var planksPallets = FindObjectsOfType<PlanksPalletBehaviour>();

        PlanksPalletBehaviour selectedPallet = null;
        var candidatePallets = new List<PlanksPalletBehaviour>();

        if (getMinimum)
        {
            int minimum = int.MaxValue;

            foreach (var pallet in planksPallets)
            {
                if (pallet.isFromTreatyZone != isFromTreatyZone) continue;

                if (pallet.planksAmt < minimum)
                {
                    candidatePallets.Clear();
                    minimum = pallet.planksAmt;
                    selectedPallet = pallet;
                    candidatePallets.Add(pallet);
                }
                if (pallet.planksAmt == minimum)
                {
                    candidatePallets.Add(pallet);
                }
            }
        }
        else
        {
            int maximum = int.MinValue;

            foreach (var pallet in planksPallets)
            {
                if (pallet.isFromTreatyZone != isFromTreatyZone) continue;

                if (pallet.planksAmt > maximum)
                {
                    candidatePallets.Clear();
                    maximum = pallet.planksAmt;
                    selectedPallet = pallet;
                    candidatePallets.Add(pallet);
                }
                if (pallet.planksAmt == maximum)
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

    IEnumerator TreatingALog()
    {
        yield return new WaitForSeconds(2.5f);

        Debug.Log("Log treated!");
        logTreated = true;
        ReleaseLogToPlankTable();
    }

    IEnumerator MountingAChair()
    {
        yield return new WaitForSeconds(2.5f);

        Debug.Log("Chair mounted!");
        chairMounted = true;
        ReleasePlankToChairTable();
    }

    PlankToChairTableBehaviour SelectPlankToChairTable()
    {
        var plankToChairTables = FindObjectsOfType<PlankToChairTableBehaviour>();

        PlankToChairTableBehaviour closestTable = null;
        float minDistance = float.MaxValue;

        foreach (var table in plankToChairTables)
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

    IEnumerator CalmingDown()
    {
        yield return new WaitForSeconds(5.0f);

        currentHunger = 0;
        isJailed = false;
        actionEnded = true;
    }

    float MonkeysInTreatyZone()
    {
        int monkeyAmt = 0;

        var monkeys = FindObjectsOfType<GorillaUS>();

        foreach (var monkey in monkeys)
        {
            if (monkey.isInTreatyZone) monkeyAmt++;
        }

        return monkeyAmt / 4.0f;
    }

    float MonkeysInTreatyNeed()
    {
        var world = FindObjectOfType<WorldManager>();

        var monkeyTreatyNeed = (MonkeysInTreatyZone() + world.PlanksInTreatyZoneRatio()) / 2.0f;

        return monkeyTreatyNeed;
    }

    float MonkeysInMountingZone()
    {
        int monkeyAmt = 0;

        var monkeys = FindObjectsOfType<GorillaUS>();

        foreach (var monkey in monkeys)
        {
            if (monkey.isInMountingZone) monkeyAmt++;
        }

        return monkeyAmt / 4.0f;
    }

    float MonkeysInMountingNeed()
    {
        var world = FindObjectOfType<WorldManager>();

        var monkeyMountingNeed = (MonkeysInMountingZone() + (1 - world.PlanksInMountingZoneRatio())) / 2.0f;

        return monkeyMountingNeed;
    }


    void MoveToRestArea()
    {
        gorillaState = GorillaState.GoingToEat;
        agent.SetDestination(new Vector3(21, 1.75f, 2));
    }

    ReturnValues IsInRestArea()
    {
        if (FlattenedDistance(transform.position, new Vector3(21, 1.75f, 2)) < nearDistance)
        {
            return ReturnValues.Succeed;
        }
        else return ReturnValues.Running;
    }

    ReturnValues AreBananasLeft()
    {
        return FindObjectOfType<WorldManager>().bananasAmt > 0 ? ReturnValues.Succeed : ReturnValues.Failed;
    }

    void EatBanana()
    {
        Debug.Log("Eat a banana...");
        currentHunger = 0;
        FindObjectOfType<WorldManager>().bananasAmt--;
        actionEnded = true;
    }

    void GetAngry()
    {
        Debug.LogError("Getting angry!");
        gorillaState = GorillaState.StillAngry;
        isAngry = true;
        agent.SetDestination(GameObject.Find("PeeingPoint").transform.position);
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
        currentHunger = 0;
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

        //Debug.Log(aux);
    }
}
