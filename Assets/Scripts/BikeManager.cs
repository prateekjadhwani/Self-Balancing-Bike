using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class BikeManager : Agent
{
    public WheelCollider frontWheel;
    public WheelCollider rearWheel;
    public float torqueForce;
    public float SteeringAngle = 60;
    //public Transform CenterOfMass;

    //public GameObject Blocker;
    public GameObject Target;


    private Rigidbody rb;
    private bool CollidedWithGround = false;
    //private bool CollidedWithBlocker = false;
    private bool CollidedWithTarget = false;

    private float TimeAlive;
    private float OldTimeAlive;

    /// <reset>
    private Transform originalLocation;
    /// </reset>

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        TimeAlive = 0;
        OldTimeAlive = 0;
    }

    private void Update()
    {
        TimeAlive += Time.deltaTime;
        //if(TimeAlive > OldTimeAlive)
        //{
        //    OldTimeAlive = TimeAlive;
        //}
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ApplyLocalPositionToVisuals(frontWheel);
        ApplyLocalPositionToVisuals(rearWheel);
    }

    public void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = collider.transform.GetChild(0);

        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);

        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }

    public override void AgentReset()
    {
        float maxLocation = 120f;

        if (CollidedWithGround)
        {
            float x = gameObject.transform.localPosition.x;
            float z = gameObject.transform.localPosition.z;
            gameObject.transform.localPosition = new Vector3(x, 28f, z);
            rb.velocity = Vector3.zero;
            gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);
            gameObject.transform.localPosition = new Vector3(Random.Range(-maxLocation, maxLocation), 28f, Random.Range(-maxLocation, maxLocation));
            TimeAlive = 0;
        }

        CollidedWithGround = false;
        CollidedWithTarget = false;

        GetNewTargetLocation();
    }

    private void GetNewTargetLocation()
    {
        float maxLocation = 120f;
        Target.transform.localPosition = new Vector3(Random.Range(-maxLocation, maxLocation), 10f, Random.Range(-maxLocation, maxLocation));

        if (Vector3.Distance(transform.localPosition, Target.transform.localPosition) < 10f)
        {
            GetNewTargetLocation();
        }
    }

    public override void CollectObservations()
    {
        // Velocity
        AddVectorObs(rb.velocity.x);
        AddVectorObs(rb.velocity.y);
        AddVectorObs(rb.velocity.z);

        // Position
        AddVectorObs(gameObject.transform.position.x);
        AddVectorObs(gameObject.transform.position.y);
        AddVectorObs(gameObject.transform.position.z);

        // Rotation
        AddVectorObs(gameObject.transform.rotation.x);
        AddVectorObs(gameObject.transform.rotation.y);
        AddVectorObs(gameObject.transform.rotation.z);

        // Target
        AddVectorObs(Target.transform.position.x);
        AddVectorObs(Target.transform.position.y);
        AddVectorObs(Target.transform.position.z);

        AddVectorObs(TimeAlive);

    }

    public override void AgentAction(float[] vectorAction)
    {
        rearWheel.motorTorque = torqueForce * vectorAction[0];
        frontWheel.steerAngle = SteeringAngle * vectorAction[1];

        if (gameObject.transform.position.y < -10f || gameObject.transform.position.y > 60f) {
            // Worst case scenario, when the bike falls off the plane
            // Or the bike jumps away
            Done();
        }

        if(TimeAlive > OldTimeAlive)
        {
            SetReward(0.2f);
            OldTimeAlive = TimeAlive;
        }

        if(CollidedWithTarget)
        {
            SetReward(1);
        }

        if (CollidedWithGround)
        {
            SetReward(-0.01f);
            Done();
        }

        if (Vector3.Distance(rearWheel.gameObject.transform.position, Target.transform.position) < 1f)
        {
            SetReward(1);
            Done();
        }

        if (Vector3.Distance(frontWheel.gameObject.transform.position, Target.transform.position) < 1f)
        {
            SetReward(1);
            Done();
        }
    }

    public override float[] Heuristic()
    {
        float[] action = new float[2];
        action[0] = Input.GetAxis("Vertical");
        action[1] = Input.GetAxis("Horizontal");

        return action;
    }

    private void OnCollisionEnter(Collision collision)
    {
        CollidedWithGround |= collision.gameObject.CompareTag("Ground");

    }

    private void OnTriggerEnter(Collider other)
    {
        CollidedWithTarget |= other.gameObject.CompareTag("Target");
    }


}
