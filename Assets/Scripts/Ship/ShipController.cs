using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShipController : MonoBehaviour
{
    // Engine systems
    public ParticleSystem[] Motors;             // animacia plamenov motora
    public AudioClip engineClip;                // zvukovy klip motorov
    public float maxSpeed = 10.0f;               // max rychlost lode
    public float maxTorque = 100.0f;            // max rychlost rotacie lode
    public Vector2 LookDirection { get; private set; }  = Vector2.up; // Smer predu lode

    // Player's health
    public UIBarControl healthBar;
    public const int maxHealth = 5;              // maximalny pocet zivotov hraca
    private float _health = maxHealth;
    public float Health {
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
    private float _ammo = maxAmmo;
    public float Ammo {
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
    public List<PlanetController> myPlanets { get; set; } = new List<PlanetController>();
    
    // Pocet zasahov ostatnych lodi
    public float Hits { get; set; } = 0;

    // Respawn
    public bool IsDestroyed { get; protected set; } = false;  // stav lode, je lod znicena?

    // Dynamika herneho objektu
    protected Rigidbody2D rigidbody2d;

    // Reproduktor lode
    private AudioSource audioSource;

    // Zvukove klipy
    public AudioClip collectibleClip;
    public AudioClip damageClip;
    public AudioClip hitClip;
    public AudioClip throwClip;
    
    // Animacie lode
    protected Animator animator;

    // Collider
    private PolygonCollider2D collider2d;

    public Vector2 respawnPoint { get; set; }
    public bool IsRespawned { get; set; } = true;

    // Skore v hre
    public float Score 
    {
        get {
            float mean = 0f;

            // Vypocitaj skore hraca
            mean += (this.Health / (float)ShipController.maxHealth) * 0.01f;
            mean += (this.Fuel / (float)ShipController.maxFuel)     * 0.01f;
            mean += (this.Ammo / (float)ShipController.maxAmmo)     * 0.01f;
            mean += (this.Hits / (float)(ShipController.maxHealth*2));
            mean += this.myPlanets.Count;

            return mean;
        }
    }

    public string Nickname;

    // Start is called before the first frame update
    public virtual void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        collider2d = GetComponent<PolygonCollider2D>();

        respawnPoint = this.transform.position;

        // Pre vsetky motory lode
        foreach (var motor in Motors)
        {
            motor.Play();                    
        }
    }

    /// <summary>
    /// Pohyb s lodou
    /// </summary>
    /// <param name="move">Normalizovany vektor udavajuci smer a velkost pohybu</param>
    protected void MoveShip(Vector2 move)
    {
        if (this.Fuel > 0f)
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
            
            // Prehraj zvuk motora ak lod nehra
            if (!audioSource.isPlaying)
                PlaySound(engineClip);

            ChangeFuel(-0.006f);
        }        
    }

    public void DestroyShip()
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
        this.Hits = 0;

        animator.SetBool("Respawn", false);
        animator.SetBool("Destroyed", true);
    }

    public void WinnerShip()
    {
        IsDestroyed = true;
        collider2d.enabled = false;

        animator.SetBool("Respawn", false);
        animator.SetBool("Winner", true);
    }

    public void RespawnShip()
    {
        rigidbody2d.position = respawnPoint;  //Respawn.getPoint();
        IsDestroyed = false;
        collider2d.enabled = true;
        IsRespawned = true;

        animator.SetBool("Winner", false);
        animator.SetBool("Destroyed", false);
        animator.SetBool("Respawn", true);
    }

    /// <summary>
    /// Zmeni stav municie hraca
    /// </summary>
    /// <param name="amount">Mnozstvo municie, ktore sa pripocita k sucasnemu stavu municie</param>
    public void ChangeAmmo(float amount)
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
    public void ChangeHealth(float amount)
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