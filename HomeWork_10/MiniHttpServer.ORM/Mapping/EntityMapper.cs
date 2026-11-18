using System.Data;
using System.Reflection;

namespace MiniHttpServer.ORM.Mapping;

/// <summary>
/// Сопоставляет строки SQL-запроса со свойствами объекта модели.
/// </summary>
public class EntityMapper
{
    /// <summary>
    /// Заполняет объект значениями из текущей строки reader'а.
    /// Возвращает true, если удалось прочитать строку; false — если строк больше нет.
    /// </summary>
    public static bool TryMap<T>(IDataReader reader, out T? entity)
        where T : class, new()
    {
        entity = null;

        if (!reader.Read())
            return false;

        entity = new T();
        var properties = GetWritableProperties(typeof(T));

        // Строим словарь "имя колонки → индекс в reader" один раз
        var columns = GetColumnIndices(reader);

        foreach (var propertyInfo in properties)
        {
            if (!columns.TryGetValue(propertyInfo.Name, out int ordinal))
                continue;
            
            propertyInfo.SetValue(entity, ReadValue(reader, ordinal, propertyInfo.PropertyType));
        }

        return true;
    }

    /// <summary>
    /// Читает все оставшиеся строки reader'а и возвращает список объектов.
    /// </summary>
    public static IReadOnlyList<T> MapAll<T>(IDataReader reader)
        where T : class, new()
    {
        var result = new List<T>();
        while (TryMap(reader, out T? entity) && entity != null)
            result.Add(entity);

        return result;
    }

    private static PropertyInfo[] GetWritableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p is { CanWrite: true, SetMethod.IsPublic: true })
            .ToArray();
    }

    private static Dictionary<string, int> GetColumnIndices(IDataReader reader)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < reader.FieldCount; i++)
            columns[reader.GetName(i)] = i;
        
        return columns;
    }

    private static object? ReadValue(IDataReader reader, int ordinal, Type targetType)
    {
        if (reader.IsDBNull(ordinal))
        {
            // Для value-типов (int, bool) без nullable — возвращаем дефолт
            // Для ссылочных и nullable — null
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                return null;
            return Activator.CreateInstance(targetType);
        }

        object dbValue = reader.GetValue(ordinal);
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Если тип уже совпадает — возвращаем как есть
        if (underlyingType.IsInstanceOfType(dbValue))
            return dbValue;

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, dbValue);

        // Иначе — стандартная конвертация
        return Convert.ChangeType(dbValue, underlyingType);
    }
}