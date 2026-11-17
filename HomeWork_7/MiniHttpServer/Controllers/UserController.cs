using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Controllers;

[HttpController]
public class UserController : BaseController
{
    [HttpGet]
    public IHttpResult Profile()
    {
        var user = new
        {
            Name = "Alex",
            Age = 20,
            Email = "alex@example.com",
            IsActive = true
        };

        return Page("User/Profile.html", user);
    }

    [HttpGet]
    public IHttpResult List()
    {
        var users = new[]
        {
            new { Name = "Alex",  Age = 20, IsActive = true  },
            new { Name = "Maria", Age = 22, IsActive = false },
            new { Name = "Ivan",  Age = 19, IsActive = true  }
        };
        return Page("User/List.html", new { Users = users });
    }
}