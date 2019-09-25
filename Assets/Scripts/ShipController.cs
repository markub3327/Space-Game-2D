using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    // Engine systems
    public ParticleSystem[] Motors;     // animacia plamenov motora
    public AudioClip engineClip;        // zvukovy klip motorov
    public float maxSpeed = 5.0f;       // max rychlost lode
    public float maxTorque = 100.0f;     // max rychlost rotacie lode

    // Player's health
    public int maxHealth = 10;              // maximalny pocet zivotov hraca
    public float timeInvincible = 2.0f;     // casova medzera pri preniknuti do nebezpecnej zony
    public int Health { get; private set; } // aktualny stav hracovych zivotov
    private bool isInvincible;              // je nesmrtelny
    private float invincibleTimer;          // casovac uchovavajuci cas do skoncenia nesmrtelnosti

    // Player's ammo
    public int maxAmmo = 100;               // maximalny pocet nabojov hraca
    public int Ammo { get; private set; }   // aktualny stav hracovej municie 

    // Player's fuel
    public int maxFuel = 10;                // maximalny pocet paliva v nadrzi hraca
    public int Fuel { get; private set; }   // aktualny stav paliva
    private bool isEmptyFuel;
    private float fuelTimer;                // casovac uchovavajuci cas do minutia jednej nadrze paliva

    // Dynamika herneho objektu
    private Rigidbody2D rigidbody2d;

    // Reproduktor lode
    AudioSource audioSource;
    public bool IsPlayingClip
    {
        get
        {
            return audioSource.isPlaying;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        Health = maxHealth;
        Ammo = maxAmmo;
        Fuel = maxFuel;
    }

    // Update is called once per frame
    void Update()
    {
        // Nacitaj zmenu polohy z klavesnice alebo joysticku
        float axisH = Input.GetAxis("Horizontal");
        float axisV = Input.GetAxis("Vertical");
        Vector2 move = new Vector2(axisH, axisV).normalized;

        // Ak je lod v pohybe zapni motory
        if (Mathf.Abs(move.magnitude) > 0.0f)
        {
            // Pohni s lodou podla vstupu od uzivatela
            MoveShip(move);
        }

        // Ak je hrac "neporazitelny"
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer < 0)
                isInvincible = false;
        }
    }
   
    /// <summary>
    /// Pohyb s lodou
    /// </summary>
    /// <param name="move">Normalizovany vektor udavajuci smer a velkost pohybu</param>
    private void MoveShip(Vector2 move)
    {
        if (this.Fuel > 0)
        {
            // Vypocitaj zmenu rotacie hraca
            var position = rigidbody2d.position;
            var rotation = rigidbody2d.rotation;
            var deltaPos = maxSpeed * move.y * Time.deltaTime;

            // Vypocet prirastku zmeny pozicie a rotacie lode
            rotation += maxTorque * (-move.x) * Time.deltaTime;
            var rotationRad = rotation * Mathf.Deg2Rad;
            position += new Vector2((Mathf.Cos(rotationRad) * deltaPos), (Mathf.Sin(rotationRad) * deltaPos));

            // Pre vsetky motory lode
            foreach (var motor in Motors)
            {
                // Ak nie je uz spusteny zapni ho
                if (!motor.isEmitting)
                {
                    // Nastav dlzku plamena podla rychlosti lode
                    var motorShape = motor.shape;
                    motorShape.length = Mathf.Clamp((Mathf.Abs(move.y) * 0.4f), 0.0f, 0.4f);

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
                if (fuelTimer < 0)
                    isEmptyFuel = true;
            }

            // Zmen pohyb a rotaciu lode
            rigidbody2d.rotation = rotation;
            rigidbody2d.MovePosition(position);
        }
    }

    /// <summary>
    /// Zmeni stav municie hraca
    /// </summary>
    /// <param name="amount">Mnozstvo municie, ktore sa pripocita k sucasnemu stavu municie</param>
    public void ChangeAmmo(int amount)
    {
        Ammo = Mathf.Clamp(Ammo + amount, 0, maxAmmo);

        // Vykonaj zmenu na UI bare zobrazujuceho zivot hraca
        //UIAmmoBar.instance.SetValue(ammo / (float)maxAmmo);
        Debug.Log($"Player: {this.name} has ammo {this.Ammo}/{this.maxAmmo}");
    }

    /// <summary>
    /// Zmeni stav municie hraca
    /// </summary>
    /// <param name="amount">Mnozstvo municie, ktore sa pripocita k sucasnemu stavu municie</param>
    public void ChangeFuel(int amount)
    {
        Fuel = Mathf.Clamp(Fuel + amount, 0, maxFuel);

        // Vykonaj zmenu na UI bare zobrazujuceho zivot hraca
        //UIAmmoBar.instance.SetValue(ammo / (float)maxAmmo);
        Debug.Log($"Player: {this.name} has fuel {this.Fuel}/{this.maxFuel}");
    }

    /// <summary>
    /// Zmeni stav zivota hraca
    /// </summary>
    /// <param name="amount">Mnozstvo zivota, ktore sa pripocita k sucasnemu stavu zivota</param>
    public void ChangeHealth(int amount)
    {
        // Pri uberani zivota
        if (amount < 0)
        {
            // Ak je hrac "neporazitelny" vyskoc z funkcie
            if (isInvincible)
                return;

            isInvincible = true;
            invincibleTimer = timeInvincible;
        }
        Health = Mathf.Clamp(Health + amount, 0, maxHealth);

        // Vykonaj zmenu na UI bare zobrazujuceho zivot hraca
        UIHealthBar.instance.SetValue(Health / (float)maxHealth);
    }

    /// <summary>
    /// Prehra zvukovy klip
    /// </summary>
    /// <param name="clip">Zvukovy klip</param>
    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
}