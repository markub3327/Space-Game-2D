using UnityEngine;

namespace Sensors
{
    public static class Radar
    {
        // Matica zasiahnutych objektov lucom (uhly x okruhy)
        private static readonly RaycastHit2D[][] hits = new RaycastHit2D[8][];

        // Radar okolo lode
        public static void Scan(Vector2 origin, Vector2 lookDirection)
        {
            float angle = 0;    // uhol lucu

            // Vysli luce pod uhlami po 45 stupnov (8 skenov)
            for (int idx = 0; angle < 360; angle += 45, idx++)
            {
                hits[idx] = Physics2D.RaycastAll(origin, lookDirection.Shift(angle), 20f);
                Debug.DrawRay(origin, lookDirection.Shift(angle), Color.magenta);
            }
        }

        public static RaycastHit2D? GetNearestObject(GameObject itselfObject)
        {            
            RaycastHit2D? wanted = null;        // referencia na hladany objekt

            // Prejde vsetky zasahy lucov radaru
            foreach (var obj in hits)
            {
                for (int i = 0; i < obj.Length; i++)
                {
                    if (obj[i].transform.CompareTag("Player") && obj[i].transform.gameObject != itselfObject)
                    {
                        Debug.Log($"obj[{i}] = {obj[i].transform.name}, {obj[i].distance}");

                        if (wanted == null || obj[i].distance < wanted.Value.distance)
                            wanted = obj[i];
                    }
                }
            }
            return wanted;
        }

        public static GameObject[] GetRound(int round)
        {
            GameObject[] objects = new GameObject[hits.Length];

            // Prejde vsetky zasahy lucov radaru
            for (int i = 0; i < objects.Length; i++)
            {
                // ak existuje okruh videnych objektov a maju pripojene collidery
                if (hits[i].Length > round && hits[i][round].collider != null)
                    objects[i] = hits[i][round].collider.gameObject;
                else
                    objects[i] = null;  // inak vrat nulovy objekt (nenasiel sa)
            }

            return objects;
        }
    }
}
