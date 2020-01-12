﻿using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShipController : MonoBehaviour
{
    // Engine systems
    public ParticleSystem[] Motors;             // animacia plamenov motora
    public AudioClip engineClip;                // zvukovy klip motorov
    public float maxSpeed = 5.0f;               // max rychlost lode
    public float maxTorque = 100.0f;            // max rychlost rotacie lode
    public Vector2 LookDirection { get; private set; }  = Vector2.up; // Smer predu lode

    // Player's health
    public UIBarControl healthBar;
    public const int maxHealth = 5;              // maximalny pocet zivotov hraca
    private int _health = maxHealth;
    public int Health {
        get
        {
            return _health;
        }
        private set
        {
            // Zapis novu hodnotu
            _health = value;

            // Vykonaj zmenu na UI bare zobrazujuceho stav municie hraca ak je pripojeny
            if (healthBar != null)
                healthBar.SetValue(_health / (float)maxHealth);
        }
    }

    // Player's ammo
    public UIBarControl ammoBar;
    public const int maxAmmo = 200;               // maximalny pocet nabojov hraca
    private int _ammo = maxAmmo;
    public int Ammo {
        get
        {
            return _ammo;
        }
        private set
        {
            // Zapis novu hodnotu
            _ammo = value;

            // Vykonaj zmenu na UI bare zobrazujuceho stav municie hraca ak je pripojeny
            if (ammoBar != null)
                ammoBar.SetValue(_ammo / (float)maxAmmo);
        }
    }   // aktualny stav hracovej municie 

    // Player's fuel
    public UIBarControl fuelBar;
    public const int maxFuel = 5;                // maximalny pocet paliva v nadrzi hraca
    private float _fuel = maxFuel;
    public float Fuel {
        get
        {
            return _fuel;
        }
        private set
        {
            // Zapis novu hodnotu
            _fuel = value;

            // Vykonaj zmenu na UI bare zobrazujuceho stav municie hraca ak je pripojeny
            if (fuelBar != null)
              fuelBar.SetValue(_fuel / (float)maxFuel);
        }
    }

    // Stavy t-1 (pre ziskanie odmeny za tah v hre)
    //protected int Health_old = maxHealth;
    //protected float Fuel_old = maxFuel;
    //protected int Ammo_old = maxAmmo;

    // Zoznam planet, ktore vlastni lod
    public List<PlanetController> myPlanets;
    public bool hasNewPlanet = false;

    // Respawn
    public bool IsDestroyed { get; protected set; } = false;  // stav lode, je lod znicena?

    // Dynamika herneho objektu
    protected Rigidbody2D rigidbody2d;

    // Reproduktor lode
    private AudioSource audioSource;

    // Animacie lode
    protected Animator animator;

    // Collider
    private PolygonCollider2D collider2d;

    // Start is called before the first frame update
    public virtual void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        collider2d = GetComponent<PolygonCollider2D>();
    }

    /// <summary>
    /// Pohyb s lodou
    /// </summary>
    /// <param name="move">Normalizovany vektor udavajuci smer a velkost pohybu</param>
    protected void MoveShip(Vector2 move)
    {
        if (!IsDestroyed && this.Fuel > 0f)
        {
            // Vypocitaj zmenu rychlosti rotacie a pozicie hraca
            var position = rigidbody2d.position;
            var rotation = rigidbody2d.rotation;
            var velocity = maxSpeed * move.y * Time.deltaTime;

            // Vypocet prirastku zmeny pozicie a rotacie lode
            rotation += maxTorque * (-move.x) * Time.deltaTime;
            var rotationRad = (rotation + 90.0f) * Mathf.Deg2Rad;
            this.LookDirection = new Vector2(Mathf.Cos(rotationRad), Mathf.Sin(rotationRad)); // normalizovany vektor smeru pohybu lode
            position += LookDirection * velocity;

            // Zmen pohyb a rotaciu lode
            rigidbody2d.rotation = rotation;
            rigidbody2d.MovePosition(position);
            
            // Pre vsetky motory lode
            foreach (var motor in Motors)
            {
                // Ak nie je uz spusteny zapni ho
                if (!motor.isEmitting)
                {
                    // Prehraj animaciu
                    motor.Play();

                    // Prehraj zvuk motora ak nehra
                    if (!audioSource.isPlaying)
                        PlaySound(engineClip);
                }
            }

            ChangeFuel(-Time.deltaTime);    // plaivo sa znizi o 1 za 1 sekundu
        }        
    }

    protected void DestroyShip()
    {
        IsDestroyed = true;
        collider2d.enabled = false;

        // obnov zivoty, palivo a municiu lode na maximum
        this.Health = maxHealth;
        this.Ammo = maxAmmo;
        this.Fuel = maxFuel;

        // Znicena lod straca vlastnictvo u planety
        foreach (var p in this.myPlanets)
        {
            p.OwnerPlanet = null;
        }
        this.myPlanets.Clear();
        
        animator.SetBool("Respawn", false);
        animator.SetBool("Destroyed", true);
    }

    protected void WinnerShip()
    {
        IsDestroyed = true;
        collider2d.enabled = false;

        animator.SetBool("Respawn", false);
        animator.SetBool("Winner", true);
    }

    protected void RespawnShip()
    {
        rigidbody2d.position = Respawn.getPoint();        
        rigidbody2d.rotation = 0f;
        IsDestroyed = false;
        collider2d.enabled = true;

        animator.SetBool("Winner", false);
        animator.SetBool("Destroyed", false);
        animator.SetBool("Respawn", true);
    }

    /// <summary>
    /// Zmeni stav municie hraca
    /// </summary>
    /// <param name="amount">Mnozstvo municie, ktore sa pripocita k sucasnemu stavu municie</param>
    public void ChangeAmmo(int amount)
    {
        if (!IsDestroyed)
        {
            this.Ammo = Mathf.Clamp(Ammo + amount, 0, maxAmmo);
        }
    }

    /// <summary>
    /// Zmeni stav municie hraca
    /// </summary>
    /// <param name="amount">Mnozstvo municie, ktore sa pripocita k sucasnemu stavu municie</param>
    public void ChangeFuel(float amount)
    {
        if (!IsDestroyed)
        {
            this.Fuel = Mathf.Clamp(Fuel + amount, 0f, maxFuel);
        }
    }

    /// <summary>
    /// Zmeni stav zivota hraca
    /// </summary>
    /// <param name="amount">Mnozstvo zivota, ktore sa pripocita k sucasnemu stavu zivota</param>
    public void ChangeHealth(int amount)
    {
        if (!IsDestroyed)
        {
            this.Health = Mathf.Clamp(Health + amount, 0, maxHealth);
        }
    }

    /// <summary>
    /// Prehra zvukovy klip
    /// </summary>
    /// <param name="clip">Zvukovy klip</param>
    public void PlaySound(AudioClip clip)
    {
        if (!IsDestroyed)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}