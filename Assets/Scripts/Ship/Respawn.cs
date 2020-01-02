using UnityEngine;

public static class Respawn
{
    private struct RespawnPoint
    {
        public Vector2 point;
        public bool isUsed;  

        public RespawnPoint(float x, float y)
        {
            this.point = new Vector2(x, y);
            this.isUsed = false;
        }      
    }
    
    private static RespawnPoint[] respawnPoints = 
    { 
        new RespawnPoint(13, -5),
        new RespawnPoint(5, -10),
        new RespawnPoint(-9, 2),
        new RespawnPoint(-6, -12),
        new RespawnPoint(11, 0.5f),
        new RespawnPoint(-11, -1),
    };

    private static int usedCount = 0;

    public static Vector2 getPoint()
    {
        RespawnPoint point;
        do {
            point = respawnPoints[Random.Range(0, respawnPoints.Length)];
        } while (point.isUsed);
        usedCount++;

        if (usedCount >= 4)
        {
            for (int i = 0; i < respawnPoints.Length; i++)
            {
                respawnPoints[i].isUsed = false;
            }
            usedCount = 0;
        }

        return point.point;
    }
}