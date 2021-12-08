using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GorillaUS : MonoBehaviour
{
    public bool isAngry;
    public bool isTrapped;
    public bool isJailed;

    [Header("Treaty zone config")]
    [SerializeField] private bool isInTreatyZone;

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

        SetRandomHunger();

        gorillaCurves = new UtilitySystemEngine(1.0f);
        hungerBehaviour = new BehaviourTreeEngine();
        CreateUtilityCurves();
    }

    // Update is called once per frame
    void Update()
    {
        gorillaCurves.Update();

        string aux = "[";

        foreach (var action in gorillaCurves.actions)
        {
            aux += action.getUtility() + "/ ";
        }

        aux += "]";

        Debug.Log(aux);

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

        gorillaCurves.CreateUtilityAction("logs to treaty", () => { Debug.Log("Moving logs..."); }, logsInTreatyZone);
        gorillaCurves.CreateUtilityAction("planks to mounting", () => { Debug.Log("Moving planks..."); }, planksFromTreatingToMounting);
        gorillaCurves.CreateUtilityAction("treating logs", () => { Debug.Log("Treating logs..."); }, monkeysInTreatyZoneNeed);
        gorillaCurves.CreateUtilityAction("mounting chairs", () => { Debug.Log("Mounting chairs..."); }, monkeysInMountingZoneNeed);
        gorillaCurves.CreateUtilityAction("go eating", () => { Debug.Log("Going to eat..."); }, timeWithoutEating);
    }

    void CreateBehaviourTree()
    {

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
