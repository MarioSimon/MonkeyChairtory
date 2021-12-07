using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PatrollerFSM : MonoBehaviour
{
    private enum MyState { Patrolling, Peeing, CheckDoor, ChaseMonkey, JailMonkey };
    [SerializeField] private MyState myState;

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
    [SerializeField] private int angryMonkeys;
    [SerializeField] private float nearDistance;

    [Header("Jailing monkey behaviour")]
    public Transform jailTransform;
    [SerializeField] private GorillaUS trappedGorilla;

    private StateMachineEngine patrollerFSM;
    private NavMeshAgent agent;

    

    // Start is called before the first frame update
    void Start()
    {
        myState = MyState.Patrolling;
        agent = GetComponent<NavMeshAgent>();

        doorTransform = GameObject.Find("DoorPosition").transform;
        jailTransform = GameObject.Find("JailPosition").transform;

        SetPatrollingPoints();
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
        Perception needsToPee = patrollerFSM.CreatePerception<TimerPerception>(timeToPee);
        Perception endedPeeing = patrollerFSM.CreatePerception<PushPerception>();

        Perception angryMonkeys = patrollerFSM.CreatePerception<ValuePerception>(AngryGorillasLeft);
        Perception noAngryMonkeys = patrollerFSM.CreatePerception<ValuePerception>(NotAngryGorillasLeft);

        Perception receiveWarning = patrollerFSM.CreatePerception<PushPerception>();

        Perception distanceToMonkey = patrollerFSM.CreatePerception<ValuePerception>(NearToMonkey);
        Perception isMonkeyJailed = patrollerFSM.CreatePerception<ValuePerception>(IsMonkeyJailed);


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
        patrollerFSM.CreateTransition("no more monkey", chaseMonkey, noAngryMonkeys, patrolling);

        patrollerFSM.CreateTransition("more monkeys", jailMonkey, isMonkeyJailed, chaseMonkey);
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
        Debug.Log("Checking door");
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
    }

    bool NearToMonkey()
    {
        CalculateDistanceToMonkey(out float distance, out GorillaUS gorillaToChase);

        if (distance < nearDistance)
        {
            gorillaToChase.TrapGorilla(transform);
            //gorillaToChase.transform.parent = transform;
            //gorillaToChase.isTrapped = true;
            trappedGorilla = gorillaToChase;
            return true;
        }
        else
        {
            return false;
        }
    }

    void JailMonkey()
    {
        Debug.Log("Jailing monkey");
    }

    void AtAnyState()
    {
        if (FlattenedDistance(transform.position, patrollingPoints[patrollingIndex].position) < nearDistance)
        {
            patrollingIndex = (patrollingIndex + 1) % patrollingPoints.Length;
        }
    }

    void UpdateAngryGorillaCount()
    {
        angryMonkeys = 0;

        foreach (var gorilla in FindObjectsOfType<GorillaUS>())
        {
            if (gorilla.isAngry && !gorilla.isTrapped)
            {
                angryMonkeys++;
            }
        }

        if (trappedGorilla != null)
        {
            angryMonkeys++;
        }
    }

    bool AngryGorillasLeft()
    {
        UpdateAngryGorillaCount();

        if (myState == MyState.CheckDoor)
        {
            if (!(FlattenedDistance(transform.position, doorTransform.position) < nearDistance))
            {
                return false;
            }
        }

        return angryMonkeys > 0;
    }

    bool NotAngryGorillasLeft()
    {
        UpdateAngryGorillaCount();

        if (myState == MyState.CheckDoor)
        {
            if (!(FlattenedDistance(transform.position, doorTransform.position) < nearDistance))
            {
                return false;
            }
        }

        return angryMonkeys <= 0;
    }

    bool IsMonkeyJailed()
    {
        if (trappedGorilla == null) return true;

        if (FlattenedDistance(transform.position, jailTransform.position) < nearDistance)
        {
            trappedGorilla.ReleaseGorilla();
            trappedGorilla = null;
            return true;
        }

        return false;
    }

    public float FlattenedDistance(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;

        return Vector3.Distance(a, b);
    }
}
