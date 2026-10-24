using System.Net;
using System.Net.Mail;
using MyServer_v3.Framework.Attributes;

namespace MyServer_v3.Controllers;

[HttpController]
public class SteamController
{
    [HttpGet]
    public void SendMessage(string toLogin, string hero)
    {
        
    }
}