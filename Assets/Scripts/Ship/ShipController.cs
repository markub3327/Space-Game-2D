using UnityEngine;

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
    public int maxHealth = 10;              // maximalny pocet zivotov hraca
    private int _health;
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
    public int maxAmmo = 100;               // maximalny pocet nabojov hraca
    private int _ammo;
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
    public int maxFuel = 5;                // maximalny pocet paliva v nadrzi hraca
    private int _fuel;
    public int Fuel {
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
    private bool isEmptyFuel;               // je nadrz prazdna?
    private float fuelTimer;                // casovac uchovavajuci cas do minutia jednej nadrze paliva

    // Pocet planet vo vlastnictve hraca
    public int NumOfPlanets { get; set; }

    // Respawn
    public float timeRespawn = 10.0f;       // 10 seconds do obnovenia lode v bode respawn
    public bool IsDestroyed { get; private set; }   // je lod znicena?
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

    protected Unity.Mathematics.Random randomGen = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);


    // Start is called before the first frame update
    public virtual void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        collider2d = GetComponent<PolygonCollider2D>();
        respawn = GameObject.FindGameObjectWithTag("Respawn");

        NumOfPlanets = 0;

        // Zacina v mieste respawnu
        RespawnShip();
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
        if (!IsDestroyed && this.Fuel > 0)
        {
            // Vypocitaj zmenu rotacie hraca
            var pos = rigidbody2d.position;
            var rot = rigidbody2d.rotation;
            var deltaPos = maxSpeed * move.y * Time.deltaTime;

            // Vypocet prirastku zmeny pozicie a rotacie lode
            rot += maxTorque * (-move.x) * Time.deltaTime;
            var rotationRad = (rot + 90.0f) * Mathf.Deg2Rad;
            LookDirection = new Vector2(Mathf.Cos(rotationRad), Mathf.Sin(rotationRad)); // normalizovany vektor smeru pohybu lode
            pos += LookDirection * deltaPos;

            // Zmen pohyb a rotaciu lode
            rigidbody2d.rotation = rot;
            rigidbody2d.MovePosition(pos);

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

            // Ak je nadrz prazdna pouzi dalsiu
            if (isEmptyFuel)
            {
                ChangeFuel(-1);

                isEmptyFuel = false;
                fuelTimer = 5;
            }
            // Ak ma lod v nadrzi palivo
            else
            {
                fuelTimer -= Time.deltaTime;
                if (fuelTimer < 0.0f)   // ak vyprsal cas minul nadrz (je prazdna)
                    isEmptyFuel = true;
            }
        }        
    }

    protected void RespawnShip()
    {
        // obnov zivoty, palivo a municiu lode na maximum
        Health = maxHealth;
        Ammo = maxAmmo;
        Fuel = maxFuel;

        // Nastav lod do bodu respawn
        Vector2 point = randomGen.NextFloat2(-9f, 9f);
        while ((point.x > -8.1f && point.x < 8.1f) || (point.y > -8.3f && point.y < 8.2f))
        {
            point = randomGen.NextFloat2(-9f, 9f);
        }

        rigidbody2d.position = point; //respawn.transform.position;
        rigidbody2d.rotation = randomGen.NextFloat(0f, 360f); //respawn.transform.rotation.eulerAngles.z;
        IsDestroyed = false;
        collider2d.enabled = true;
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
    public void ChangeFuel(int amount)
    {
        if (!IsDestroyed)
        {
            Fuel = Mathf.Clamp(Fuel + amount, 0, maxFuel);

            if (this.Fuel <= 0)
            {
                this.ChangeHealth(-this.Health);
            }
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

    protected void DestroyShip()
    {
        Debug.Log($"Lod bude znicena!");

        IsDestroyed = true;
        collider2d.enabled = false;
        respawnTimer = timeRespawn;

        animator.SetTrigger("Destroyed");
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