namespace ReE.Combat.Effects
{
    public enum StackRule
    {
        Refresh = 0, // Reset Duration
        Extend = 1,  // Add Duration
        Stack = 2,   // Add separate instance
        Replace = 3  // Remove old, add new
    }
}
