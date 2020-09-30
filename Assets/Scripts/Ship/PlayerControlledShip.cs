using UnityEngine;
using System.IO;

public class PlayerControlledShip : ShipController
{
    public StreamWriter log_file;

    // Use this for initialization
    public override void Start()
    {
        // Spusti prvu funkciu nadtriedy
        base.Start();

        this.reward = -0.001f;         // za pohyb lode        

        log_file = File.CreateText("clovek.log");
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
                show_stat();

                this.episode += 1;
                this.score = 0f;
                this.step = 0;

                // Destrukcia lode
                this.DestroyShip();
            }
            // hrac obsadil vsetky planety
            else if (this.myPlanets.Count >= 4)
            {
                show_stat();

                this.episode += 1;
                this.score = 0f;
                this.step = 0;

                Debug.Log("Vytaz!!!");
                WinnerShip();
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

        // log bestAgent to file
        this.log_file.WriteLine($"{this.episode};{this.step};{this.score};{this.myPlanets.Count};{this.Health};{this.Ammo};{this.Fuel}");
    }

    public void OnApplicationQuit()
    {
        log_file.Close();
    }
}
