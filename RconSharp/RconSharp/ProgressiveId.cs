namespace RconSharp;

/// <summary>
/// Progressive id counter starting from 0
/// </summary>
public static class ProgressiveId
{
    private static readonly object Padlock = new();
    private static int Counter = 1;

    /// <summary>
    /// Get next Id
    /// </summary>
    /// <returns>Next progressive Id</returns>
    public static int Next()
    {
        lock (Padlock)
        {
            return Counter++;
        }
    }
    /// <summary>
    /// Change the starting value for internal counter
    /// </summary>
    /// <param name="seed"></param>
    public static void Seed(int seed = 0)
    {
        lock (Padlock)
        {
            Counter = seed;
        }
    }
}
