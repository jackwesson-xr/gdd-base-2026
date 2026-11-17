namespace GDD110
{
    /// <summary>Something a held Pickup does when the player presses Use (left click / RT). Add a component that implements this to a Pickup.</summary>
    public interface IGddUsable
    {
        void Use(GddActor holder);
    }
}
