using UnityEngine;

namespace Sensors
{
    public static class Radar
    {
        public const float max_distance = 10f;

        // Radar okolo lode
        public static RaycastHit2D?[] Scan(Vector2 origin, Vector2 lookDirection, Transform parent)
        {
            float angle = 0f;
            var hits = new RaycastHit2D?[32];

            // Vysli luce pod uhlami po 11.25 stupnoch (32 skenov)
            for (int idx = 0; angle < 360; angle += 11.25f, idx++)
            {
                hits[idx] = null;

                // Maximalna dlzka lucu - 5 units
                var ray = Physics2D.RaycastAll(origin, lookDirection.Shift(angle), max_distance);                
                for (int i = 0; i < ray.Length; i++)
                {
                    if (ray[i].transform != parent)
                    {
                        hits[idx] = ray[i];
                        break;
                    }
                }
                //Debug.DrawRay(origin, lookDirection.Shift(angle), Color.magenta);
            }

            return hits;
        }        
    }
}
