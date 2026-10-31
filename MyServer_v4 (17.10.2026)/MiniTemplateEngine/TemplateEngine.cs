using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Text;

namespace MiniTemplateEngine;

/// <summary>
/// Конкретная реализация движка шаблонизации.
/// Поддерживает переменные ${...}, условия $if(...) ... $else ... $endif и циклы $foreach(... in ...) ... $endfor.
/// </summary>
public class TemplateEngine : ITemplateEngine
{
    /// <summary>
    /// Рендерит шаблон из строки, подставляя значения из модели.
    /// </summary>
    public string RenderFromString(string template, object model)
    {
        // Создаем контекст выполнения на основе переданной модели
        var context = CreateContext(model);
        // Запускаем рекурсивную обработку шаблона
        return ProcessTemplate(template, context);
    }

    /// <summary>
    /// Рендерит шаблон из файла.
    /// </summary>
    public string RenderFromFile(string filePath, object model)
    {
        // Проверяем существование файла
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл шаблона не найден: {filePath}");
        
        // Читаем содержимое файла
        string template = File.ReadAllText(filePath);
        // Вызываем рендеринг из строки
        return RenderFromString(template, model);
    }

    /// <summary>
    /// Рендерит шаблон из файла и сохраняет результат в новый файл.
    /// </summary>
    public string RenderToFile(string inputFilePath, string outputFilePath, object model)
    {
        // Получаем отрендеренную строку
        string result = RenderFromFile(inputFilePath, model);
        
        // Получаем директорию выходного файла
        string? directory = Path.GetDirectoryName(Path.GetFullPath(outputFilePath));
        // Создаём директорию, если её нет
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Записываем результат в файл
        File.WriteAllText(outputFilePath, result);

        return result;
    }

    /// <summary>
    /// Создает контекст выполнения (словарь переменных) на основе свойств переданного объекта модели.
    /// Использует рефлексию для получения свойств.
    /// </summary>
    private IDictionary<string, object> CreateContext(object model)
    {
        // Создаем динамический объект в виде словаря
        IDictionary<string, object> context = new ExpandoObject();
        
        // Если модель не null, извлекаем её свойства
        if (model != null)
        {
            // Получаем все публичные свойства типа модели через рефлексию
            foreach (PropertyInfo propertyInfo in model.GetType().GetProperties())
            {
                // Добавляем имя свойства и его значение в контекст
                context[propertyInfo.Name] = propertyInfo.GetValue(model) ?? string.Empty;
            }
        }

        // Возвращаем заполненный контекст
        return context;
    }

    /// <summary>
    /// Основной метод рекурсивной обработки шаблона.
    /// </summary>
    private string ProcessTemplate(string template, IDictionary<string, object> context)
    {
        // StringBuilder для накопления результата
        var result = new StringBuilder();
        // Текущая позиция в строке шаблона
        int pos = 0;

        // Цикл обработки шаблона до конца строки
        while (pos < template.Length)
        {
            // Ищем индексы начала всех возможных тегов
            int varIndex = template.IndexOf("${", pos, StringComparison.Ordinal);
            int ifIndex = template.IndexOf("$if(", pos, StringComparison.OrdinalIgnoreCase);
            int foreachIndex = template.IndexOf("$foreach(", pos, StringComparison.OrdinalIgnoreCase);

            // Находим минимальный индекс (ближайший тег)
            int minIndex = -1;
            // Перебираем все найденные индексы
            foreach (int index in new[] {varIndex, ifIndex, foreachIndex})
            {
                // Если индекс валиден и меньше текущего минимального (или минимальный еще не найден)
                if (index != -1 && (minIndex == -1 || index < minIndex))
                    minIndex = index; // Обновляем минимальный индекс
            }
            
            // Если теги больше не встречаются
            if (minIndex == -1)
            {
                // Добавляем оставшийся текст шаблона в результат
                result.Append(template.Substring(pos));
                // Прерываем цикл
                break;
            }
            
            // Добавляем обычный текст от текущей позиции до начала найденного тега
            result.Append(template, pos, minIndex - pos);
            
            // Если ближайший тег - это переменная ${...}
            if (minIndex == varIndex)
            {
                // Ищем закрывающую скобку }
                int endIndex = template.IndexOf("}", minIndex + 2, StringComparison.Ordinal);
                // Если скобка не найдена, выбрасываем исключение
                if (endIndex == -1)
                    throw new Exception("Синтаксическая ошибка: не найден закрывающий '}' для '${'");

                // Извлекаем имя переменной или выражение
                string expression = template.Substring(minIndex + 2, endIndex - minIndex - 2).Trim();
                // Вычисляем значение выражения через контекст и рефлексию
                object? value = EvaluateExpression(expression, context);
                // Добавляем строковое представление значения в результат (или пустую строку, если null)
                result.Append(value?.ToString() ?? "");
                // Сдвигаем позицию за закрывающую скобку
                pos = endIndex + 1;
            }
            // Если ближайший тег - это условие $if(...)
            else if (minIndex == ifIndex)
            {
                // Ищем закрывающую скобку ) для условия
                int closeBracketIndex = template.IndexOf(")", minIndex + 4, StringComparison.Ordinal);
                // Если скобка не найдена, выбрасываем исключение
                if (closeBracketIndex == -1)
                    throw new Exception("Синтаксическая ошибка: не найден ')' для '$if('");
                
                // Извлекаем условие
                string condition = template.Substring(minIndex + 4, closeBracketIndex - minIndex - 4).Trim();
                
                // Ищем тег $else на текущем уровне вложенности
                int elseIndex = FindTagAtDepth1(template, closeBracketIndex + 1, "$else", "$if(", "$endif");
                // Ищем парный закрывающий тег $endif
                int endifIndex = FindMatchingTag(template, closeBracketIndex + 1, "$if(", "$endif");
                
                // Проверяем истинность условия
                bool isTrue = IsConditionTrue(condition, context);
                
                // Определяем тело блока, которое нужно выполнить
                string body;
                if (isTrue)
                {
                    // Если условие истинно, берем текст до $else (или до $endif, если $else нет)
                    body = elseIndex != -1
                        ? template.Substring(closeBracketIndex + 1, elseIndex - closeBracketIndex - 1)
                        : template.Substring(closeBracketIndex + 1, endifIndex - closeBracketIndex - 1);
                }
                else
                {
                    // Если условие ложно, берем текст после $else (или пустую строку, если $else нет)
                    body = elseIndex != -1
                        ? template.Substring(elseIndex + 5, endifIndex - elseIndex - 5)
                        : "";
                }
                
                // Рекурсивно обрабатываем выбранное тело блока
                result.Append(ProcessTemplate(body, context));
                // Сдвигаем позицию за тег $endif (длина "$endif" = 6)
                pos = endifIndex + 6;
            }
            // Если ближайший тег - это цикл $foreach(... in ...)
            else if (minIndex == foreachIndex)
            {
                // Ищем закрывающую скобку )
                int closeBracketIndex = template.IndexOf(")", minIndex + 9, StringComparison.Ordinal);
                // Если скобка не найдена, выбрасываем исключение
                if(closeBracketIndex == -1)
                    throw new Exception("Синтаксическая ошибка: не найден ')' для '$foreach('");
                
                // Извлекаем внутреннее выражение цикла (например, "item in items")
                string inner = template.Substring(minIndex + 9, closeBracketIndex - minIndex - 9).Trim();
                // Разбиваем выражение по ключевому слову " in "
                string[] parts = inner.Split(new[] { " in " }, StringSplitOptions.RemoveEmptyEntries);
                // Если формат неверный, выбрасываем исключение
                if (parts.Length != 2)
                    throw new Exception("Синтаксическая ошибка в $foreach. Ожидается формат: $foreach(переменная in коллекция)");
                
                // Имя переменной для текущего элемента цикла
                string varName = parts[0].Trim();
                // Выражение для получения коллекции
                string collectionExpression = parts[1].Trim();
                
                // Вычисляем значение коллекции через контекст
                object? collectionObject = EvaluateExpression(collectionExpression, context);
                // Приводим к IEnumerable для итерации
                IEnumerable? collection = collectionObject as IEnumerable;
                // Если результат не является коллекцией, выбрасываем исключение
                if (collection == null)
                    throw new Exception($"Ошибка: выражение '{collectionExpression}' не возвращает коллекцию");
                
                // Ищем парный закрывающий тег $endfor
                int endforIndex = FindMatchingTag(template, closeBracketIndex + 1, "$foreach(", "$endfor");
                // Извлекаем тело цикла
                string body = template.Substring(closeBracketIndex + 1, endforIndex - closeBracketIndex - 1);
                
                // Итерируемся по элементам коллекции
                foreach (object? item in collection)
                {
                    // Создаем новый контекст, копируя текущий и добавляя переменную цикла
                    IDictionary<string, object> newContext = new Dictionary<string, object>(context, StringComparer.OrdinalIgnoreCase)
                    {
                        [varName] = item ?? string.Empty // Добавляем текущий элемент в контекст
                    };
                    // Рекурсивно обрабатываем тело цикла с новым контекстом
                    result.Append(ProcessTemplate(body, newContext));
                }
                
                // Сдвигаем позицию за тег $endfor (длина "$endfor" = 7)
                pos = endforIndex + 7;
            }
        }
        
        // Возвращаем итоговую собранную строку
        return result.ToString();
    }

    /// <summary>
    /// Вычисляет значение выражения (например, "user.Name" или просто "Name") используя контекст и рефлексию.
    /// </summary>
    private object? EvaluateExpression(string expression, IDictionary<string, object> context)
    {
        // Разбиваем выражение по точкам для поддержки вложенных свойств
        string[] parts = expression.Split('.');
        // Пытаемся получить первый элемент из контекста
        object? current = context.TryGetValue(parts[0], out object? value)
            ? value
            : null;
        
        // Если частей больше одной, идем вглубь по свойствам объекта
        for (int i = 1; i < parts.Length; i++)
        {
            // Если текущий объект null, дальнейший поиск невозможен
            if (current == null) return null;
            // Получаем тип текущего объекта
            Type type = current.GetType();
            // Ищем публичное свойство по имени через рефлексию
            PropertyInfo? propertyInfo = type.GetProperty(parts[i]);
            // Если свойство не найдено, возвращаем null
            if (propertyInfo == null) return null;
            // Получаем значение свойства
            current = propertyInfo.GetValue(current);
        }
        
        // Возвращаем вычисленное значение
        return current;
    }

    /// <summary>
    /// Проверяет условие на истинность.
    /// </summary>
    private bool IsConditionTrue(string condition, IDictionary<string, object> context)
    {
        // Вычисляем значение выражения условия
        object? value = EvaluateExpression(condition, context);
        // Если значение null, условие ложно
        if (value == null) return false;
        // Если это bool, возвращаем его значение
        if (value is bool b) return b;
        // Если это строка, проверяем, что она не пустая
        if (value is string s) return !string.IsNullOrEmpty(s);
        // Если это коллекция, проверяем, что она содержит элементы
        if (value is IEnumerable enumerable) return enumerable.Cast<object>().Any();
        // Если это число, проверяем, что оно не равно нулю
        if (value is int i) return i != 0;
        // Во всех остальных случаях считаем условие истинным
        return true;
    }

    /// <summary>
    /// Ищет парный закрывающий тег с учетом вложенности открывающих тегов.
    /// </summary>
    private int FindMatchingTag(string template, int startIndex, string openTag, string closeTag)
    {
        // Начальная глубина вложенности (мы уже внутри одного открывающего тега)
        int depth = 1;
        // Начальный индекс поиска
        int index = startIndex;
        
        // Цикл поиска до конца шаблона
        while (index < template.Length)
        {
            // Ищем следующий открывающий тег
            int nextOpen = template.IndexOf(openTag, index, StringComparison.OrdinalIgnoreCase);
            // Ищем следующий закрывающий тег
            int nextClose = template.IndexOf(closeTag, index, StringComparison.OrdinalIgnoreCase);

            // Если закрывающий тег не найден, выбрасываем исключение
            if (nextClose == -1) throw new Exception($"Синтаксическая ошибка: не найден парный закрывающий тег {closeTag}");

            // Если открывающий тег найден раньше закрывающего
            if (nextOpen != -1 && nextOpen < nextClose)
            {
                depth++; // Увеличиваем глубину вложенности
                index = nextOpen + openTag.Length; // Сдвигаем индекс
            }
            else
            {
                depth--; // Уменьшаем глубину вложенности
                if (depth == 0) return nextClose; // Если глубина 0, мы нашли парный тег
                index = nextClose + closeTag.Length; // Сдвигаем индекс
            }
        }
        // Если цикл завершился, а парный тег не найден
        throw new Exception($"Синтаксическая ошибка: не найден парный закрывающий тег {closeTag}");
    }
    
    /// <summary>
    /// Ищет тег (например, $else) строго на первом уровне вложенности.
    /// </summary>
    private int FindTagAtDepth1(string template, int startIndex, string targetTag, string openTag, string closeTag)
    {
        // Начальная глубина вложенности
        int depth = 1;
        // Начальный индекс поиска
        int index = startIndex;
        
        // Цикл поиска
        while (index < template.Length)
        {
            // Ищем следующие вхождения тегов
            int nextOpen = template.IndexOf(openTag, index, StringComparison.OrdinalIgnoreCase);
            int nextClose = template.IndexOf(closeTag, index, StringComparison.OrdinalIgnoreCase);
            int nextTarget = template.IndexOf(targetTag, index, StringComparison.OrdinalIgnoreCase);

            int minIdx = -1;
            string minType = null;
            
            // Находим ближайшее вхождение любого из тегов
            foreach (var (idx, type) in new[] { (nextOpen, "open"), (nextClose, "close"), (nextTarget, "target") })
            {
                if (idx != -1 && (minIdx == -1 || idx < minIdx))
                {
                    minIdx = idx;
                    minType = type;
                }
            }

            // Если ни один тег не найден, возвращаем -1
            if (minIdx == -1) return -1;

            // Обновляем глубину в зависимости от типа найденного тега
            if (minType == "open") depth++;
            else if (minType == "close") depth--;
            // Если найден целевой тег и мы на первом уровне вложенности
            else if (minType == "target" && depth == 1) return minIdx;

            // Сдвигаем индекс
            index = minIdx + (minType == "open" ? openTag.Length : minType == "close" ? closeTag.Length : targetTag.Length);
        }
        // Если тег не найден
        return -1;
    }
}