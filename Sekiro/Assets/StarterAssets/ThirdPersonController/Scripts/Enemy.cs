using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;

// Enemy states
public enum EnemyState
{
    MoveTowards,
    MoveAway,
    Attack,
    Parry,
    Idle,
    Dead
}

public class Enemy : MonoBehaviour
{

    public int health;
    public int damage;
    protected int maxHealth;
    EnemyState state;
    public GameObject player;
    protected NavMeshAgent agent;
    public Animator anim;

    // Enemy Field of View
    public Transform target;
    public float radius;
    [Range(0, 360)]
    public float angle;
    public LayerMask obstructionMask;
    public LayerMask playerMask;
    public bool canSeePlayer;

    // Attacking
    public float timeBetweenAttacks;
    bool alreadyAttacked;
    public GameObject weapon;

    // Audio
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        health = 100;
        damage = 10;
        maxHealth = 100;
        state = EnemyState.Idle;
        target = player.transform;
        agent = GetComponent<NavMeshAgent>();

        timeBetweenAttacks = 2.0f;

        if (GetComponent<Animator>() != null )
            anim = GetComponent<Animator>();

        // Makes the field of view not run all the time to help with performance
        StartCoroutine(FOVRoutine());
    }

    void FixedUpdate()
    {
        // Function for all the enemy actions
        EnemyAI();
    }

    protected virtual void EnemyAI()
    {
        anim.SetFloat("Speed", agent.speed);

        // Sets the enemy to die
        if (health <= 0)
            state = EnemyState.Dead;

        // Depending on the distance of the player and the enemy view distance
        // The enemy will enter a different state
        float distance = Vector3.Distance(target.position, transform.position);
        if(!canSeePlayer && state != EnemyState.Dead)
        {
            state = EnemyState.Idle;
        }

        if (canSeePlayer && state != EnemyState.Dead)
        {
            state = EnemyState.MoveTowards;
        }

        if (distance <= agent.stoppingDistance && state != EnemyState.Dead)
            state = EnemyState.Attack;

        // Switches between enemy states
        switch (state)
        {
            case EnemyState.Idle:
                StopEnemy();
                break;

            case EnemyState.MoveTowards:
                StartEnemy();
                Chase();
                break;

            // Haven't done this yet but this will make it so
            // If the player gets too close to the enemy
            // the enemy moves away from the player to make space
            case EnemyState.MoveAway:
                StartEnemy();
                break;

            case EnemyState.Attack:
                Attack();
                //Parry();
                break;

            case EnemyState.Parry:
                break;

            case EnemyState.Dead:
                StopEnemy();
                break;
        }
    }

    // Waits a few seconds before running the field of view check
    // Makes the performance of the game a bit betters
    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    // Checks the field of view of the enemy
    private void FieldOfViewCheck()
    {
        // Checks for any players in the specific view radius
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, playerMask);

        // If there is a player in the radius
        if (rangeChecks.Length != 0)
        {
            Transform currentTarget = rangeChecks[0].transform;
            Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;

            // Checks if the player is in the specific view triangle in front of the enemy
            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

                // If it is and nothing is blocking the view then the enemy can see the player
                if (!Physics.Raycast(transform.position, directionToTarget, 1f, obstructionMask))
                    canSeePlayer = true;
                else
                    canSeePlayer = false;
            }
            else
                canSeePlayer = false;
        }
        else if (canSeePlayer)
            canSeePlayer = false;
    }

    // Chases the player
    private void Chase()
    {
        agent.speed = 2;
        agent.SetDestination(target.position);
    }

    // Okay I am not happy with how this works right now so I am
    // Going to come back and fix it later
    protected void CheckHit()
    {
        CapsuleCollider hitbox = weapon.GetComponent<CapsuleCollider>();

        Vector3 direction = new Vector3 { [hitbox.direction] = 1 };
        float offset = hitbox.height / 2 - hitbox.radius;
        Vector3 weaponStart = weapon.transform.TransformPoint(hitbox.center - direction * offset);
        Vector3 weaponEnd = weapon.transform.TransformPoint(hitbox.center + direction * offset);

        Collider[] hits = Physics.OverlapCapsule(weaponStart, weaponEnd, hitbox.radius);

        if (hits.Length <= 0)
            return;
        foreach (Collider hit in hits)
        {
            if (hit.tag == "Player")
            {
                Debug.Log("hit");
                CancelInvoke(nameof(CheckHit));
            }
        }
    }

    // Attack the player
    private void Attack()
    {
        agent.SetDestination(transform.position);
        agent.speed = 0;

        transform.LookAt(target);

        Vector3 lookAngle = transform.rotation.eulerAngles;
        lookAngle.x = 0;

        transform.rotation = Quaternion.Euler(lookAngle);

        if (!alreadyAttacked)
        {
            // Attack code
            anim.SetBool("Attack", true);

            // Checks if the enemy has hit the player
            InvokeRepeating(nameof(CheckHit), 1.5f, 0.1f);

            // If so then the enemy attack is reset
            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }

    }

    private void Parry()
    {
        agent.SetDestination(transform.position);
        agent.speed = 0;

        transform.LookAt(target);

        Vector3 lookAngle = transform.rotation.eulerAngles;
        lookAngle.x = 0;

        transform.rotation = Quaternion.Euler(lookAngle);

        anim.SetBool("Parry", true);
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
        anim.SetBool("Attack", false);
        CancelInvoke(nameof(CheckHit));
        //Debug.Log("attack finish");
    }

    // Stops the Enemy completely 
    // and sets up the idle animation
    public void StopEnemy()
    {
        agent.isStopped = true;
        agent.speed = 0;
        anim.SetFloat("Speed", 0);
    }

    // Starts the Enemy back up with a given speed
    // and sets the animation according to the speed set
    private void StartEnemy()
    {
        agent.isStopped = false;
        agent.speed = 2;
        anim.SetFloat("Speed", 2);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(transform.position), FootstepAudioVolume);
            }
        }
    }
}
