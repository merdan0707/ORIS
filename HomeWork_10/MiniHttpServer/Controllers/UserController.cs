using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.HtmlTemplating;
using MiniHttpServer.Models;
using MiniHttpServer.ORM;

namespace MiniHttpServer.Controllers;

[HttpController]
public class UserController : BaseController
{
    private static IOrmContext CreateOrm() => new OrmContext(ConfigurationManager.Instance.Settings.ConnectionString);
    
    [HttpGet]
    public async Task<IHttpResult> List()
    {
        var orm = CreateOrm();
        IReadOnlyList<User> users = await orm.GetAllAsync<User>("users");
        
        return Page("User/List.html", new { Users = users });
    }

    [HttpGet]
    [Route("find")]
    public async Task<IHttpResult> FindById([FromQuery] int id)
    {
        var orm = CreateOrm();
        var user = await orm.GetByIdAsync<User>("users", id);

        if (user == null)
            return Text($"Пользователь с Id={id} не найден", 404); // 404 Not Found
        
        return Text($"Найден: {user.Name}, группа: {user.GroupName}, возраст: {user.Age}");
    }
    
    [HttpPost]
    [Route("create")]
    public async Task<IHttpResult> Create(
        [FromForm] string name,
        [FromForm] string groupName,
        [FromForm] int age,
        [FromForm] bool gender)
    {
        var orm = CreateOrm();
        var user = new User
        {
            Name = name,
            GroupName = groupName,
            Age = age,
            Gender = gender
        };
        int affected = await orm.InsertAsync("users", user);
        return Text($"Создано записей: {affected}");
    }
    
    [HttpPost]
    [Route("update")]
    public async Task<IHttpResult> Update(
        [FromForm] int id,
        [FromForm] string name,
        [FromForm] string groupName,
        [FromForm] int age,
        [FromForm] bool gender)
    {
        var orm = CreateOrm();
        var user = new User
        {
            Name = name,
            GroupName = groupName,
            Age = age,
            Gender = gender
        };
        int affected = await orm.UpdateAsync("users", id, user);
        return Text($"Обновлено записей: {affected}");
    }
    
    [HttpGet]
    [Route("delete")]
    public async Task<IHttpResult> Delete([FromQuery] int id)
    {
        var orm = CreateOrm();
        int affected = await orm.DeleteAsync("users", id);
        return Text($"Удалено записей: {affected}");
    }

    [HttpGet]
    [Authorize]
    public IHttpResult Profile()
    {
        return Html("<h1>Личный кабинет</h1><p>Вы успешно авторизованы.</p>");
    }
}