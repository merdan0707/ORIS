namespace MiniHttpServer.Models;

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? GroupName { get;  set;}
    public int Age { get; set;}
    public bool Gender { get; set;}

    public User() { }
    
    public User(string name, string group, int age, bool gender)
    {
        Name = name;
        GroupName = group;
        Age = age;
        Gender = gender;
    }
}