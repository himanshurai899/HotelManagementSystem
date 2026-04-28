using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.Models;

public class RoomBlock
{
    // RoomBlock.cs
    public int          Id        { get; set; }
    public int          RoomId    { get; set; }
    public Room         Room      { get; set; } = null!;
    public DateTime     StartDate { get; set; }
    public DateTime     EndDate   { get; set; }
    public string       Reason    { get; set; } = string.Empty;
    public RoomBlockType BlockType { get; set; }
    public DateTime     CreatedAt { get; set; } = DateTime.UtcNow;
}
