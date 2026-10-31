using System.Text;
using System.Web;
using Controlwork_1.Attributes;
using Controlwork_1.Entities;
using Controlwork_1.HttpResults;
using Controlwork_1.HttpResults.Abstractions;
using Controlwork_1.InmemoryData;

namespace Controlwork_1.Handlers;

[HttpController]
[Route("bookings")]
public class BookingsController
{
    private static BookingStore Store => BookingStore.Instance;

    // GET /api/bookings?tableNumber=5
    [HttpGet]
    [Route("")]
    public IActionResult List([FromQuery] int? tableNumber = null)
    {
        if (tableNumber is < 1 or > 20)
            throw new ArgumentException("Параметр tableNumber должен быть целым от 1 до 20.");

        var items = Store.GetAll(tableNumber);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Бронирования</title>");
        sb.Append("<style>table{border-collapse:collapse}td,th{border:1px solid #888;padding:4px 8px}</style>");
        sb.Append("</head><body><h1>Бронирования</h1>");

        sb.Append("<form method='get' action='/api/bookings'>Столик: ");
        sb.Append("<input type='number' name='tableNumber' min='1' max='20' value='")
          .Append(tableNumber?.ToString() ?? "").Append("'> <button>Фильтр</button></form>");

        sb.Append("<p><a href='/api/bookings/create'>Создать бронирование</a></p>");

        if (items.Count == 0)
        {
            sb.Append("<p>Бронирований не найдено.</p>");
        }
        else
        {
            sb.Append("<table><tr><th>Id</th><th>Посетитель</th><th>Столик</th><th>Статус</th></tr>");
            foreach (var b in items)
            {
                sb.Append("<tr>")
                  .Append("<td>").Append(b.Id).Append("</td>")
                  .Append("<td>").Append(HttpUtility.HtmlEncode(b.Visitor)).Append("</td>")
                  .Append("<td>").Append(b.TableNumber).Append("</td>")
                  .Append("<td>").Append(b.Status == BookingStatus.Confirmed ? "confirmed" : "pending").Append("</td>")
                  .Append("</tr>");
            }
            sb.Append("</table>");
        }

        sb.Append("</body></html>");
        return new HtmlResult(sb.ToString());
    }

    // GET /api/bookings/details?id=1
    [HttpGet]
    [Route("details")]
    public IActionResult Details([FromQuery] int id)
    {
        var b = Store.GetById(id)
                ?? throw new KeyNotFoundException($"Бронирование #{id} не найдено.");

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Бронирование ")
          .Append(b.Id).Append("</title></head><body>");
        sb.Append("<h1>Бронирование #").Append(b.Id).Append("</h1>");
        sb.Append("<p>Посетитель: ").Append(HttpUtility.HtmlEncode(b.Visitor)).Append("</p>");
        sb.Append("<p>Столик: ").Append(b.TableNumber).Append("</p>");
        sb.Append("<p>Статус: ").Append(b.Status == BookingStatus.Confirmed ? "confirmed" : "pending").Append("</p>");
        sb.Append("<p><a href='/api/bookings'>К списку</a></p>");
        sb.Append("</body></html>");
        return new HtmlResult(sb.ToString());
    }

    // GET /api/bookings/create — форма
    [HttpGet]
    [Route("create")]
    public IActionResult CreateForm()
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><title>Новое бронирование</title></head><body>");
        sb.Append("<h1>Новое бронирование</h1>");
        sb.Append("<form method='post' action='/api/bookings/create'>");
        sb.Append("Посетитель (2–60): <input type='text' name='visitor' minlength='2' maxlength='60' required><br><br>");
        sb.Append("Столик (1–20): <input type='number' name='tableNumber' min='1' max='20' required><br><br>");
        sb.Append("<button type='submit'>Создать</button>");
        sb.Append("</form>");
        sb.Append("<p><a href='/api/bookings'>К списку</a></p>");
        sb.Append("</body></html>");
        return new HtmlResult(sb.ToString());
    }

    // POST /api/bookings/create
    [HttpPost]
    [Route("create")]
    public IActionResult Create([FromForm] string visitor, [FromForm] int tableNumber)
    {
        var created = Store.Create(visitor, tableNumber);
        return new RedirectResult($"/api/bookings/details?id={created.Id}");
    }

    // PUT /api/bookings/update?id=1
    [HttpPut]
    [Route("update")]
    public IActionResult Update(
        [FromQuery] int id,
        [FromForm] string visitor,
        [FromForm] int tableNumber,
        [FromForm] BookingStatus status)
    {
        _ = Store.Update(id, visitor, tableNumber, status)
            ?? throw new KeyNotFoundException($"Бронирование #{id} не найдено.");

        return new NoContentResult();
    }
}