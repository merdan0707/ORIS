using System.Data;
using MiniHttpServer.ORM.Mapping;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public class EntityMapperTests
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public enum UserRole
    {
        Guest = 0,
        Admin = 1,
        Moderator = 2
    }

    public class UserWithRole
    {
        public int Id { get; set; }
        public UserRole Role { get; set; }
    }


    [TestMethod]
    public void MapAll_MapsColumnsToProperties()
    {
        // Arrange
        var table = CreateUserTable();
        table.Rows.Add(1, "Иван", 20);
        table.Rows.Add(2, "Мария", 21);
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<User>(reader);
        
        // Assert
        Assert.HasCount(2, users);
        Assert.AreEqual(1, users[0].Id);
        Assert.AreEqual("Иван", users[0].Name);
        Assert.AreEqual(20, users[0].Age);
        Assert.AreEqual("Мария", users[1].Name);
    }
    
    [TestMethod]
    public void MapAll_CaseInsensitiveColumnNames()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));    // маленькими
        table.Columns.Add("NAME", typeof(string)); // большими
        table.Rows.Add(1, "Иван");
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<User>(reader);
        
        // Assert
        Assert.AreEqual("Иван", users[0].Name);
    }

    [TestMethod]
    public void MapAll_UnknownColumnIgnored()
    {
        // Arrange
        var table = CreateUserTable();
        table.Columns.Add("UnknownColumn", typeof(string));
        table.Rows.Add(1, "John", 30, "empty");
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<User>(reader);
        
        // Assert
        Assert.HasCount(1, users);
        Assert.AreEqual("John", users[0].Name);
    }
    
    [TestMethod]
    public void MapAll_MissingColumn_PropertyKeepsDefault()
    {
        // Arrange — в БД нет колонки Age
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(1, "Иван");
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<User>(reader);
        
        // Assert
        Assert.AreEqual(0, users[0].Age); // значение по умолчанию
    }
    
    [TestMethod]
    public void MapAll_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var table = CreateUserTable();
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<User>(reader);
        
        // Assert
        Assert.IsEmpty(users);
    }
    
    [TestMethod]
    public void MapAll_NullForReferenceType_SetsNull()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(1, DBNull.Value);
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<User>(reader);
        
        // Assert
        Assert.IsNull(users[0].Name);
    }
    
    [TestMethod]
    public void MapAll_NullForValueType_SetsDefault()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Age", typeof(int));
        table.Rows.Add(1, DBNull.Value);
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<User>(reader);
        
        // Assert
        Assert.AreEqual(0, users[0].Age);
    }
    
    [TestMethod]
    public void MapAll_LongToInt_ConvertsCorrectly()
    {
        // Arrange — в БД тип long, в модели int
        var table = new DataTable();
        table.Columns.Add("Id", typeof(long));
        table.Columns.Add("Age", typeof(long));
        table.Rows.Add(1L, 20L);
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<User>(reader);
        
        // Assert
        Assert.AreEqual(20, users[0].Age);
    }
    
    [TestMethod]
    public void MapAll_StringToInt_ConvertsCorrectly()
    {
        // Arrange — в БД строка "20", в модели int
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Age", typeof(string));
        table.Rows.Add(1, "20");
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<User>(reader);
        
        // Assert
        Assert.AreEqual(20, users[0].Age);
    }
    
    [TestMethod]
    public void MapAll_IntToEnum_ConvertsCorrectly()
    {
        // Arrange
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Role", typeof(int));
        table.Rows.Add(1, 1); // 1 = Admin
        using var reader = table.CreateDataReader();
        
        // Act
        var users = EntityMapper.MapAll<UserWithRole>(reader);
        
        // Assert
        Assert.AreEqual(UserRole.Admin, users[0].Role);
    }
    
    [TestMethod]
    public void TryMap_ReturnsFalse_WhenNoRows()
    {
        // Arrange
        var table = CreateUserTable();
        using var reader = table.CreateDataReader();
        
        // Act
        bool result = EntityMapper.TryMap<User>(reader, out var entity);
        
        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(entity);
    }

    [TestMethod]
    public void TryMap_ReturnsTrue_AndFillsEntity()
    {
        // Arrange
        var table = CreateUserTable();
        table.Rows.Add(1, "Иван", 20);
        using var reader = table.CreateDataReader();
        
        // Act
        bool result = EntityMapper.TryMap<User>(reader, out var entity);
        
        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(entity);
        Assert.AreEqual("Иван", entity.Name);
    }

    private static DataTable CreateUserTable()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Age", typeof(int));
        return table;
    }
}