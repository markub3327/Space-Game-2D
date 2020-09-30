using System.Collections.Generic;

public static class ReplayBuffer
{
    private const int max_count = 1000000;

    public static List<ReplayBufferItem> items = new List<ReplayBufferItem>();

    public static void Add(ReplayBufferItem item)
    {
        // LIFO
        if (items.Count >= max_count)    
            items.RemoveAt(0);
        items.Add(item);        
    }

    public static List<ReplayBufferItem> Sample(int batch_size)
    {
        List<ReplayBufferItem> buff = new List<ReplayBufferItem>(batch_size);

        for (int i = 0; i < batch_size; i++)
        {
            var idx = UnityEngine.Random.Range(0, ReplayBuffer.Count);
            buff.Add(ReplayBuffer.items[idx]);
        }

        return buff;
    }

    public static int Count { get { return items.Count; } }
}

public class ReplayBufferItem
{
    public float[]       State;
    public int          Action;
    public float        Reward;
    public float[]  Next_state;
    public bool           Done;
}