using System.Reflection;
using System.Text.RegularExpressions;

namespace MiniHttpServer.ORM.Builders;

public static class SqlQueryBuilder
{
    public record SqlQuery(string Query, Dictionary<string, object?> Parameters);

    public static SqlQuery BuildSelectAll(string tableName)
    {
        string safeTableName = EscapeIdentifier(tableName, toLower: true);
        string sql = $"SELECT * FROM {safeTableName}";
        return new SqlQuery(sql, new Dictionary<string, object?>());
    }

    public static SqlQuery BuildSelectById(string tableName, int id)
    {
        string safeTableName = EscapeIdentifier(tableName, toLower: true);
        string safeIdCol = EscapeIdentifier("Id", toLower: true);
        string sql = $"SELECT * FROM {safeTableName} WHERE {safeIdCol} = @Id";
        return new SqlQuery(sql, new Dictionary<string, object?> { { "@Id", id } });
    }

    public static SqlQuery BuildInsert<T>(string tableName, T entity) where T : class
    {
        string safeTableName = EscapeIdentifier(tableName, toLower: true);
        var properties = GetWritableProperties(typeof(T));

        // Исключаем Id — база сама его сгенерирует
        var insertProps = properties.Where(p => p.Name != "Id").ToArray();
        
        if (insertProps.Length == 0)
            throw new InvalidOperationException($"У модели {typeof(T).Name} нет свойств для вставки.");

        var columns = string.Join(", ", insertProps.Select(p => EscapeIdentifier(p.Name, toLower: true)));
        var parameters = string.Join(", ", insertProps.Select(p => $"@{p.Name}"));

        string sql = $"INSERT INTO {safeTableName} ({columns}) VALUES ({parameters})";
        var paramDict = ToParameters(insertProps, entity);
        return new SqlQuery(sql, paramDict);
    }

    public static SqlQuery BuildUpdate<T>(string tableName, int id, T entity) where T : class
    {
        string safeTableName = EscapeIdentifier(tableName, toLower: true);
        var properties = GetWritableProperties(typeof(T));

        // Id нельзя обновлять
        var updateProps = properties.Where(p => p.Name != "Id").ToArray();

        var setColumns = string.Join(", ",
            updateProps.Select(p => $"{EscapeIdentifier(p.Name, toLower: true)} = @{p.Name}"));

        string idCol = EscapeIdentifier("Id", toLower: true);
        string sql = $"UPDATE {safeTableName} SET {setColumns} WHERE {idCol} = @Id";

        var paramDict = ToParameters(updateProps, entity);
        paramDict["@Id"] = id;
        return new SqlQuery(sql, paramDict);
    }

    public static SqlQuery BuildDelete(string tableName, int id)
    {
        string safeTableName = EscapeIdentifier(tableName, toLower: true);
        string idCol = EscapeIdentifier("Id", toLower: true);
        string sql = $"DELETE FROM {safeTableName} WHERE {idCol} = @Id";
        return new SqlQuery(sql, new Dictionary<string, object?> { { "@Id", id } });
    }

    private static PropertyInfo[] GetWritableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true)
                   .ToArray();
    }

    /// <summary>
    /// Безопасно оборачивает имя таблицы или колонки в двойные кавычки,
    /// чтобы PostgreSQL не привёл его к нижнему регистру автоматически.
    /// Если <paramref name="toLower"/> = true — переводит имя в нижний регистр
    /// (для сопоставления C# PascalCase с PostgreSQL lowercase).
    /// </summary>
    public static string EscapeIdentifier(string identifier, bool toLower = false)
    {
        if (string.IsNullOrWhiteSpace(identifier) ||
            !Regex.IsMatch(identifier, @"^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            throw new ArgumentException("Недопустимое имя SQL-идентификатора.", nameof(identifier));
        }

        return toLower 
            ? $"\"{identifier.ToLowerInvariant()}\"" 
            : $"\"{identifier}\"";
    }
    
    /// <summary>
    /// Собирает словарь параметров @PropName → значение свойства.
    /// </summary>
    private static Dictionary<string, object?> ToParameters(PropertyInfo[] props, object entity)
    {
        var parameters = new Dictionary<string, object?>(props.Length);
        foreach (var p in props)
            parameters[$"@{p.Name}"] = p.GetValue(entity);
        return parameters;
    }
}