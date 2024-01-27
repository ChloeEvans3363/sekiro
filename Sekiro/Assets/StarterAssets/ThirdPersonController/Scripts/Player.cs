using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    //General Stats
    public int health;
    public int damage;
    protected int maxHealth;
    public int stamina;
    protected int maxStamina;

    //Attacking
    public Enemy enemy;
    public Animator anim;
    public GameObject weapon;
    private float maxAttackCooldown;
    private float attackCooldown;
    public bool isAttacking;
    private float maxParryCooldown;
    private float parryCooldown;
    public bool isParrying;
    public bool isBlocking;

    //UI
    public Slider healthBarSlider;
    public Image uiImage;


    // Start is called before the first frame update
    void Start()
    {
        health = maxHealth = 100;
        stamina = maxStamina = 100;
        maxAttackCooldown = maxParryCooldown = 1;
        attackCooldown = parryCooldown = 0;
        isAttacking = isParrying = isBlocking = false;

        if (GetComponent<Animator>() != null)
            anim = GetComponent<Animator>();

        //UI
        healthBarSlider.maxValue = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        float deltaTime = Time.deltaTime;
        UpdateInput();
        UpdateUI();

        if (health <= 0) { return; }

        attackCooldown -= deltaTime;
        parryCooldown -= deltaTime;

        if(attackCooldown <= 0)
        {
            anim.SetBool("Attack", false);
            isAttacking = false;
        }
        else if(isAttacking == true) { CheckHit(); }

        if(parryCooldown <= 0)
        {
            Block();
        }

    }

    //Check for input
    private void UpdateInput()
    {
        //Left Click
        if (attackCooldown <= 0 && Input.GetMouseButtonDown(0))
        {
            Attack();
        }
        //Right Click
        if (Input.GetMouseButtonDown(1))
        {
            Parry();
        }
    }

    private void Attack()
    {
        attackCooldown = maxAttackCooldown;
        anim.SetBool("Attack", true);
        isAttacking = true;
    }

    private void Parry()
    {
        parryCooldown = maxParryCooldown;
        isParrying = true;
        anim.SetBool("Parry", true);
    }

    private void Block()
    {
        //Right click is down
        if (Input.GetMouseButton(1))
        {
            isBlocking = true;
        }
        else
        {
            isBlocking = isParrying = false;
            anim.SetBool("Parry", false);
        }

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
                isAttacking = false;
                enemy.health -= damage;
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
    }

    //To be called by enemy when hitting the player
    public bool CheckGetHit(int damage)
    {
        if (isBlocking) { health -= damage / 2; return true; }
        if (isParrying) { return false; }
        health -= damage;
        //Debug.Log("took " + damage + " damage: " + health + " health remains");
        return true;
    }


}
