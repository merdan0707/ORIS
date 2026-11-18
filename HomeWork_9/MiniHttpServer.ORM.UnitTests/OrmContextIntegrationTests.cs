using System.Text.Json;
using MiniHttpServer.ORM;
using Npgsql;

namespace MiniHttpServer.ORM.UnitTests;

[DoNotParallelize] 
[TestClass]
public class OrmContextIntegrationTests
{
    private static  string _connectionString = null!;
    private OrmContext _orm = null!;
    
    // 1 раз при старте класса читаем настройки из testsettings.json и создаём таблицы
    [ClassInitialize] 
    public static async Task ClassSetupAsync(TestContext context)
    {
        string configPath = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Файл настроек тестов не найден: {configPath}");
            
        string json = File.ReadAllText(configPath);
        using var doc = JsonDocument.Parse(json);
        _connectionString = doc.RootElement.GetProperty("ConnectionString").GetString() 
                            ?? throw new InvalidOperationException("ConnectionString пуст.");

        // Создание таблицы
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var command = new NpgsqlCommand(
            "DROP TABLE IF EXISTS testusers; CREATE TABLE testusers (id SERIAL PRIMARY KEY, name TEXT, age INT);", conn);
        await command.ExecuteNonQueryAsync();
    }

    [TestInitialize]
    public async Task Setup()
    {
        _orm = new OrmContext(_connectionString);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("TRUNCATE TABLE testusers RESTART IDENTITY CASCADE;", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DROP TABLE IF EXISTS testusers;", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    [TestMethod]
    public async Task InsertAndGet_WorksWithRealDatabase()
    {
        // Arrange
        var user = new TestUser { Name = "Alex", Age = 25 };

        // Act
        int inserted = await _orm.InsertAsync("testusers", user);
        var allUsers = await _orm.GetAllAsync<TestUser>("testusers");

        // Assert
        Assert.AreEqual(1, inserted);
        Assert.HasCount(1, allUsers);
        Assert.AreEqual("Alex", allUsers[0].Name);
        Assert.IsGreaterThan(0, allUsers[0].Id, "База должна присвоить Id");
    }

    [TestMethod]
    public async Task Update_WorksWithRealDatabase()
    {
        // Arrange
        await _orm.InsertAsync("testusers", new TestUser { Name = "Old", Age = 20 });
        var users = await _orm.GetAllAsync<TestUser>("testusers");
        var id = users[0].Id;

        var updatedUser = new TestUser { Name = "New", Age = 30 };

        // Act
        int updated = await _orm.UpdateAsync("testusers", id, updatedUser);
        var userFromDb = await _orm.GetByIdAsync<TestUser>("testusers", id);

        // Assert
        Assert.AreEqual(1, updated);
        Assert.IsNotNull(userFromDb);
        Assert.AreEqual("New", userFromDb.Name);
        Assert.AreEqual(30, userFromDb.Age);
    }

    [TestMethod]
    public async Task Delete_WorksWithRealDatabase()
    {
        // Arrange
        await _orm.InsertAsync("testusers", new TestUser { Name = "ToDelete", Age = 20 });
        var users = await _orm.GetAllAsync<TestUser>("testusers");
        var id = users[0].Id;

        // Act
        int deleted = await _orm.DeleteAsync("testusers", id);
        var userFromDb = await _orm.GetByIdAsync<TestUser>("testusers", id);

        // Assert
        Assert.AreEqual(1, deleted);
        Assert.IsNull(userFromDb, "Запись должна быть удалена");
    }

    // Модель для тестов (должна совпадать с таблицей)
    public class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }
}