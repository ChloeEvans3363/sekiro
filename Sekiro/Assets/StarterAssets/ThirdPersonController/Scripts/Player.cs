using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum PlayerState
{
    parrying,
    dodging,
    blocking,
    attacking,
    stunned,
    idle
}

public class Player : MonoBehaviour
{
    //General Stats
    public int health;
    public int damage;
    protected int maxHealth;
    public float posture = 100;
    private float maxPosture = 100;
    private float maxPostureCooldown = 3f;
    private float postureCooldown = 0;
    //public int stamina;
    //protected int maxStamina;

    //Attacking
    public Enemy enemy;
    public Animator anim;
    public GameObject weapon;
    private float maxAttackCooldown;
    private float attackCooldown;
    private float maxParryCooldown;
    private float parryCooldown;
    public PlayerState state = PlayerState.idle;

    //Dodging
    public float dodgeDuration = 0.5f;
    public float maxDodgeCooldown = 1;
    private float dodgeCooldown = 0;
    //public float dodgeSpeed = 3;

    //Stun Duration
    public float maxStunDuration = 3;
    public float stunDuration = 0;


    //UI
    public Slider healthBarSlider;
    public Slider postureBarSlider;
    public Slider enemyPostureBarSlider;
    public Image uiImage;


    // Start is called before the first frame update
    void Start()
    {
        health = maxHealth = 1000;
        //stamina = maxStamina = 100;
        maxAttackCooldown = maxParryCooldown = 1;
        attackCooldown = parryCooldown = 0;

        if (GetComponent<Animator>() != null)
            anim = GetComponent<Animator>();

        //UI
        healthBarSlider.maxValue = maxHealth;
        postureBarSlider.maxValue = maxPosture;
        if(enemyPostureBarSlider && enemy)
        enemyPostureBarSlider.maxValue = enemy.maxPosture;
    }

    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.deltaTime;
        UpdateInput();
        UpdateUI();

        //Debug.Log(state);

        if (health <= 0) { return; }
        if (stunDuration > 0)
        {
            //Debug.Log("Stunned!");
            state = PlayerState.stunned;
            stunDuration -= deltaTime;
            if (stunDuration <= 0)
            {
                anim.SetBool("Dodge", false);
                state = PlayerState.idle;
                posture = maxPosture / 1.5f;
            }
            return;
        }

        attackCooldown -= deltaTime;
        parryCooldown -= deltaTime;
        dodgeCooldown -= deltaTime;
        postureCooldown -= deltaTime;

        if(attackCooldown <= 0)
        {
            anim.SetBool("Attack", false);
            if (state == PlayerState.attacking) { state = PlayerState.idle; }
        }
        else if(state == PlayerState.attacking) { CheckHit(); }

        if(parryCooldown <= 0 && (state == PlayerState.parrying || state == PlayerState.blocking))
        {
            Block();
        }

        if (postureCooldown <= 0 && posture < maxPosture && posture > 0)
        {
            posture += deltaTime*2;
        }

    }

    //Check for input
    private void UpdateInput()
    {
        //Left Click - Attack
        if (attackCooldown <= 0 && Input.GetMouseButtonDown(0))
        {
            Attack();
        }
        //Right Click - Parry
        if (Input.GetMouseButtonDown(1))
        {
            Parry();
        }
        //Left Ctrl - Dodge
        if (dodgeCooldown <= 0 && Input.GetKey(KeyCode.LeftControl))
        {
            Dodge();
        }
    }

    private void Attack()
    {
        attackCooldown = maxAttackCooldown;
        anim.SetBool("Attack", true);
        state = PlayerState.attacking;
    }

    private void Parry()
    {
        parryCooldown = maxParryCooldown;
        state = PlayerState.parrying;
        anim.SetBool("Parry", true);
    }

    private void Block()
    {
        //Right click is down
        if (Input.GetMouseButton(1))
        {
            state = PlayerState.blocking;
        }
        else
        {
            state = PlayerState.idle;
            anim.SetBool("Parry", false);
        }

    }

    private void Dodge()
    {
        postureCooldown = maxPostureCooldown;
        state = PlayerState.dodging;
        dodgeCooldown = maxDodgeCooldown;
        anim.SetBool("Dodge", true);
        Invoke(nameof(StopDodge), dodgeDuration);
    }

    private void StopDodge()
    {
        state = PlayerState.idle;
        dodgeCooldown = maxDodgeCooldown;
        anim.SetBool("Dodge", false);
    }


    // Based off the enemy's CheckHit
    private void CheckHit()
    {
        //Check Collision
        CapsuleCollider hitbox = weapon.GetComponent<CapsuleCollider>();

        Vector3 direction = new Vector3 { [hitbox.direction] = 1 };
        float offset = hitbox.height / 2 - hitbox.radius;
        Vector3 weaponStart = weapon.transform.TransformPoint(hitbox.center - direction * offset);
        Vector3 weaponEnd = weapon.transform.TransformPoint(hitbox.center + direction * offset);

        Collider[] hits = Physics.OverlapCapsule(weaponStart, weaponEnd, hitbox.radius);

        if (hits.Length <= 0)
            return;
        // Enemy doesn't have a collider, only the sword does
        foreach (Collider hit in hits)
        {
            if (hit.tag == "Player")
            {
                Debug.Log("Enemy Hit: "+hit);
                anim.SetBool("Attack", false);
                state = PlayerState.idle;
                enemy.health -= damage;
                enemy.posture -= damage;
                enemy.state = EnemyState.Damage;
            }
        }
    }

    //Updates UI
    private void UpdateUI()
    {
        healthBarSlider.value = health;
        if(health <= 0) { 
            healthBarSlider.value = 0;
            uiImage.color = new Color(79f/256f, 79f/256f, 79f/256f, 190f/256f);
        }
        if(postureBarSlider)
        postureBarSlider.value = posture;
        if(enemyPostureBarSlider && enemy)
        enemyPostureBarSlider.value = enemy.posture;
    }

    //To be called by enemy when hitting the player
    public PlayerState CheckGetHit(int damage)
    {
        switch (state)
        {
            case PlayerState.parrying:
            case PlayerState.dodging:
                Debug.Log("Player avoided damage: " + state);
                break;
            case PlayerState.blocking:
                health -= damage / 2;
                posture -= damage;
                break;
            case PlayerState.attacking:
            case PlayerState.idle:
                health -= damage;
                posture -= (float)damage * 1.5f;
                anim.Play("Damage");
                break;
            case PlayerState.stunned:
                health -= damage;
                anim.Play("Damage");
                break;
            default:
                break;
        }
        postureCooldown = maxPostureCooldown;
        if(posture <= 0 && state != PlayerState.stunned) { stunDuration = maxStunDuration; }
        //Debug.Log("took " + damage + " damage: " + health + " health remains");
        return state;
    }


}
