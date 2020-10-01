using UnityEngine;

namespace Sensors
{
    public static class Radar
    {
        public const int num_of_rays = 16;
        public const int num_of_objs = 25;

        // Radar okolo lode
        public static float[] Scan(Vector2 origin, Vector2 lookDirection, ShipController parent)
        {
            var state = new float[num_of_rays * num_of_objs + 3];
            float angle;
            int idx;

            // stav hraca
            state[0] = parent.Health / (float)ShipController.maxHealth;
            state[1] = parent.Ammo / (float)ShipController.maxAmmo;
            state[2] = parent.Fuel / (float)ShipController.maxFuel;
            //Debug.Log($"state[0]: {state[0]}, state[1]: {state[1]}, state[2]: {state[2]}");

            // Vysli luce pod uhlami po 22.5 stupnoch
            for (idx = 3, angle = 0f; angle < 360; angle += 22.5f, idx+=num_of_objs)
            {
                //Debug.DrawRay(origin, lookDirection.Shift(angle), Color.magenta);
                var ray = Physics2D.RaycastAll(origin, lookDirection.Shift(angle), 20.0f);
                
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
                }
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
                    state[idx + 12] = ray.fraction;
                    break;
                case "Nebula-Red":
                    state[idx + 13] = ray.fraction;
                    break;
                case "Nebula-Blue":
                    state[idx + 14] = ray.fraction;
                    break;
                case "Nebula-Silver":
                    state[idx + 15] = ray.fraction;
                    break;
                case "Asteroid":
                    state[idx + 16] = ray.fraction;
                    break;
                case "Sun":
                    state[idx + 17] = ray.fraction;
                    break;                
                case "Ammo":
                    state[idx + 18] = ray.fraction;
                    break;                
                case "Health":
                    state[idx + 19] = ray.fraction;
                    break;
                case "Moon":
                    state[idx + 20] = ray.fraction;
                    break;                
                case "Fobos":
                    state[idx + 21] = ray.fraction;
                    break;       
                case "Projectile":
                    var projectile = ray.collider.GetComponent<Projectile>();
                    if (projectile.firingShip == parent)
                        state[idx + 22] = ray.fraction;
                    else
                        state[idx + 23] = ray.fraction;
                    break;
                case "Ship-Destroyer":
                    state[idx + 24] = ray.fraction;
                    break;
                default:
                    Debug.Log($"ray.name = {ray.collider.name}, distance = {ray.distance}, fraction = {ray.fraction}");
                    break;
            }
        } 

        private static void TransformPlanetANN(RaycastHit2D ray, float[] state, int idx, ShipController parent)
        {
            var planet = ray.collider.GetComponent<PlanetController>();
            // Ak planeta nema vlastnika oznaci sa ako volna planeta
            if (planet.OwnerPlanet == null)
                state[idx] = ray.fraction;
            else if (planet.OwnerPlanet == parent.gameObject)
                state[idx + 1] = ray.fraction;
            else
                state[idx + 2] = ray.fraction;
        }    
    }
}
