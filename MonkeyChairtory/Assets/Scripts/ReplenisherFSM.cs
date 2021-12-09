using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ReplenisherFSM : MonoBehaviour
{
    private enum ReplenisherState { Patrolling, Peeing, ReplenishBananas, ReplenishMonkeys };
    [SerializeField] private ReplenisherState myState;
    [SerializeField] private float nearDistance;

    [Header("Patrolling behaviour")]
    public Transform[] patrollingPoints;
    [SerializeField] private int patrollingIndex;
    [SerializeField] private Vector3 currentDestination;

    [Header("Peeing behaviour")]
    public float minTimeToPee = 55;
    public float maxTimeToPee = 65;
    [SerializeField] private float timeToPee;
    public Transform peeingPoint;
    public float minTimePeeing = 1.5f;
    public float maxTimePeeing = 4f;

    [Header("Replenishing bananas behaviour")]
    public int maxBananasHeld = 4;
    [SerializeField] private int bananasHeld = 0;
    public Transform bananaStorage;
    public Transform bananaPosition;
    [SerializeField] private GameObject bananaMoved;
    public GameObject bananaObject;

    [Header("Replenishing monkeys behaviour")]
    public int minMonkeys = 4;
    public GorillaUS monkey;

    private StateMachineEngine replenisherFSM;
    private NavMeshAgent agent;

    // Start is called before the first frame update
    void Start()
    {
        myState = ReplenisherState.Patrolling;
        agent = GetComponent<NavMeshAgent>();

        peeingPoint = GameObject.Find("PeeingPoint").transform;
        bananaPosition = GameObject.Find("BananaPosition").transform;
        bananaStorage = GameObject.Find("BananaStorage").transform;

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
            myState = ReplenisherState.Patrolling;
        });
        State peeing = replenisherFSM.CreateState("peeing", () => {
            //Debug.Log("Need to pee!");
            myState = ReplenisherState.Peeing;
            agent.SetDestination(peeingPoint.position);
            currentDestination = peeingPoint.position;
        });
        State replenishingBananas = replenisherFSM.CreateState("replenishbananas", () => {
            //Debug.Log("Replenishing bananas");
            myState = ReplenisherState.ReplenishBananas;
            agent.SetDestination(bananaStorage.position);
            currentDestination = bananaStorage.position;
        });
        State replenishingMonkeys = replenisherFSM.CreateState("replenishingmonkeys", () => {
            //Debug.Log("Replenishing monkey");
            myState = ReplenisherState.ReplenishMonkeys;
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
            case ReplenisherState.Patrolling:
                Patrolling();
                break;
            case ReplenisherState.Peeing:
                Peeing();
                break;
            case ReplenisherState.ReplenishBananas:
                ReplenishBananas();
                break;
            case ReplenisherState.ReplenishMonkeys:
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
        //TODO make replenish banana animation
        Debug.Log("Not enough bananas!");

        if (FlattenedDistance(transform.position, currentDestination) < nearDistance)
        {
            if (currentDestination == bananaStorage.position)
            {
                bananasHeld = maxBananasHeld;
                agent.SetDestination(bananaPosition.position);
                currentDestination = bananaPosition.position;

                bananaMoved = Instantiate(bananaObject, transform.position + new Vector3(1,-1,0), Quaternion.Euler(0, 0, 0));
                bananaMoved.transform.parent = transform;
            }
            else
            {
                if(bananasHeld > 0)
                {
                    FindObjectOfType<WorldManager>().bananasAmt += bananasHeld;
                    bananasHeld = 0;
                }
                if (NotEnoughBananas())
                {
                    agent.SetDestination(bananaStorage.position);
                    currentDestination = bananaStorage.position;
                }
                if (bananaMoved != null)
                {
                    Destroy(bananaMoved);
                }
            }
        }
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
        var bananaAmt = FindObjectOfType<WorldManager>().bananasAmt;

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
