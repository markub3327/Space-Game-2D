using UnityEngine;

namespace Sensors
{
    public static class Radar
    {
        public const float max_distance = 3f;

        // Radar okolo lode
        public static RaycastHit2D?[] Scan(Vector2 origin, Vector2 lookDirection, Transform parent)
        {
            float angle = 0f;
            var hits = new RaycastHit2D?[16];

            // Vysli luce pod uhlami po 22.5 stupnoch (16 skenov)
            for (int idx = 0; angle < 360; angle += 22.5f, idx++)
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
