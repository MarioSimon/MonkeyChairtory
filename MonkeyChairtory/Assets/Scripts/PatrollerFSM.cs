using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PatrollerFSM : MonoBehaviour
{
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

    [Header("Checking door behaviour")]
    public Transform doorTransform;

    [Header("Chasing monkey behaviour")]
    public int angryMonkeys;
    [SerializeField] private float nearDistance;

    [Header("Jailing monkey behaviour")]
    public Transform jailTransform;
    [SerializeField] private GorillaUS trappedGorilla;

    private StateMachineEngine patrollerFSM;
    private NavMeshAgent agent;

    private enum MyState { Patrolling, Peeing, CheckDoor, ChaseMonkey, JailMonkey };
    private MyState myState;

    // Start is called before the first frame update
    void Start()
    {
        myState = MyState.Patrolling;
        agent = GetComponent<NavMeshAgent>();

        SetRandomTimeToPee();

        patrollerFSM = new StateMachineEngine();
        CreateStateMachine();
    }

    // Update is called once per frame
    void Update()
    {
        patrollerFSM.Update();

        if (Input.GetMouseButtonDown(0))
        {
            patrollerFSM.Fire("receive warning");
        }

        SelectAction();
    }

    void SetRandomTimeToPee()
    {
        timeToPee = UnityEngine.Random.Range(minTimeToPee, maxTimeToPee);
    }

    void CreateStateMachine()
    {
        //Perceptions
        Perception needsToPee = patrollerFSM.CreatePerception<TimerPerception>(timeToPee);
        Perception endedPeeing = patrollerFSM.CreatePerception<PushPerception>();

        Perception receiveWarning = patrollerFSM.CreatePerception<PushPerception>();
        Perception angryMonkeys = patrollerFSM.CreatePerception<PushPerception>();
        Perception noAngryMonkeys = patrollerFSM.CreatePerception<PushPerception>();

        Perception moreMonkeys = patrollerFSM.CreatePerception<PushPerception>();
        Perception noMoreMonkeys = patrollerFSM.CreatePerception<PushPerception>();

        Perception distanceToMonkey = patrollerFSM.CreatePerception<ValuePerception>(NearToMonkey);
        Perception resumePatrolling = patrollerFSM.CreatePerception<PushPerception>();

        //States
        State patrolling = patrollerFSM.CreateEntryState("patrolling", () => {
            Debug.Log("Patrolling...");
            myState = MyState.Patrolling;
        });
        State peeing = patrollerFSM.CreateState("peeing", () => {
            Debug.Log("Need to pee!");
            myState = MyState.Peeing;
            agent.SetDestination(peeingPoint.position);
            currentDestination = peeingPoint.position;
        });
        State checkDoor = patrollerFSM.CreateState("checkdoor", () => {
            Debug.Log("Checking door");
            myState = MyState.CheckDoor;
            agent.SetDestination(doorTransform.position);
        });
        State chaseMonkey = patrollerFSM.CreateState("chasemonkey", () => {
            Debug.Log("Chasing monkey");
            myState = MyState.ChaseMonkey;
        });
        State jailMonkey = patrollerFSM.CreateState("jailmonkey", () => {
            Debug.Log("Jailing monkey");
            myState = MyState.JailMonkey;
            agent.SetDestination(jailTransform.position);
        });

        //Transitions
        patrollerFSM.CreateTransition("going to pee", patrolling, needsToPee, peeing);
        patrollerFSM.CreateTransition("receive warning", patrolling, receiveWarning, checkDoor);

        patrollerFSM.CreateTransition("ended peeing", peeing, endedPeeing, patrolling);

        patrollerFSM.CreateTransition("chase monkey", checkDoor, angryMonkeys, chaseMonkey);
        patrollerFSM.CreateTransition("no monkey", checkDoor, noAngryMonkeys, patrolling);

        patrollerFSM.CreateTransition("jailing monkey", chaseMonkey, distanceToMonkey, jailMonkey);
        patrollerFSM.CreateTransition("no more monkey", chaseMonkey, noMoreMonkeys, patrolling);

        patrollerFSM.CreateTransition("resume patrolling", jailMonkey, resumePatrolling, patrolling);
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
            case MyState.CheckDoor:
                CheckDoor();
                break;
            case MyState.ChaseMonkey:
                ChaseMonkey();
                break;
            case MyState.JailMonkey:
                JailMonkey();
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

        if(patrollingPoints[patrollingIndex].position != currentDestination) 
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

        patrollerFSM.Fire("ended peeing");
    }

    void CheckDoor()
    {
        if (FlattenedDistance(transform.position, doorTransform.position) < nearDistance)
        {
            CheckForAMonkey();
        }
    }

    void CheckForAMonkey()
    {
        if (angryMonkeys > 0)
        {
            patrollerFSM.Fire("chase monkey");
        }
        else
        {
            patrollerFSM.Fire("no monkey");
        }
    }

    void CalculateDistanceToMonkey(out float minDistance, out GorillaUS gorillaToChase)
    {
        minDistance = float.MaxValue;
        gorillaToChase = null;

        foreach (var gorilla in FindObjectsOfType<GorillaUS>())
        {
            if (!gorilla.isAngry || gorilla.isTrapped) continue;

            float distance = FlattenedDistance(transform.position, gorilla.transform.position);
            if (distance < minDistance)
            {
                gorillaToChase = gorilla;
                minDistance = distance;
            }
        }
    }

    void ChaseMonkey()
    {
        CalculateDistanceToMonkey(out float distance, out GorillaUS gorillaToChase);

        if (gorillaToChase != null)
            agent.SetDestination(gorillaToChase.transform.position);
        else
            patrollerFSM.Fire("no more monkey");
    }

    bool NearToMonkey()
    {
        CalculateDistanceToMonkey(out float distance, out GorillaUS gorillaToChase);

        if (distance < nearDistance)
        {
            gorillaToChase.transform.parent = transform;
            trappedGorilla = gorillaToChase;
            gorillaToChase.isTrapped = true;
            return true;
        }
        else
        {
            return false;
        }
    }

    void JailMonkey()
    {
        if (trappedGorilla == null)
        {
            patrollerFSM.Fire("resume patrolling");
        }

        else if (FlattenedDistance(transform.position, jailTransform.position) < nearDistance)
        {
            trappedGorilla.transform.parent = null;
            patrollerFSM.Fire("resume patrolling");
        }
    }

    void AtAnyState()
    {
        if (FlattenedDistance(transform.position, patrollingPoints[patrollingIndex].position) < nearDistance)
        {
            patrollingIndex = (patrollingIndex + 1) % patrollingPoints.Length;
        }
    }

    public float FlattenedDistance(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;

        return Vector3.Distance(a, b);
    }
}
