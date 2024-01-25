using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Enemy states
public enum EnemyState
{
    MoveTowards,
    MoveAway,
    Attack,
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

    // Enemy Field of View
    public Transform target;
    public float radius;
    [Range(0, 360)]
    public float angle;
    public LayerMask obstructionMask;
    public LayerMask playerMask;
    public bool canSeePlayer;

    // Start is called before the first frame update
    void Start()
    {
        health = 100;
        damage = 10;
        maxHealth = 100;
        state = EnemyState.Idle;
        target = player.transform;
    }

    void FixedUpdate()
    {
        // Function for all the enemy actions
        EnemyAI();
    }

    protected virtual void EnemyAI()
    {
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
        else
        {
            state = EnemyState.MoveTowards;
        }

        // Switches between enemy states
        switch (state)
        {
            case EnemyState.Idle:
                break;

            case EnemyState.MoveTowards:
                break;

            case EnemyState.MoveAway:
                break;

            case EnemyState.Attack:
                break;

            case EnemyState.Dead:
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

    private void Chase()
    {
        agent.SetDestination(target.position);
    }
}
