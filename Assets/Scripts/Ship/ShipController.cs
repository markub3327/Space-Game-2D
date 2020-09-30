using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class ShipController : MonoBehaviour
{
    // Engine systems
    public ParticleSystem[] Motors;             // animacia plamenov motora
    public AudioClip engineClip;                // zvukovy klip motorov
    public const float maxSpeed = 10.0f;              // max rychlost lode
    public const float maxTorque = 100.0f;            // max rychlost rotacie lode
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
    public const int maxAmmo = 30;               // maximalny pocet nabojov hraca
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

    // Zoznam planet, ktore vlastni lod
    public List<PlanetController> myPlanets { get; protected set; } = new List<PlanetController>();

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
    public float score = 0;
    protected float reward;

    // Lod
    public Text nameBox;
    public Text levelBox;
    public TurretController[] turretControllers;

    // Premenne herneho prostredia
    public int episode { get; set; } = 0;
    public int step = 0;
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

        // Init Player info panel
        this.nameBox.text = this.Nickname;
        this.levelBox.text = "0";
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

            ChangeFuel(-Time.deltaTime * 0.25f);
        }        
    }

    protected void DestroyShip()
    {
        IsDestroyed = true;
        collider2d.enabled = false;

        // Znicena lod straca vlastnictvo u planety
        foreach (var p in this.myPlanets)
        {
            p.OwnerPlanet = null;
        }

        animator.SetBool("Respawn", false);
        animator.SetBool("Destroyed", true);
    }

    protected void WinnerShip()
    {
        IsDestroyed = true;
        collider2d.enabled = false;

        // Znicena lod straca vlastnictvo u planety
        foreach (var p in this.myPlanets)
        {
            p.OwnerPlanet = null;
        }

        animator.SetBool("Respawn", false);
        animator.SetBool("Winner", true);
    }

    public void RespawnShip()
    {
        // obnov zivoty, palivo a municiu lode na maximum
        this.Health = maxHealth;
        this.Ammo = maxAmmo;
        this.Fuel = maxFuel;

        rigidbody2d.position = respawnPoint;  //Respawn.getPoint();
        IsDestroyed = false;
        collider2d.enabled = true;
        IsRespawned = true;
        this.score = 0;

        // vycisti zoznam planet
        this.myPlanets.Clear();

        animator.SetBool("Winner", false);
        animator.SetBool("Destroyed", false);
        animator.SetBool("Respawn", true);
    }

    /// <summary>
    /// Zmeni stav municie hraca
    /// </summary>
    /// <param name="amount">Mnozstvo municie, ktore sa pripocita k sucasnemu stavu municie</param>
    protected void ChangeAmmo(float amount)
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
    protected void ChangeFuel(float amount)
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
    protected void ChangeHealth(float amount)
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
    protected void PlaySound(AudioClip clip)
    {
        if (!IsDestroyed)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    protected void Fire()
    {
        if (this.Ammo > 0)
        {       
            // Prehra zvuk vystrelu
            this.PlaySound(throwClip);
                     
            foreach (var turret in this.turretControllers)
            {
                // Vystrel
                turret.Fire();
                // Uber z municie hraca jeden naboj
                this.ChangeAmmo(-1.0f);
                // Ak uz hrac nema municiu nemoze pokracovat v strelbe
                if (this.Ammo <= 0)
                    break;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Podla oznacenia herneho objektu vykonaj akciu kolizie
        switch (collision.gameObject.tag)
        {
            case "Star":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);
                this.reward = -0.75f;
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;

        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Podla oznacenia herneho objektu vykonaj akciu kolizie
        switch (collision.gameObject.tag)
        {
            case "Star":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);
                this.reward = -0.75f;
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;
            case "Ammo":
                if (this.Ammo < ShipController.maxAmmo)
                {
                    // Pridaj municiu hracovi
                    this.ChangeAmmo(+10.00f);
                    this.reward = 0.10f;
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                Destroy(collision.gameObject);
                break;
            case "Health":
                if (this.Health < ShipController.maxHealth)
                {
                    // Pridaj zivot hracovi
                    this.ChangeHealth(+1.0f);
                    this.reward = 0.10f;
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                Destroy(collision.gameObject);
                break;
            case "Nebula":
                if (this.Fuel < ShipController.maxFuel)
                {
                    // Pridaj zivot hracovi
                    this.ChangeFuel(+ShipController.maxFuel);
                    this.reward = 0.10f;
                    // Prehraj klip
                    this.PlaySound(collectibleClip);
                }
                collision.gameObject.GetComponent<NebulaController>().Delete();
                break;
            case "Asteroid":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);      
                this.reward = -0.10f;
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;
            case "Planet":
                {
                    var planet = collision.gameObject.GetComponent<PlanetController>();
                    if (planet.OwnerPlanet == null)
                    {
                        planet.OwnerPlanet = this.gameObject;
                        this.myPlanets.Add(planet);
                        this.reward = 1.0f;

                        planet.dialogBox.Clear();
                        planet.dialogBox.WriteLine($"Planet: {planet.name}");
                        planet.dialogBox.WriteLine($"Owner: {this.Nickname}");
                    }
                }
                break;
            case "Player":
                // Uberie sa hracovi zivot
                this.ChangeHealth(-1.0f);
                this.reward = -0.10f;
                // Prahra clip poskodenia lode
                this.PlaySound(damageClip);
                break;
            case "Space":
                this.reward = -1.0f;
                break;
            //default:
                //Debug.Log($"collision_tag: {collision.gameObject.tag}");
                //break;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        // Podla oznacenia herneho objektu vykonaj akciu kolizie
        switch (collider.gameObject.tag)
        {
            case "Projectile":
                var firingShip = collider.gameObject.GetComponent<Projectile>().firingShip.gameObject;
                if (firingShip != this.gameObject)
                {
                    // Prehra zvuk zasahu projektilu
                    this.PlaySound(this.hitClip);
                    // Znizi zivoty hracovi
                    this.ChangeHealth(-0.50f);
                    this.reward = -0.10f;
                }
                break;
            default:
                Debug.Log($"collider_tag: {collider.gameObject.tag}");
                break;
        }
    }
}