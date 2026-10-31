namespace Controlwork_1.Entities;

public enum BookingStatus { Pending, Confirmed }

public class Booking
{
    public int Id { get; set; }
    public string Visitor { get; set; } = "";
    public int TableNumber { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
}