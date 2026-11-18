namespace MiniHttpServer.HtmlTemplating.UnitTests;

/// <summary>Плоская модель: только простые свойства.</summary>
public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public string? Nickname { get; set; }
}

/// <summary>Модель с вложенным объектом — для ${user.Name}.</summary>
public class Order
{
    public string Number { get; set; } = "";
    public Person Customer { get; set; } = new();
    public decimal Total { get; set; }
    public List<string> Items { get; set; } = new();
}

/// <summary>Модель с null-полями — для проверки fallback'ов.</summary>
public class NullableModel
{
    public string? Name { get; set; }
    public Person? User { get; set; }
    public List<string>? Items { get; set; }
}

/// <summary>Модель с коллекцией — для перечисления.</summary>
public class Cart
{
    public string Owner { get; set; } = "";
    public List<Person> Customers { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}