using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ShipController : MonoBehaviour
{
    // Engine systems
    public ParticleSystem[] Motors;             // animacia plamenov motora
    public AudioClip engineClip;                // zvukovy klip motorov
    public float maxSpeed = 5.0f;               // max rychlost lode
    public float maxTorque = 100.0f;            // max rychlost rotacie lode
    private Vector2 lookDirection = Vector2.up; // Smer predu lode

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

    // Respawn
    public float timeRespawn = 10.0f;       // 10 seconds to respawn
    public bool IsDestroyed { get; private set; }   // aktualny stav lode
    private float respawnTimer;

    // Dynamika herneho objektu
    private Rigidbody2D rigidbody2d;

    // Reproduktor lode
    private AudioSource audioSource;

    // Animacie lode
    private Animator animator;

    // Collider
    private PolygonCollider2D collider2d;

    // Respawn
    private GameObject respawn;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        collider2d = GetComponent<PolygonCollider2D>();
        respawn = GameObject.FindGameObjectWithTag("Respawn");

        Health = 1;//maxHealth;
        Ammo = maxAmmo;
        Fuel = maxFuel;

        // Zacina v mieste respawnu
        RespawnShip();
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

        // Ak je lod "neznicitelna"
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer < 0.0f)
                isInvincible = false;
        }

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

        // Eyes
        RadarScan();
    }

    /// <summary>
    /// Pohyb s lodou
    /// </summary>
    /// <param name="move">Normalizovany vektor udavajuci smer a velkost pohybu</param>
    private void MoveShip(Vector2 move)
    {
        if (!IsDestroyed && this.Fuel > 0)
        {
            // Vypocitaj zmenu rotacie hraca
            var position = rigidbody2d.position;
            var rotation = rigidbody2d.rotation;
            var deltaPos = maxSpeed * move.y * Time.deltaTime;

            // Vypocet prirastku zmeny pozicie a rotacie lode
            rotation += maxTorque * (-move.x) * Time.deltaTime;
            var rotationRad = (rotation + 90.0f) * Mathf.Deg2Rad;
            lookDirection = new Vector2(Mathf.Cos(rotationRad), Mathf.Sin(rotationRad)); // normalizovany vektor smeru pohybu lode
            position += lookDirection * deltaPos;

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
                if (fuelTimer < 0.0f)
                    isEmptyFuel = true;
            }
        }        
    }

    private void RespawnShip()
    {
        // set to respawn point
        rigidbody2d.position = respawn.transform.position;
        rigidbody2d.rotation = respawn.transform.rotation.eulerAngles.z;
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

            // Vykonaj zmenu na UI bare zobrazujuceho zivot hraca
            //UIAmmoBar.instance.SetValue(ammo / (float)maxAmmo);
            Debug.Log($"Player: {this.name} has ammo {this.Ammo}/{this.maxAmmo}");
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

            // Vykonaj zmenu na UI bare zobrazujuceho zivot hraca
            //UIAmmoBar.instance.SetValue(ammo / (float)maxAmmo);
            Debug.Log($"Player: {this.name} has fuel {this.Fuel}/{this.maxFuel}");
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
            // Pri uberani zivota
            if (amount < 0.0f)
            {
                // Ak je hrac "neporazitelny" vyskoc z funkcie
                if (isInvincible)
                    return;

                isInvincible = true;
                invincibleTimer = timeInvincible;

                // Ak uz hrac nema zivoty znici sa lod
                if (this.Health <= 0.0f)
                {
                    IsDestroyed = true;
                    collider2d.enabled = false;
                    respawnTimer = timeRespawn;
                    
                    animator.SetTrigger("Destroyed");
                }
            }
            Health = Mathf.Clamp(Health + amount, 0, maxHealth);

            // Vykonaj zmenu na UI bare zobrazujuceho zivot hraca
            UIHealthBar.instance.SetValue(Health / (float)maxHealth);
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

    // Radar okolo lode
    public void RadarScan()
    {
        // Max distance of ray
        const float dist = 6.0f;

        // Create new array of objects around ship [size = number of rays] (8 lucov po 45 stupnoch okolo celej lode)
        RaycastHit2D[] rays = new RaycastHit2D[8];

        // Front
        rays[0] = SendRay(lookDirection, dist);
        // Front-Right
        rays[1] = SendRay(lookDirection.Shift(ExtendedVector2.down_right), dist);
        // Right
        rays[2] = SendRay(lookDirection.Shift(Vector2.down), dist);
        // Right-Bottom
        rays[3] = SendRay(lookDirection.Shift(ExtendedVector2.down_left), dist);
        // Bottom
        rays[4] = SendRay(lookDirection.Shift(Vector2.left), dist);
        // Bottom-Left
        rays[5] = SendRay(lookDirection.Shift(ExtendedVector2.up_left), dist);
        // Left
        rays[6] = SendRay(lookDirection.Shift(Vector2.up), dist);
        // Left-Front
        rays[7] = SendRay(lookDirection.Shift(ExtendedVector2.up_right), dist);

        // Zobrazi luc v scene
        Debug.DrawRay(rigidbody2d.position, lookDirection, Color.magenta);            // lookDirection = offset = "0deg"        
        Debug.DrawRay(rigidbody2d.position, lookDirection.Shift(ExtendedVector2.down_right), Color.yellow); // lookDirection + Vector(1,0) = "90deg"
        Debug.DrawRay(rigidbody2d.position, lookDirection.Shift(Vector2.down), Color.yellow); // lookDirection + Vector(1,0) = "90deg"
        Debug.DrawRay(rigidbody2d.position, lookDirection.Shift(ExtendedVector2.down_left), Color.yellow); // lookDirection + Vector(1,0) = "90deg"
        Debug.DrawRay(rigidbody2d.position, lookDirection.Shift(Vector2.left), Color.yellow); // lookDirection + Vector(1,0) = "90deg"
        Debug.DrawRay(rigidbody2d.position, lookDirection.Shift(ExtendedVector2.up_left), Color.yellow); // lookDirection + Vector(1,0) = "90deg"
        Debug.DrawRay(rigidbody2d.position, lookDirection.Shift(Vector2.up), Color.yellow); // lookDirection + Vector(1,0) = "90deg"
        Debug.DrawRay(rigidbody2d.position, lookDirection.Shift(ExtendedVector2.up_right), Color.yellow); // lookDirection + Vector(1,0) = "90deg"

        for (int i = 0; i < 8; i++)
        {
            // Ak luc narazil na objekt s colliderom
            if (rays[i].collider != null)
            {
                Debug.Log($"Ray{i} is colliding with {rays[i].collider.name}");
            }
        }
    }

    public RaycastHit2D SendRay(Vector2 direction, float distance)
    {
        // Send ray
        var objs = Physics2D.RaycastAll(rigidbody2d.position, direction, distance);
        foreach (var obj in objs)
        {
            // Ak zasiahnuty objekt nebude vlastna lod
            if (obj.collider != null && obj.collider.gameObject != this.gameObject)
            {
                return obj;
            }
        }

        // Null raycast2D
        return new RaycastHit2D();
    }
}

// Rozsirujuca trieda pre triedu UnityEngine.Vector2
public static class ExtendedVector2
{
    public static readonly Vector2 down_right = new Vector2(1f, -1f);

    public static readonly Vector2 down_left = new Vector2(-1f, -1f);

    public static readonly Vector2 up_left = new Vector2(-1f, 1f);

    public static readonly Vector2 up_right = new Vector2(1f, 1f);

    /// <summary>
    /// Posun vektor o vektor odovzdany parametrom
    /// </summary>
    /// <param name="a">Povodny vektor</param>
    /// <param name="b">Vektor posunu</param>
    /// <returns></returns>
    public static Vector2 Shift(this Vector2 orig, Vector2 shift)
    {
        Vector2 vector = new Vector2(orig.x * shift.x - orig.y * shift.y, orig.y * shift.x + orig.x * shift.y);

        //Debug.Log($"New shifted vector is {vector}.");

        return vector;
    }
}