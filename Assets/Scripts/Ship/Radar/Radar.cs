using UnityEngine;

namespace Sensors
{
    public static class Radar
    {
        public const float max_distance = 10.0f;
        
        public const float close_range = 1.0f;

        public const float middle_range = 5.0f;

        public const int num_of_rays = 32;

        public const int num_of_objs = 70;

        // Radar okolo lode
        public static float[] Scan(Vector2 origin, Vector2 lookDirection, ShipController parent)
        {
            var state = new float[num_of_rays * num_of_objs];
            float angle;
            int idx;

            // Vysli luce pod uhlami po 11.25 stupnoch (32 skenov)
            for (idx = 0, angle = 0f; angle < 360; angle += 11.25f, idx+=num_of_objs)
            {
                Debug.DrawRay(origin, lookDirection.Shift(angle), Color.magenta);
                var ray = Physics2D.RaycastAll(origin, lookDirection.Shift(angle), max_distance);
                
                if (ray.Length > 0)
                {
                    if (ray[0].transform != parent.transform)
                    {
                        //Debug.Log($"Ray({parent.name})(angle={angle},{idx}): name = {ray[0].collider.name}, fraction = {ray[0].fraction}");
                        TransformRadarANN(ray[0], state, idx, parent);
                    }
                    else if (ray.Length > 1)
                    {
                        //Debug.Log($"Ray({parent.name})(angle={angle}): name = {ray[1].collider.name}, fraction = {ray[1].fraction}");
                        TransformRadarANN(ray[1], state, idx, parent);
                    }
                    
                    //for (int i = 0; i < num_of_objs; i++)
                    //{
                    //    if (state[idx + i] > 0f)
                    //       Debug.Log($"Ray({parent.Nickname})(angle={angle}): state[{i}]={state[idx + i]}");
                    //}
                }
                //Debug.Log($"idx={idx}, angle = {angle}");
            }

            return state;
        }  

        private static void TransformRadarANN(RaycastHit2D ray, float[] state, int idx, ShipController parent)
        {
            switch (ray.collider.name)
            {
                case "Saturn":
                    TransformPlanetANN(ray, state, idx, parent);
                    break;
                case "Earth":
                    TransformPlanetANN(ray, state, idx + 6, parent);
                    break;
                case "Mars":
                    TransformPlanetANN(ray, state, idx + 12, parent);
                    break;
                case "Jupiter":
                    TransformPlanetANN(ray, state, idx + 18, parent);
                    break;
                case "Space":
                    TransformDistance(ray, state, idx + 24);
                    break;
                case "Nebula-Red":
                    // Ak lod potrebuje palivo
                    if (parent.Fuel > (ShipController.maxFuel-1))
                        TransformDistance(ray, state, idx + 26);
                    else if (parent.Fuel > (ShipController.maxFuel/2f))
                        TransformDistance(ray, state, idx + 28);
                    else
                        TransformDistance(ray, state, idx + 30);
                    break;
                case "Nebula-Blue":
                    // Ak lod potrebuje palivo
                    if (parent.Fuel > (ShipController.maxFuel-1))
                        TransformDistance(ray, state, idx + 32);
                    else if (parent.Fuel > (ShipController.maxFuel/2f))
                        TransformDistance(ray, state, idx + 34);
                    else
                        TransformDistance(ray, state, idx + 36);
                    break;
                case "Nebula-Silver":
                    // Ak lod potrebuje palivo
                    if (parent.Fuel > (ShipController.maxFuel-1))
                        TransformDistance(ray, state, idx + 38);
                    else if (parent.Fuel > (ShipController.maxFuel/2f))
                        TransformDistance(ray, state, idx + 40);
                    else
                        TransformDistance(ray, state, idx + 42);
                    break;
                case "Asteroid":
                    TransformDistance(ray, state, idx + 44);
                    break;
                case "Sun":
                    TransformDistance(ray, state, idx + 46);
                    break;                
                case "Ammo":
                    // Ak lod potrebuje palivo
                    if (parent.Ammo > (ShipController.maxAmmo-10))
                        TransformDistance(ray, state, idx + 48);
                    else if (parent.Ammo > (ShipController.maxAmmo/2f))
                        TransformDistance(ray, state, idx + 50);
                    else
                        TransformDistance(ray, state, idx + 52);
                    break;                
                case "Health":
                    // Ak lod potrebuje palivo
                    if (parent.Health > (ShipController.maxHealth-1))                
                        TransformDistance(ray, state, idx + 54);
                    else if (parent.Health > (ShipController.maxHealth/2f))                
                        TransformDistance(ray, state, idx + 56);
                    else
                        TransformDistance(ray, state, idx + 58);
                    break;                
                case "Moon":
                    TransformDistance(ray, state, idx + 60);
                    break;                
                case "Fobos":
                    TransformDistance(ray, state, idx + 62);
                    break;       
                case "Projectile":
                    var projectile = ray.collider.GetComponent<Projectile>();
                    if (projectile.firingShip == parent)
                        TransformDistance(ray, state, idx + 64);
                    else
                        TransformDistance(ray, state, idx + 66);
                    break;
                case "Ship-Destroyer":
                    TransformDistance(ray, state, idx + 68);
                    break;
                default:
                    Debug.Log($"ray.name = {ray.collider.name}, ray.fraction = {ray.fraction}");
                    break;
            }
        } 

        private static void TransformPlanetANN(RaycastHit2D ray, float[] state, int idx, ShipController parent)
        {
            var planet = ray.collider.GetComponent<PlanetController>();
            // Ak planeta nema vlastnika oznaci sa ako volna planeta
            if (planet.OwnerPlanet == null)
                TransformDistance(ray, state, idx);
            else if (planet.OwnerPlanet == parent.gameObject)
                TransformDistance(ray, state, idx + 2);
            else
                TransformDistance(ray, state, idx + 4);
        }    

        private static void TransformDistance(RaycastHit2D ray, float[] state, int idx)
        {
            // blizka prekazka
            if (ray.distance <= close_range)
            {
                state[idx] = 1.0f;
                state[idx + 1] = 1.0f;
                //Debug.Log("Round1");
            }
            // stredne vzdialena prekazka
            else if (ray.distance <= middle_range)
            {
                state[idx] = 0.0f;
                state[idx + 1] = 1.0f;
                //Debug.Log("Round2");
            }
            // vzdialena prekazka
            else
            {
                state[idx] = 1.0f;
                state[idx + 1] = 0.0f;                
                //Debug.Log("Round3");
            }
        }
    }
}
