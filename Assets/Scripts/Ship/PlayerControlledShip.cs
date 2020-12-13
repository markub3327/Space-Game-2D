using UnityEngine;
using System.IO;

public class PlayerControlledShip : ShipController
{
    public StreamWriter log_file;

    //private ReplayBufferItem replayBufferItem = null;

    // Use this for initialization
    public override void Start()
    {
        // Spusti prvu funkciu nadtriedy
        base.Start();

        log_file = File.CreateText("clovek.log");

        // Nacitaj stav, t=0
        //this.replayBufferItem = new ReplayBufferItem { State = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this) };
        this.reward = -0.015f;    // najkratsia cesta k planetam
        this.done = false;
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
            //Debug.Log($"move: {move.x}, {move.y}");

            // Pohni s lodou podla vstupu od uzivatela
            this.MoveShip(move);

            if (Input.GetKeyDown("space"))
            {
                Fire();
            }

            // Nacitaj stav hry
            //this.replayBufferItem.Next_state = Sensors.Radar.Scan(this.rigidbody2d.position, this.LookDirection, this);
            
            //this.replayBufferItem.Reward = this.reward;
            this.score += this.reward;
            //this.replayBufferItem.Done = this.done;
            this.step += 1;
            this.levelBox.text = this.score.ToString("0.00");

            // Ak uz hrac nema zivoty ani palivo znici sa lod
            if (this.Health <= 0 || this.Fuel <= 0)
            {
                show_stat();

                // Destrukcia lode
                this.DestroyShip();
            }
            // hrac obsadil vsetky planety
            else if (this.myPlanets.Count >= 4)
            {
                show_stat();

                Debug.Log("Vytaz!!!");
                WinnerShip();
            }

            // Uloz udalost do bufferu
            //ReplayBuffer.Add(this.replayBufferItem);

            //this.replayBufferItem = new ReplayBufferItem { State = this.replayBufferItem.Next_state };
            this.reward = -0.015f;    // najkratsia cesta k planetam
            this.done = false;
        }
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
