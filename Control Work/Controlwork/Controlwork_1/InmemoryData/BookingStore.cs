using Controlwork_1.Entities;
using Controlwork_1.Exeptions;

namespace Controlwork_1.InmemoryData;

public sealed class BookingStore
{
    public static BookingStore Instance { get; } = new();

    private readonly List<Booking> _bookings = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public IReadOnlyList<Booking> GetAll(int? tableNumber = null)
    {
        lock (_lock)
        {
            var q = _bookings.AsEnumerable();
            if (tableNumber.HasValue) q = q.Where(b => b.TableNumber == tableNumber.Value);
            return q.OrderBy(b => b.Id).ToList();
        }
    }

    public Booking? GetById(int id)
    {
        lock (_lock) return _bookings.FirstOrDefault(b => b.Id == id);
    }

    public Booking Create(string visitor, int tableNumber)
    {
        Validate(visitor, tableNumber);
        lock (_lock)
        {
            var booking = new Booking
            {
                Id = _nextId++,
                Visitor = visitor.Trim(),
                TableNumber = tableNumber,
                Status = BookingStatus.Pending
            };
            _bookings.Add(booking);
            return booking;
        }
    }

    public Booking? Update(int id, string visitor, int tableNumber, BookingStatus status)
    {
        Validate(visitor, tableNumber);
        lock (_lock)
        {
            var booking = _bookings.FirstOrDefault(x => x.Id == id);
            if (booking == null) return null;

            if (status == BookingStatus.Confirmed &&
                _bookings.Any(x => x.Id != id &&
                                   x.TableNumber == tableNumber &&
                                   x.Status == BookingStatus.Confirmed))
            {
                throw new ConflictException($"Столик {tableNumber} уже имеет подтверждённое бронирование.");
            }

            booking.Visitor = visitor.Trim();
            booking.TableNumber = tableNumber;
            booking.Status = status;
            return booking;
        }
    }

    private static void Validate(string visitor, int tableNumber)
    {
        if (string.IsNullOrWhiteSpace(visitor) || visitor.Length < 2 || visitor.Length > 60)
            throw new ArgumentException("Имя посетителя должно быть от 2 до 60 символов.");
        if (tableNumber < 1 || tableNumber > 20)
            throw new ArgumentException("Номер столика должен быть от 1 до 20.");
    }
}