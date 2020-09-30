using UnityEngine;

public class PlayerControlledShip : ShipController
{
    // Use this for initialization
    public override void Start()
    {
        // Spusti prvu funkciu nadtriedy
        base.Start();

        this.reward = -0.001f;         // za pohyb lode        
    }

    public void Update()
    {
        // Ak nie je lod znicena = hra hru
        if (!IsDestroyed)
        {   
            // Nacitaj zmenu polohy z klavesnice alebo joysticku
            float axisH = Input.GetAxis("Horizontal");
            float axisV = Input.GetAxis("Vertical");
            Vector2 move = new Vector2(axisH, axisV).normalized;

            // Pohni s lodou podla vstupu od uzivatela
            this.MoveShip(move);

            if (Input.GetKeyDown("space"))
            {
                Fire();
            }

            this.score += this.reward;
            this.step += 1;
            //this.fitness = ((this.Health / (float)ShipController.maxHealth) * 0.10f) + ((this.Fuel / (float)ShipController.maxFuel) * 0.10f) + ((this.Ammo / (float)ShipController.maxAmmo) * 0.05f) + ((float)this.Hits * 0.05f) + ((float)this.myPlanets.Count * 0.70f);
            this.levelBox.text = this.score.ToString("0.00");

            // Ak uz hrac nema zivoty ani palivo znici sa lod
            if (this.Health <= 0 || this.Fuel <= 0)
            {
                // Destrukcia lode
                this.DestroyShip();
                    
                this.episode += 1;

                show_stat();
            }
            // hrac obsadil vsetky planety
            else if (this.myPlanets.Count >= 4)
            {
                Debug.Log("Vytaz!!!");
                WinnerShip();

                this.episode += 1;

                show_stat();
            }

            this.reward = -0.001f;         // za pohyb lode
        }
        else
            RespawnShip();
    }

    private void show_stat()
    {
        if (this.myPlanets.Count > 1)
        {
            var strPlanets = string.Empty;
            this.myPlanets.ForEach(x => strPlanets += x.name + ", ");
            Debug.Log($"MyPlanets[{this.Nickname}]: {strPlanets}");
        }

        Debug.Log($"episode[{this.Nickname}]: {episode}");
        Debug.Log($"step[{this.Nickname}]: {step}");
        Debug.Log($"score[{this.Nickname}]: {score}");
    }
}
