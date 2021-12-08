using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ReplenisherFSM : MonoBehaviour
{
    private enum MyState { Patrolling, Peeing, ReplenishBananas, ReplenishMonkeys };
    [SerializeField] private MyState myState;
    [SerializeField] private float nearDistance;

    [Header("Patrolling behaviour")]
    public Transform[] patrollingPoints;
    [SerializeField] private int patrollingIndex;
    [SerializeField] private Vector3 currentDestination;

    [Header("Peeing behaviour")]
    public float minTimeToPee = 180;
    public float maxTimeToPee = 300;
    [SerializeField] private float timeToPee;
    public Transform peeingPoint;
    public float minTimePeeing = 1.5f;
    public float maxTimePeeing = 4f;

    [Header("Replenishing bananas behaviour")]
    public int bananaAmt = 4;

    [Header("Replenishing monkeys behaviour")]
    public int minMonkeys = 4;
    public GorillaUS monkey;

    private StateMachineEngine replenisherFSM;
    private NavMeshAgent agent;

    // Start is called before the first frame update
    void Start()
    {
        myState = MyState.Patrolling;
        agent = GetComponent<NavMeshAgent>();

        peeingPoint = GameObject.Find("PeeingPoint").transform;

        SetPatrollingPoints();
        SetRandomTimeToPee();

        replenisherFSM = new StateMachineEngine();
        CreateStateMachine();
    }

    // Update is called once per frame
    void Update()
    {
        replenisherFSM.Update();

        SelectAction();
    }

    void SetRandomTimeToPee()
    {
        timeToPee = UnityEngine.Random.Range(minTimeToPee, maxTimeToPee);
    }

    void SetPatrollingPoints()
    {
        var patrollingParent = GameObject.Find("PatrollingPoints");

        List<Transform> auxList = new List<Transform>();
        patrollingParent.GetComponentsInChildren(false, auxList);

        auxList.RemoveAll((elem) => { return elem.gameObject.name == "PatrollingPoints"; });
        patrollingPoints = auxList.ToArray();
    }

    void CreateStateMachine()
    {
        //Perceptions
        Perception needsToPee = replenisherFSM.CreatePerception<TimerPerception>(timeToPee);
        Perception endedPeeing = replenisherFSM.CreatePerception<PushPerception>();

        Perception notEnoughBananas = replenisherFSM.CreatePerception<ValuePerception>(NotEnoughBananas);
        Perception notEnoughMonkeys = replenisherFSM.CreatePerception<ValuePerception>(NotEnoughMonkeys);

        Perception enoughBananas = replenisherFSM.CreatePerception<ValuePerception>(() => { return !NotEnoughBananas(); });
        Perception enoughMonkeys = replenisherFSM.CreatePerception<ValuePerception>(() => { return !NotEnoughMonkeys(); });


        //States
        State patrolling = replenisherFSM.CreateEntryState("patrolling", () => {
            //Debug.Log("Patrolling...");
            myState = MyState.Patrolling;
        });
        State peeing = replenisherFSM.CreateState("peeing", () => {
            //Debug.Log("Need to pee!");
            myState = MyState.Peeing;
            agent.SetDestination(peeingPoint.position);
            currentDestination = peeingPoint.position;
        });
        State replenishingBananas = replenisherFSM.CreateState("replenishbananas", () => {
            //Debug.Log("Replenishing bananas");
            myState = MyState.ReplenishBananas;
        });
        State replenishingMonkeys = replenisherFSM.CreateState("replenishingmonkeys", () => {
            //Debug.Log("Replenishing monkey");
            myState = MyState.ReplenishMonkeys;
        });


        //Transitions
        replenisherFSM.CreateTransition("going to pee", patrolling, needsToPee, peeing);
        replenisherFSM.CreateTransition("not enough bananas", patrolling, notEnoughBananas, replenishingBananas);
        replenisherFSM.CreateTransition("not enough monkeys", patrolling, notEnoughMonkeys, replenishingMonkeys);

        replenisherFSM.CreateTransition("ended peeing", peeing, endedPeeing, patrolling);

        replenisherFSM.CreateTransition("enough bananas", replenishingBananas, enoughBananas, patrolling);
        replenisherFSM.CreateTransition("enough monkeys", replenishingMonkeys, enoughMonkeys, patrolling);

    }

    void SelectAction()
    {
        AtAnyState();

        switch (myState)
        {
            case MyState.Patrolling:
                Patrolling();
                break;
            case MyState.Peeing:
                Peeing();
                break;
            case MyState.ReplenishBananas:
                ReplenishBananas();
                break;
            case MyState.ReplenishMonkeys:
                ReplenishMonkeys();
                break;
        }
    }

    void Patrolling()
    {
        if (patrollingPoints.Length == 0) return;

        if (FlattenedDistance(transform.position, patrollingPoints[patrollingIndex].position) < nearDistance)
        {
            patrollingIndex = (patrollingIndex + 1) % patrollingPoints.Length;
        }

        if (patrollingPoints[patrollingIndex].position != currentDestination)
        {
            agent.SetDestination(patrollingPoints[patrollingIndex].position);
            currentDestination = patrollingPoints[patrollingIndex].position;
        }
    }

    void Peeing()
    {
        if (FlattenedDistance(transform.position, peeingPoint.position) < nearDistance)
        {
            StartCoroutine(IsPeeing());
        }
    }

    IEnumerator IsPeeing()
    {
        float randomTimePeeing = UnityEngine.Random.Range(minTimePeeing, maxTimePeeing);

        yield return new WaitForSeconds(randomTimePeeing);

        replenisherFSM.Fire("ended peeing");
    }

    void ReplenishBananas()
    {
        Debug.Log("Not enough bananas!");

        bananaAmt++;
    }

    void ReplenishMonkeys()
    {
        Debug.Log("Not enough monkeys!");

        Instantiate(monkey, peeingPoint.position, peeingPoint.rotation);
    }

    void AtAnyState()
    {
        if (FlattenedDistance(transform.position, patrollingPoints[patrollingIndex].position) < nearDistance)
        {
            patrollingIndex = (patrollingIndex + 1) % patrollingPoints.Length;
        }
    }

    bool NotEnoughBananas()
    {
        var monkeyAmt = FindObjectsOfType<GorillaUS>().Length;

        return bananaAmt < (monkeyAmt / 2);
    }

    bool NotEnoughMonkeys()
    {
        var monkeyAmt = FindObjectsOfType<GorillaUS>().Length;

        return monkeyAmt < minMonkeys;
    }

    public float FlattenedDistance(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;

        return Vector3.Distance(a, b);
    }
}
