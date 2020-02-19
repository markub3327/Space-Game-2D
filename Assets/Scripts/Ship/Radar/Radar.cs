using UnityEngine;

namespace Sensors
{
    public static class Radar
    {
        public const float max_distance = 2f;

        public const int num_of_rays = 32;

        public const int num_of_objs = 35;

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
                    //        Debug.Log($"Ray({parent.name})(angle={angle}): state[{i}]={state[idx + i]}");
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
                    TransformPlanetANN(ray, state, idx + 3, parent);
                    break;
                case "Mars":
                    TransformPlanetANN(ray, state, idx + 6, parent);
                    break;
                case "Jupiter":
                    TransformPlanetANN(ray, state, idx + 9, parent);
                    break;
                case "Space":
                    state[idx + 12] = 1.0f;
                    break;
                case "Nebula-Red":
                    // Ak lod potrebuje palivo
                    if (parent.Fuel > (ShipController.maxFuel-1))
                        state[idx + 13] = 1.0f;
                    else if (parent.Fuel > (ShipController.maxFuel/2f))
                        state[idx + 14] = 1.0f;
                    else
                    {
                        state[idx + 15] = 1.0f;
                        //Debug.Log("Fuel is full!");                        
                    }
                    break;
                case "Nebula-Blue":
                    // Ak lod potrebuje palivo
                    if (parent.Fuel > (ShipController.maxFuel-1))
                        state[idx + 16] = 1.0f;
                    else if (parent.Fuel > (ShipController.maxFuel/2f))
                        state[idx + 17] = 1.0f;
                    else
                    {
                        state[idx + 18] = 1.0f;
                        //Debug.Log("Fuel is full!");                        
                    }
                    break;
                case "Nebula-Silver":
                    // Ak lod potrebuje palivo
                    if (parent.Fuel > (ShipController.maxFuel-1))
                        state[idx + 19] = 1.0f;
                    else if (parent.Fuel > (ShipController.maxFuel/2f))
                        state[idx + 20] = 1.0f;
                    else
                    {
                        state[idx + 21] = 1.0f;
                        //Debug.Log("Fuel is full!");                        
                    }
                    break;
                case "Asteroid":
                    state[idx + 22] = 1.0f;
                    break;
                case "Sun":
                    state[idx + 23] = 1.0f;
                    break;                
                case "Ammo":
                    // Ak lod potrebuje palivo
                    if (parent.Ammo > (ShipController.maxAmmo-10))
                        state[idx + 24] = 1.0f;
                    else if (parent.Ammo > (ShipController.maxAmmo/2f))
                        state[idx + 25] = 1.0f;
                    else
                        state[idx + 26] = 1.0f;
                    break;                
                case "Health":
                    // Ak lod potrebuje palivo
                    if (parent.Health > (ShipController.maxHealth-1))                
                        state[idx + 27] = 1.0f;
                    else if (parent.Health > (ShipController.maxHealth/2f))                
                        state[idx + 28] = 1.0f;
                    else
                        state[idx + 29] = 1.0f;
                    break;                
                case "Moon":
                    state[idx + 30] = 1.0f;
                    break;                
                case "Fobos":
                    state[idx + 31] = 1.0f;
                    break;       
                case "Projectile":
                    var projectile = ray.collider.GetComponent<Projectile>();
                    if (projectile.firingShip == parent)
                        state[idx + 32] = 1.0f;
                    else
                        state[idx + 33] = 1.0f;
                    break;
                case "Ship-Destroyer":
                    state[idx + 34] = 1.0f;
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
                state[idx] = 1.0f;
            else if (planet.OwnerPlanet == parent.gameObject)
                state[idx + 1] = 1.0f;
            else
                state[idx + 2] = 1.0f;
        }      
    }
}
