using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    public static Vector2[] respawnPoints = 
    { 
        new Vector2(13, -5),
        new Vector2(5, -10),
        new Vector2(-9, 2),
        new Vector2(-6, -12),
        new Vector2(11, 0.5f),
        new Vector2(-11, -1),
    };

    // Engine systems
    public ParticleSystem[] Motors;             // animacia plamenov motora
    public AudioClip engineClip;                // zvukovy klip motorov
    public float maxSpeed = 5.0f;               // max rychlost lode
    public float maxTorque = 100.0f;            // max rychlost rotacie lode
    public Vector2 LookDirection { get; private set; }  = Vector2.up; // Smer predu lode

    // Player's health
    public UIBarControl healthBar;
    public const int maxHealth = 10;              // maximalny pocet zivotov hraca
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
    public const int maxAmmo = 100;               // maximalny pocet nabojov hraca
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
    public const int maxFuel = 10;                // maximalny pocet paliva v nadrzi hraca
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

    // Zoznam planet, ktore vlastni lod
    public List<PlanetController> myPlanets;

    // Respawn
    public float timeRespawn = 25.0f;       // 10 sekund do obnovenia lode v bode respawnu
    public bool IsDestroyed { get; private set; } = false;  // stav lode, je lod znicena?
    protected float respawnTimer;   // casovac pre obnovu lode

    // Dynamika herneho objektu
    protected Rigidbody2D rigidbody2d;

    // Reproduktor lode
    private AudioSource audioSource;

    // Animacie lode
    protected Animator animator;

    // Collider
    private PolygonCollider2D collider2d;

    // Respawn
    private GameObject respawn;

    protected Unity.Mathematics.Random randGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);


    // Start is called before the first frame update
    public virtual void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        collider2d = GetComponent<PolygonCollider2D>();
        respawn = GameObject.FindGameObjectWithTag("Respawn");

        var rot = this.transform.rotation;
    }

    public virtual void Update()
    {
        // Ak je lod znicena
        if (IsDestroyed)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer < 0.0f)
            {
                RespawnShip();
                animator.SetTrigger("Respawn");
            }
        }
        else
        {
            // Ak uz hrac nema zivoty znici sa lod
            if (this.Health <= 0)
            {
                DestroyShip();
            }
        }
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
        respawnTimer = timeRespawn;

        // Znicena lod straca vlastnictvo u planety
        foreach (var planet in myPlanets)
        {
            planet.OwnerPlanet = null;
        }
        myPlanets.Clear();  // Vycisti zoznam vlastnenych planet

        animator.SetTrigger("Destroyed");
    }

    protected void WinnerShip()
    {
        IsDestroyed = true;
        respawnTimer = 1f;

        animator.SetTrigger("Winner");
    }

    protected void RespawnShip()
    {
        // obnov zivoty, palivo a municiu lode na maximum
        Health = maxHealth;
        Ammo = maxAmmo;
        Fuel = maxFuel;

        var rotation = randGen.NextInt(0, 360);
        var rotationRad = (rotation + 90.0f) * Mathf.Deg2Rad;
        this.LookDirection = new Vector2(Mathf.Cos(rotationRad), Mathf.Sin(rotationRad));

        rigidbody2d.position = respawnPoints[randGen.NextInt(0, respawnPoints.Length)];        
        rigidbody2d.rotation = rotation;
        IsDestroyed = false;
        collider2d.enabled = true;

        animator.SetTrigger("Respawn");
    }

    /// <summary>
    /// Zmeni stav municie hraca
    /// </summary>
    /// <param name="amount">Mnozstvo municie, ktore sa pripocita k sucasnemu stavu municie</param>
    public void ChangeAmmo(int amount)
    {
        if (!IsDestroyed)
        {
            Ammo = Mathf.Clamp(Ammo + amount, 0, maxAmmo);
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
            Fuel = Mathf.Clamp(Fuel + amount, 0f, maxFuel);
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
            Health = Mathf.Clamp(Health + amount, 0, maxHealth);
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