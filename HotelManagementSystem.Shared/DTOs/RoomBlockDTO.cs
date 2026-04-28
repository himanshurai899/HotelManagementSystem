using HotelManagementSystem.Shared.Enums;

namespace HotelManagementSystem.Shared.DTOs;

public class RoomBlockDTO
{
    public int           Id         { get; set; }
    public int           RoomId     { get; set; }
    public string        RoomNumber { get; set; } = string.Empty;  // To avoid complex object graph
    public DateTime      StartDate  { get; set; }
    public DateTime      EndDate    { get; set; }
    public string        Reason     { get; set; } = string.Empty;
    public RoomBlockType BlockType  { get; set; }
    public DateTime      CreatedAt  { get; set; }
}
