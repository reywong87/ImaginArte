public sealed class RoomCleanupOptions
{
    public const string SectionName = "RoomCleanup";

    public bool Enabled { get; set; }

    public int InactiveRoomTtlHours { get; set; } = 12;

    public int IntervalMinutes { get; set; } = 30;
}
