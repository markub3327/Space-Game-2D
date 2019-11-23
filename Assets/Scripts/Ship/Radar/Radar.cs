using UnityEngine;

namespace Sensors
{
    public static class Radar
    {
        // Radar okolo lode
        public static RaycastHit2D?[] Scan(Vector2 origin, Vector2 lookDirection, Transform parent)
        {
            float angle = 0f;
            var hits = new RaycastHit2D?[16];

            // Vysli luce pod uhlami po 22.5 stupnoch (16 skenov)
            for (int idx = 0; angle < 360; angle += 22.5f, idx++)
            {
                hits[idx] = null;

                // Maximalna dlzka lucu - 10 units
                var rays = Physics2D.RaycastAll(origin, lookDirection.Shift(angle), 10f);                
                for (int i = 0; i < rays.Length; i++)
                {
                    if (rays[i].transform != parent)
                    {
                        hits[idx] = rays[i];
                        break;
                    }
                }
                Debug.DrawRay(origin, lookDirection.Shift(angle), Color.magenta);
            }

            return hits;
        }        
    }
}
