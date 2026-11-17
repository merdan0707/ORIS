using System.Collections;
using System.Data;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;

namespace MiniHttpServer.HtmlTemplating;

/// <summary>
/// Движок шаблонизации.
/// Поддерживает: ${...}, $if(...) ... $else ... $endif, $foreach(... in ...) ... $endfor.
/// </summary>
public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    /// <summary>
    /// Рендерит шаблон из строки, подставляя значения из модели.
    /// </summary>
    public string RenderFromString(string template, object model)
    {
        IDictionary<string, object> context = CreateContext(model);
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

        // Читаем содержимое файла затем вызываем рендеринг из строки
        string template = File.ReadAllText(filePath);
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
        // Если директория существует и не пуста, создаем её
        // Если не сущ. то мы либо в корне, либо надо создать в текущей директории
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Записываем результат в файл и затем возвращаем результат
        File.WriteAllText(outputFilePath, result);
        return result;
    }

    /// <summary>
    /// Создает контекст выполнения (словарь переменных) на основе свойств переданного объекта модели.
    /// Ключ — имя свойства, значение — его значение.
    /// Использует рефлексию для получения свойств.
    /// </summary>
    private IDictionary<string, object> CreateContext(object? model)
    {
        // StringComparer.OrdinalIgnoreCase — имена переменных в шаблоне нечувствительны к регистру
        var context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Если модель не передана, контекст остаётся пустым — все ${...} в шаблоне отрендерятся как пустые строки
        if (model != null)
        {
            // Получаем все публичные свойства типа модели через рефлексию
            foreach (PropertyInfo propertyInfo in model.GetType().GetProperties())
            {
                // Читаем значение свойства и кладём в словарь под именем свойства.
                // если значение null, подставляем пустую строку
                context[propertyInfo.Name] = propertyInfo.GetValue(model) ?? string.Empty;
            }
        }

        return context;
    }

    /// <summary>
    /// Рекурсивно обрабатывает шаблон.
    /// </summary>
    private string ProcessTemplate(string template, IDictionary<string, object> context)
    {
        // Готовый результат рендеринга.
        var result = new StringBuilder();
        // Текущая позиция чтения в шаблоне. Всё, что левее pos, уже обработано.
        int currentPos = 0;

        // Идём по шаблону, пока не дойдём до конца.
        while (currentPos < template.Length)
        {
            // Ищем ближайший тег (${, $if(, $foreach() начиная с текущей позиции.
            int tagPos = FindNextTag(template, currentPos, out string tagType);

            // Тегов больше нет — добавляем остаток и выходим
            if (tagPos == -1)
            {
                result.Append(template, currentPos, template.Length - currentPos);
                break;
            }

            result.Append(template, currentPos, tagPos - currentPos); // Копируем текст перед тегом

            // Обрабатываем тег, получая новую позицию
            currentPos = tagType switch
            {
                "var" => RenderVariable(template, tagPos, context, result),
                "if" => RenderIf(template, tagPos, context, result),
                "foreach" => RenderForeach(template, tagPos, context, result),
                _ => throw new InvalidOperationException($"Неизвестный тип тега: {tagType}")
            };
        }

        return result.ToString();
    }

    /// <summary>
    /// Находит ближайший тег (${, $if(, $foreach() начиная с позиции startPos.
    /// </summary>
    /// <param name="template">Исходный шаблон</param>
    /// <param name="startPos">Позиция откуда начинается поиск тега.</param>
    /// <param name="tagType">Найденный тег: "var", "if" или "foreach".</param>
    /// <returns>Индекс найденного тега или -1, если ни одного тега не осталось.</returns>
    private int FindNextTag(string template, int startPos, out string? tagType)
    {
        // Ищем позицию каждого из трёх тегов независимо от остальных (-1, если тег не найден)
        int varPos = template.IndexOf("${", startPos, StringComparison.Ordinal);
        int ifPos = template.IndexOf("$if(", startPos, StringComparison.OrdinalIgnoreCase);
        int foreachPos = template.IndexOf("$foreach(", startPos, StringComparison.OrdinalIgnoreCase);

        // Берём ${...} за «ближайший тег» (если ${...} не найден то varPos = -1)
        int nearestTag = varPos;
        tagType = varPos == -1 ? null : "var";

        // если $if( ближе всех остальных
        if (ifPos != -1 && (nearestTag == -1 || ifPos < nearestTag))
        {
            nearestTag = ifPos;
            tagType = "if";
        }

        // если $foreach( ближе всех остальных
        if (foreachPos != -1 && (nearestTag == -1 || foreachPos < nearestTag))
        {
            nearestTag = foreachPos;
            tagType = "foreach";
        }

        return nearestTag;
    }

    /// <summary>
    /// Обрабатывает тег переменной <c>${выражение}</c>.
    /// Вычисляет выражение через контекст и дописывает результат в буфер.
    /// </summary>
    /// <param name="template">Исходный шаблон, в котором ведётся поиск.</param>
    /// <param name="tagPos">Индекс начала тега (позиция символа '$').</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <param name="result">Буфер, в который дописывается отрендеренное значение.</param>
    /// <returns>Позиция после '}'.</returns>
    private int RenderVariable(string template, int tagPos, IDictionary<string, object> context, StringBuilder result)
    {
        // Ищем закрывающую '}'. Длина "${" = 2
        int endPos = template.IndexOf("}", tagPos + 2, StringComparison.Ordinal);
        if (endPos == -1)
            throw new SyntaxErrorException("Синтаксическая ошибка: не найдена закрывающая скобка '}' для '${'");

        // Извлекаем выражение между "${" и "}"
        string expression = template.Substring(tagPos + 2, endPos - tagPos - 2).Trim();
        // Вычисляем значение выражения по контексту
        object? value = EvaluateExpression(expression, context);
        // Дописываем строковое представление значения
        result.Append(WebUtility.HtmlEncode(value?.ToString() ?? ""));

        // Сдвигаем за } (1 символ)
        // Следующая итерация ProcessTemplate продолжит с этого места
        return endPos + 1;
    }

    /// <summary>
    /// Обрабатывает условный блок <c>$if(условие) ... $else ... $endif</c>.
    /// </summary>
    /// <param name="template">Исходный шаблон, в котором ведётся поиск.</param>
    /// <param name="tagPos">Индекс начала тега.</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <param name="result">Буфер, в который дописывается отрендеренный текст.</param>
    /// <returns>Позиция после <c>$endif</c>.</returns>
    private int RenderIf(string template, int tagPos, IDictionary<string, object> context, StringBuilder result)
    {
        // Ищем закрывающую скобку ')' 
        // 4 = длина "$if("
        int condEnd = template.IndexOf(")", tagPos + 4, StringComparison.Ordinal);
        if (condEnd == -1)
            throw new SyntaxErrorException("Синтаксическая ошибка: не найдена закрывающая скобка ')' для '$if('");

        // Извлекаем само условие между "$if(" и ")"
        string condition = template.Substring(tagPos + 4, condEnd - tagPos - 4).Trim();
        // Ищем $else строго на первом уровне вложенности
        int elsePos = FindTagAtDepth1(template, condEnd + 1, "$else", "$if(", "$endif");
        // Ищем парный $endif с учётом вложенности
        int endifPos = FindMatchingTag(template, condEnd + 1, "$if(", "$endif");

        // Вычисляем, истинно ли условие
        bool isTrue = IsConditionTrue(condition, context);

        // Выбираем тело блока, которое нужно отрендерить
        string body;
        if (isTrue)
        {
            // Условие истинно — берём текст до $else или до $endif
            int bodyEnd = elsePos != -1 ? elsePos : endifPos;
            body = template.Substring(condEnd + 1, bodyEnd - condEnd - 1);
        }
        else
        {
            // Условие ложно — берём текст после $else до $endif
            body = elsePos != -1
                ? template.Substring(elsePos + 5, endifPos - elsePos - 5) // 5 = длина "$else"
                : "";
        }

        // Рекурсивно рендерим выбранную ветку
        result.Append(ProcessTemplate(body, context));
        // Сдвигаем за $endif (6 = длина "$endif")
        // Следующая итерация ProcessTemplate продолжит с этого места
        return endifPos + 6;
    }

    /// <summary>
    /// Обрабатывает цикл <c>$foreach(переменная in коллекция) ... $endfor</c>.
    /// Вычисляет коллекцию, для каждого элемента создаёт дочерний контекст
    /// с переменной цикла и рекурсивно рендерит тело, дописывая результат в <paramref name="result"/>.
    /// </summary>
    /// <param name="template">Исходный шаблон, в котором ведётся поиск.</param>
    /// <param name="tagPos">Индекс начала тега.</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <param name="result">Буфер, в который дописывается отрендеренный текст.</param>
    /// <returns>Позиция после <c>$endfor</c>.</returns>
    private int RenderForeach(string template, int tagPos, IDictionary<string, object> context, StringBuilder result)
    {
        // Ищем закрывающую скобку ')' заголовка цикла. Длина "$foreach(" = 9
        int bracketEnd = template.IndexOf(")", tagPos + 9, StringComparison.Ordinal);
        if (bracketEnd == -1)
            throw new SyntaxErrorException("Синтаксическая ошибка: не найдена закрывающая скобка ')' для '$foreach('");

        // Извлекаем внутренность заголовка между "$foreach(" и ")"
        string inner = template.Substring(tagPos + 9, bracketEnd - tagPos - 9).Trim();
        // Разбиваем по ключевому слову " in " на две части: имя переменной и выражение коллекции
        string[] parts = inner.Split(" in ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new SyntaxErrorException(
                "Синтаксическая ошибка в $foreach. Ожидается формат: $foreach(переменная in коллекция)");

        // Левая часть — имя переменной цикла. Правая — выражение для получения коллекции
        string varName = parts[0].Trim();
        if (varName.StartsWith("var ", StringComparison.Ordinal))
            varName = varName.Substring(4).Trim();
        string collectionExpr = parts[1].Trim();

        // Вычисляем значение коллекции по контексту
        object? collectionObj = EvaluateExpression(collectionExpr, context);
        // Приводим к IEnumerable, чтобы итерироваться через foreach
        IEnumerable? collection = collectionObj as IEnumerable;
        if (collection == null)
            throw new InvalidOperationException($"Ошибка: выражение '{collectionExpr}' не возвращает коллекцию");

        // Ищем парный $endfor с учётом вложенности
        int endforPos = FindMatchingTag(template, bracketEnd + 1, "$foreach(", "$endfor");
        // Тело цикла — всё между ')' заголовка и $endfor
        string body = template.Substring(bracketEnd + 1, endforPos - bracketEnd - 1);

        foreach (object? item in collection)
        {
            // Создаём дочерний контекст (копию текущего плюс переменная цикла) для каждого элемента коллекции
            var childContext = new Dictionary<string, object>(context, StringComparer.OrdinalIgnoreCase)
            {
                // если элемент null, подставляем пустую строку
                [varName] = item ?? string.Empty
            };
            // Рекурсивно рендерим тело с дочерним контекстом
            result.Append(ProcessTemplate(body, childContext));
        }

        // Сдвигаем за $endfor: длина "$endfor" = 7
        // Следующая итерация ProcessTemplate продолжит с этого места
        return endforPos + 7;
    }

    /// <summary>
    /// Вычисляет выражение шаблона вида <c>name</c> или <c>user.Address.City</c> через рефлексию.
    /// </summary>
    /// <param name="expression">Выражение: имя переменной, возможно с цепочкой свойств через точку.</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <returns>Значение выражения или <c>null</c>, если его получить не удалось.</returns>
    private object? EvaluateExpression(string expression, IDictionary<string, object> context)
    {
        // Разбиваем выражение по точкам
        string[] parts = expression.Split('.');

        // По имени переменной пытаемся получить объект из контекста
        object? current = context.TryGetValue(parts[0], out object? value) ? value : null;

        // Проходим по оставшимся сегментам (свойствам) через рефлексию
        for (int i = 1; i < parts.Length; i++)
        {
            // Если на предыдущем шаге получили null, то возвращаем null
            if (current == null)
                return null;

            // Ищем публичное свойство с именем текущего объекта
            PropertyInfo? propertyInfo = current.GetType().GetProperty(parts[i]);
            if (propertyInfo == null)
                return null;

            // Читаем значение свойства и переходим на следующий уровень
            current = propertyInfo.GetValue(current);
        }

        // Возвращаем итоговое значение
        return current;
    }

    #region ConditionExpression

    /// <summary>
    /// Проверяет условие из выражения $if(...).
    /// Поддерживает:
    ///   • одиночное выражение — проверяется «истинность» значения: $if(IsActive), $if(Items)
    ///   • сравнение: ==, !=, &gt;, &lt;, &gt;=, &lt;=
    ///   • отрицание: !
    ///   • логическое И: &amp;&amp;
    ///   • логическое ИЛИ: ||
    ///   • арифметику: +, -, *, /, % и унарный минус
    /// Приоритет операторов (высший → низший):
    ///   !  →  * / %  →  + -  →  == != &gt; &lt; &gt;= &lt;=  →  &amp;&amp;  →  ||
    /// Скобки (группировка) НЕ поддерживаются.
    /// </summary>
    /// <param name="condition">Строка условия из $if(...).</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <returns>true, если условие истинно; иначе false.</returns>
    private bool IsConditionTrue(string condition, IDictionary<string, object> context)
    {
        // Верхний уровень — логическое ИЛИ (наименьший приоритет)
        return EvaluateOr(condition, context);
    }

    // ───────────────────────── Логические уровни ─────────────────────────

    /// <summary>
    /// Вычисляет логическое ИЛИ (||) — верхний уровень приоритета.
    /// Разбивает выражение по последнему (правому) || и рекурсивно вычисляет обе части.
    /// Если || в выражении нет — переходит на уровень ниже (EvaluateAnd).
    /// </summary>
    /// <param name="expression">Выражение условия из $if(...).</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <returns>true, если хотя бы одна часть истинна; иначе false.</returns>
    private bool EvaluateOr(string expression, IDictionary<string, object> context)
    {
        // Ищем разделитель "||"
        int idx = FindTopLevel(expression, "||");
        if (idx >= 0)
        {
            // Разбиваем на левую и правую части по найденному ||
            string left = expression.Substring(0, idx).Trim();
            string right = expression.Substring(idx + 2).Trim();

            // Рекурсивно вычисляем обе части и объединяем через ||
            return EvaluateOr(left, context) || EvaluateOr(right, context);
        }

        // || не найден, передаём управление на уровень ниже — EvaluateAnd
        return EvaluateAnd(expression, context);
    }

    /// <summary>
    /// Вычисляет логическое И (&amp;&amp;)
    /// Разбивает выражение по последнему (правому) &amp;&amp; и рекурсивно вычисляет обе части.
    /// Если &amp;&amp; в выражении нет — переходит на уровень ниже (EvaluateComparisonOrTruthy).
    /// </summary>
    /// <param name="expression">Выражение условия из $if(...).</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <returns>true, если обе части истинны; иначе false.</returns>
    private bool EvaluateAnd(string expression, IDictionary<string, object> context)
    {
        // Ищем разделитель "&&"
        int idx = FindTopLevel(expression, "&&");
        if (idx >= 0)
        {
            // Разбиваем на левую и правую части по найденному &&
            string left = expression.Substring(0, idx).Trim();
            string right = expression.Substring(idx + 2).Trim();
            
            // Рекурсивно вычисляем обе части и объединяем через &&
            return EvaluateAnd(left, context) && EvaluateAnd(right, context);
        }

        // && не найден, передаём управление на уровень ниже — EvaluateComparisonOrTruthy
        return EvaluateComparisonOrTruthy(expression, context);
    }

    /// <summary>
    /// Вычисляет то, что остаётся после разбиения по || и &amp;&amp;.
    /// Поддерживает три формы:
    ///   1) отрицание: !выражение
    ///   2) сравнение: левая часть оператор правая часть  (==, !=, &gt;, &lt;, &gt;=, &lt;=)
    ///   3) одиночное выражение — проверяется «истинность» значения (truthiness)
    /// Является нижним уровнем цепочки логических операторов:
    ///   EvaluateOr → EvaluateAnd → EvaluateComparisonOrTruthy.
    /// </summary>
    /// <param name="expression">Часть условия без || и &amp;&amp; (может начинаться с !).</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <returns>true, если эта часть истинна; иначе false.</returns>
    private bool EvaluateComparisonOrTruthy(string expression, IDictionary<string, object> context)
    {
        // Убираем пробелы по краям
        expression = expression.Trim();
        
        // Ищем отрицание, применяется ко всему, что справа: !a == b → !(a == b)
        if (expression.StartsWith('!'))
            return !EvaluateComparisonOrTruthy(expression.Substring(1).Trim(), context);

        // Ищем сравнение (==, !=, >, <, >=, <=)
        if (TryEvaluateComparison(expression, context, out bool result))
            return result;
        
        // Иначе — одиночное выражение, проверяем «истинность» его значения
        return IsTruthy(EvaluateArithmetic(expression, context));
    }
    
    // ───────────────────────── Сравнения ─────────────────────────

    /// <summary>
    /// Пытается разобрать условие как сравнение и вычислить его.
    /// </summary>
    /// <param name="condition">Строка условия без || и &amp;&amp; (может содержать арифметику и литералы).</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <param name="result">Итог сравнения. При возврате false значение не определено.</param>
    /// <returns>
    /// true — условие является сравнением, result будет итогом сравнения;
    /// false — оператора сравнения нет.
    /// </returns>
    private bool TryEvaluateComparison(string condition, IDictionary<string, object> context, out bool result)
    {
        // Инициализируем out-параметр
        result = false;
        
        // Пытаемся найти оператор сравнения и разбить условие на три части
        if (!TrySplitComparison(condition, out string leftExpr, out string op, out string rightExpr))
            return false;
        
        // Вычисляем левую и правую части
        object? left = EvaluateArithmetic(leftExpr, context);
        object? right = EvaluateArithmetic(rightExpr, context);
        // Сравниваем значения указанным оператором
        result = CompareValues(left, op, right);
        
        // Оператор найден и сравнение выполнено
        return true;
    }

    /// <summary>
    /// Ищет оператор сравнения в строке и разбивает условие на три части: левый операнд, оператор и правый операнд.
    /// </summary>
    /// <param name="condition">Строка условия (после разбиения по || и &&).</param>
    /// <param name="left">Левая часть выражения до оператора (без пробелов по краям).</param>
    /// <param name="op">Найденный оператор: "==", "!=", "&gt;", "&lt;", "&gt;=" или "&lt;=".</param>
    /// <param name="right">Правая часть выражения после оператора (без пробелов по краям).</param>
    /// <returns>true, если оператор найден; иначе false (все out-параметры — пустые строки).</returns>
    private bool TrySplitComparison(string condition, out string left, out string op, out string right)
    {
        // Порядок ВАЖЕН: двухсимвольные операторы проверяются раньше односимвольных
        string[] operators = { ">=", "<=", "==", "!=", ">", "<" };

        // Флаг то что сравнение внутри строкового литерала
        bool inStr = false;
        // Тип кавычки (двойная или одинарная)
        char strCh = '\0';
        
        // Посимвольно идём по строке
        for (int i = 0; i < condition.Length; i++)
        {
            char c = condition[i];

            // Содержимое строковых литералов пропускаем целиком
            if (inStr)
            {
                if (c == strCh) inStr = false;
                continue;
            }

            // Запоминаем, какой кавычкой открыт литерал
            if (c is '"' or '\'')
            {
                inStr = true;
                strCh = c;
                continue;
            }

            // Пробуем каждый оператор по порядку (двухсимвольные — первыми)
            foreach (string candidate in operators)
            {
                // Оператор не помещается в остаток строки
                if (i + candidate.Length > condition.Length) continue;
                // Сравниваем подстроку, начинающуюся с i, с candidate
                if (string.Compare(condition, i, candidate, 0, candidate.Length, StringComparison.Ordinal) != 0)
                    continue;

                // Оператор найден в позиции i. Разбиваем строку на части до и после оператора
                string l = condition.Substring(0, i).Trim();
                string r = condition.Substring(i + candidate.Length).Trim();
                // Обе части должны быть непустыми
                if (l.Length > 0 && r.Length > 0)
                {
                    left = l;
                    op = candidate;
                    right = r;
                    return true;
                }
            }
        }

        // Оператор не найден, обнуляем out-параметры
        left = op = right = "";
        return false;
    }

    /// <summary>
    /// Сравнивает два значения указанным оператором.
    /// Поддерживает операторы: ==, !=, &gt;, &lt;, &gt;=, &lt;=.
    /// Правила сравнения:
    ///   • == и != — работают и с null; числа сравниваются как числа,
    ///     остальное — через Equals (строки, bool и т.п.).
    ///   • &gt;, &lt;, &gt;=, &lt;= — только для не-null значений;
    ///     если оба числа — числовое сравнение,
    ///     иначе — строковое сравнение (Ordinal).
    /// </summary>
    /// <param name="left">Левое значение (результат EvaluateArithmetic).</param>
    /// <param name="op">Оператор сравнения: "==", "!=", "&gt;", "&lt;", "&gt;=", "&lt;=".</param>
    /// <param name="right">Правое значение (результат EvaluateArithmetic).</param>
    /// <returns>Результат сравнения; false для неподдерживаемого оператора.</returns>
    private bool CompareValues(object? left, string op, object? right)
    {
        switch (op)
        {
            // Проверка на равенство/неравенство
            case "==":
                return AreEqual(left, right);
            case "!=":
                return !AreEqual(left, right);
        }

        // Для >, <, >=, <= оба значения должны быть не null
        if (left == null || right == null) return false;

        // Если оба — числа, сравниваем как числа
        if (TryToDouble(left, out double ln) && TryToDouble(right, out double rn))
        {
            return op switch
            {
                ">" => ln > rn,
                "<" => ln < rn,
                ">=" => ln >= rn,
                "<=" => ln <= rn,
                _ => false
            };
        }
        
        // Иначе — строковое сравнение (Ordinal).
        int result = string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal);
        return op switch
        {
            ">" => result > 0,
            "<" => result < 0,
            ">=" => result >= 0,
            "<=" => result <= 0,
            _ => false
        };
    }

    /// <summary>
    /// Проверяет равенство двух значений.
    /// Правила:
    ///   • null == null → true (оба отсутствуют — считаются равными);
    ///   • null == что-то → false (null не равен ничему, кроме null);
    ///   • оба числа → сравнение как чисел с допуском (для double);
    ///   • иначе → Equals (строки, bool и т.п.).
    /// </summary>
    /// <param name="left">Левое значение (результат EvaluateArithmetic).</param>
    /// <param name="right">Правое значение (результат EvaluateArithmetic).</param>
    /// <returns>true, если значения равны; иначе false.</returns>
    private bool AreEqual(object? left, object? right)
    {
        // Оба null => true, либо один из них null => false
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        
        // Если оба числа 
        if (TryToDouble(left, out double ln) && TryToDouble(right, out double rn))
        {
            // Сравниваем с допуском (1e-9), из-за погрешности двоичного представления double             
            return Math.Abs(ln - rn) < 1e-9;
        }

        // Остальное это сравнение строки, bool, объекты
        return left.Equals(right);
    }
    
    // ───────────────────────── Арифметика ─────────────────────────

    /// <summary>
    /// Вычисляет выражение с учётом арифметики и литералов.
    /// Возвращает object?: число для арифметики, либо исходное значение
    /// (строку, bool, объект) для литералов и переменных.
    /// </summary>
    /// <param name="expr">Выражение: литерал, переменная, свойство или арифметика над ними.</param>
    /// <param name="context">Словарь переменных шаблона.</param>
    /// <returns>Вычисленное значение или null, если вычислить не удалось.</returns>
    private object? EvaluateArithmetic(string expr, IDictionary<string, object> context)
    {
        expr = expr.Trim();
        // Пустое выражение — вычислить нечего
        if (expr.Length == 0) return null;
        
        // 1) Строковый литерал — проверяем первым, чтобы "a-b" не разобралось как a - b
        if (IsStringLiteral(expr))
            return expr.Substring(1, expr.Length - 2);
        
        // 2) Аддитивный уровень: ищем последний бинарный + или -
        int idx = FindLastBinaryAdditive(expr);
        if (idx >= 0)
        {
            // Рекурсивно вычисляем обе части
            object? left = EvaluateArithmetic(expr.Substring(0, idx).Trim(), context);
            object? right = EvaluateArithmetic(expr.Substring(idx + 1).Trim(), context);
            // Оба операнда должны быть числами
            if (!TryToDouble(left, out double l) || !TryToDouble(right, out double r))
                return null;
            
            // Применяем оператор
            return expr[idx] == '+' ? l + r : l - r;
        }
        
        // 3) Мультипликативный уровень: ищем последний * / или %
        idx = FindLastBinaryMultiplicative(expr);
        if (idx >= 0)
        {
            object? left = EvaluateArithmetic(expr.Substring(0, idx).Trim(), context);
            object? right = EvaluateArithmetic(expr.Substring(idx + 1).Trim(), context);
            if (!TryToDouble(left, out double l) || !TryToDouble(right, out double r))
                return null;
            // деление на 0 → null
            if ((expr[idx] == '/' || expr[idx] == '%') && r == 0)
                return null;

            return expr[idx] switch
            {
                '*' => l * r,
                '/' => l / r,
                '%' => l % r,
                _ => null
            };
        }
        
        // 4) Числовой литерал
        if (double.TryParse(expr, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
            return num;
        
        // 5) Унарный минус
        if (expr.StartsWith('-'))
        {
            object? inner = EvaluateArithmetic(expr.Substring(1).Trim(), context);
            return TryToDouble(inner, out double n) ? -n : null;
        }
        
        // 6) Булев литерал
        if (bool.TryParse(expr, out bool b))
            return b;
        
        // 7) null-литерал
        if (string.Equals(expr, "null", StringComparison.OrdinalIgnoreCase))
            return null;
        
        // 8) Переменная или свойство
        return EvaluateExpression(expr, context);
    }

    /// <summary>
    /// Проверяет, что строка целиком является строковым литералом.
    /// </summary>
    /// <param name="expr">Выражение (обычно уже обрезанное по краям через Trim).</param>
    /// <returns>
    /// true, если expr — это "..." или '...' целиком;
    /// false — если кавычек нет, они разные или строка не закрыта.
    /// </returns>
    private bool IsStringLiteral(string expr)
    {
        // Минимальный литерал — два символа: открывающая и закрывающая кавычки
        if (expr.Length < 2) return false;
        // Первый символ — кавычка
        char q = expr[0];
        // Открывающая кавычка может быть только " или '
        if (q != '"' && q != '\'') return false;
        // Ищем ту же кавычку начиная с позиции 1, литералом считается только если она стоит в самом конце строки
        return expr.IndexOf(q, 1) == expr.Length - 1;
    }

    /// <summary>
    /// Находит позицию последнего бинарного оператора + или - в выражении.
    /// Пропускает: унарные минусы ("-5"), строковые литералы ("a-b").
    /// </summary>
    /// <param name="expr">Выражение (обычно уже обрезанное через Trim).</param>
    /// <returns>Индекс последнего бинарного + или -. Иначе: -1, если их нет.</returns>
    private int FindLastBinaryAdditive(string expr)
    {
        // Позиция последнего найденного бинарного оператора (пока ничего не нашли)
        int last = -1;
        // Флаг «мы внутри строкового литерала»
        bool inStr = false;
        // Какая именно кавычка открыла литерал (" или ')
        char strCh = '\0';

        // Посимвольно идём по выражению
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            // Если строковый литерал, то пропускаем
            if (inStr)
            {
                if (c == strCh) inStr = false;
                continue;
            }

            // Запоминаем, какой кавычкой открыт литерал
            if (c is '"' or '\'')
            {
                strCh = c;
                inStr = true;
                continue;
            }
            
            // Если символ — не наш оператор, дальше проверять нечего
            if (c != '+' && c != '-') continue;
            // Унарный оператор пропускаем
            if (!IsBinaryOpAt(expr, i)) continue;
            // Оператор бинарный — запоминаем позицию
            last = i;
        }

        // Возвращаем позицию последнего бинарного + или - (-1, если не нашли)
        return last;
    }

    /// <summary>
    /// Находит позицию последнего бинарного оператора * / или % в выражении.
    /// Пропускает содержимое строковых литералов.
    /// </summary>
    /// <param name="expr">Выражение (обычно уже обрезанное через Trim).</param>
    /// <returns>Индекс последнего бинарного * / или % или -1, если их нет.</returns>
    private int FindLastBinaryMultiplicative(string expr)
    {
        // Позиция последнего найденного бинарного оператора (пока ничего не нашли)
        int last = -1;
        // Флаг «мы внутри строкового литерала»
        bool inStr = false;
        // Какая именно кавычка открыла литерал (" или ')
        char strCh = '\0';
        
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            
            // Если строковый литерал, то пропускаем
            if (inStr)
            {
                if (c == strCh) inStr = false;
                continue;
            }

            // Запоминаем, какой кавычкой открыт литерал
            if (c is '"' or '\'')
            {
                inStr = true;
                strCh = c;
                continue;
            }

            // Проверяем оператор и присваиваем индекс
            if (c is '*' or '/' or '%') last = i;
        }

        // Возвращаем позицию последнего оператора (-1, если не нашли).
        return last;
    }

    /// <summary>
    /// Проверяет, является ли оператор в позиции <c>pos</c> бинарным (разделителем двух операндов), а не унарным.
    /// </summary>
    /// <param name="expr">Выражение.</param>
    /// <param name="pos">Позиция проверяемого оператора (индекс символа '+', '-' и т.п.).</param>
    /// <returns>
    /// true — оператор бинарный (слева стоит операнд);
    /// false — унарный (слева оператор или начало строки).
    /// </returns>
    private bool IsBinaryOpAt(string expr, int pos)
    {
        // Начинаем с символа перед оператором
        int i = pos - 1;
        // Идём влево, пока не найдём непустой символ
        while (i >= 0 && char.IsWhiteSpace(expr[i]))
            i--;
        // Оператор стоит в начале выражения — это унарный минус
        if (i < 0) return false;

        // Проверяем, что слева стоит опреанд, а не оператор 
        char prev = expr[i];
        return prev != '+' && prev != '-' && prev != '*' && prev != '/'
               && prev != '%' && prev != '!' && prev != '='
               && prev != '<' && prev != '>' && prev != '&' && prev != '|'
               && prev != '(';
    }
    
    // ───────────────────────── Вспомогательные ─────────────────────────

    /// <summary>
    /// Находит первое вхождение <c>target</c> в строке, игнорируя содержимое строковых литералов.
    /// </summary>
    /// <param name="str">Строка, в которой ведётся поиск.</param>
    /// <param name="target">Искомый оператор: "||", "&amp;&amp;", "==", "&gt;=" и т.п.</param>
    /// <returns>Индекс найденного target или -1, если не найдено.</returns>
    private int FindTopLevel(string str, string target)
    {
        // Текущая позиция в строке
        int i = 0;
        while (i <= str.Length - target.Length)
        {
            char c = str[i];
            
            // Пропускаем содержимое строкового литерала целиком
            if (c is '"' or '\'')
            {
                // Запоминаем открывающую кавычку
                char quote = c;
                i++; // пропускаем открывающую кавычку
                while (i < str.Length && str[i] != quote) 
                    i++;
                i++; // пропускаем закрывающую кавычку
                continue;
            }

            // Сравниваем подстроку, начинающуюся с i позиции, с target
            if (string.Compare(str, i, target, 0, target.Length, StringComparison.Ordinal) == 0)
                return i;

            // Не совпало — идём к следующему символу
            i++;
        }

        // Дошагали до конца, совпадения нет
        return -1;
    }

    /// <summary>
    /// Пытается привести значение к double, если это числовой тип.
    /// Поддерживает: int, long, short, byte, double, float, decimal.
    /// НЕ конвертирует строки и bool.
    /// Используется в арифметике и сравнениях, чтобы числа разных типов
    /// (int, double, decimal) можно было складывать и сравнивать единообразно.
    /// </summary>
    /// <param name="value">Проверяемое значение (может быть null).</param>
    /// <param name="result">Числовое значение при успехе; 0 при неудаче.</param>
    /// <returns>
    /// true если значение числовое и приведено к double;
    /// false если значение не число.
    /// </returns>
    private bool TryToDouble(object? value, out double result)
    {
        // Паттерн-матчинг по типу
        switch (value)
        {
            // int → double
            case int i:
                result = i;
                return true;
            // long → double
            case long l:
                result = l;
                return true;
            // short → double
            case short sh:
                result = sh;
                return true;
            // byte → double
            case byte b:
                result = b;
                return true;
            // double → double
            case double d:
                result = d;
                return true;
            // float → double
            case float f:
                result = f;
                return true;
            // decimal → double
            case decimal m:
                result = (double)m;
                return true;
            // Строки, bool, null, объекты
            default:
                result = 0;
                return false;
        }
    }

    /// <summary>
    /// Проверяет «истинность» значения в логическом смысле (truthiness).
    /// Используется, когда условие $if(...) — это одиночное выражение без оператора.
    /// Правила (в порядке проверки):
    ///   • null            → false
    ///   • bool            → как есть (true / false)
    ///   • строка          → false для пустой ("") и null, иначе true
    ///   • любое число     → false для 0, иначе true
    ///   • коллекция       → false для пустой, иначе true
    ///   • любой другой объект → true (ссылка существует)
    /// </summary>
    /// <param name="value">Значение, которое нужно проверить.</param>
    /// <returns>true, если значение считается «истинным»; иначе false.</returns>
    private bool IsTruthy(object? value)
    {
        // Если null -> false
        if (value == null) return false;
        // Если bool -> возвращается как есть
        if (value is bool b) return b;
        // Если пустая строка -> false, иначе true
        if (value is string str) return !string.IsNullOrEmpty(str);
        // Если числовое значение: 0 -> false; не 0 -> true
        if (TryToDouble(value, out double num)) return num != 0;
        // Если непустая коллекция -> true, иначе false
        if (value is IEnumerable enumerable) return enumerable.Cast<object>().Any();
        // Любой не-null объект -> true
        return true;
    }

    #endregion

    
    /// <summary>
    /// Ищет парный закрывающий тег с учётом вложенности.
    /// Вызывается, когда открывающий тег уже найден и мы стоим сразу после него, поэтому начальная глубина = 1.
    /// </summary>
    /// <param name="template">Строка шаблона.</param>
    /// <param name="startPos">Позиция сразу после открывающего тега.</param>
    /// <param name="openTag">Открывающий тег, например "$if(" или "$foreach(".</param>
    /// <param name="closeTag">Парный закрывающий тег, например "$endif" или "$endfor".</param>
    /// <returns>Индекс найденного парного закрывающего тега.</returns>
    /// <exception cref="SyntaxErrorException">
    /// Если парный закрывающий тег не найден до конца шаблона.
    /// </exception>
    private int FindMatchingTag(string template, int startPos, string openTag, string closeTag)
    {
        // Глубина вложенности 1 — потому что мы уже внутри одного открывающего тега
        int depth = 1;
        // Текущая позиция поиска
        int pos = startPos;

        while (pos < template.Length)
        {
            // Ищем ближайшие открывающий и закрывающий теги начиная с pos
            int nextOpen = template.IndexOf(openTag, pos, StringComparison.OrdinalIgnoreCase);
            int nextClose= template.IndexOf(closeTag, pos, StringComparison.OrdinalIgnoreCase);

            // Если закрывающего тега больше нет, а глубина ещё не 0, значит открывающий тег не закрыт
            if (nextClose == -1)
                throw new SyntaxErrorException($"Синтаксическая ошибка: не найден парный закрывающий тег {closeTag}");

            // Если открывающий тег встретился раньше закрывающего, значит это вложенный блок. Увеличиваем глубину и прыгаем за него.
            if (nextOpen != -1 && nextOpen < nextClose)
            {
                depth++;
                pos = nextOpen + openTag.Length;
            }
            // Ближайший тег закрывающий
            else
            {
                depth--;
                
                // Если глубина 0 значит нашли парный closeTag для исходного openTag
                if (depth == 0) 
                    return nextClose;
                
                // Иначе это был закрывающий тег вложенного блока, то прыгаем за него
                pos = nextClose + closeTag.Length;
            }
        }

        // Если парный тег так и не нашли
        throw new SyntaxErrorException($"Синтаксическая ошибка: не найден парный закрывающий тег {closeTag}");
    }

    /// <summary>
    /// Ищет тег targetTag строго на первом уровне вложенности.
    /// Используется для поиска $else внутри $if.
    /// </summary>
    /// <param name="template">Строка шаблона.</param>
    /// <param name="startPos">Позиция, с которой начинаем поиск.</param>
    /// <param name="targetTag">Тег который требуется найти.</param>
    /// <param name="openTag">Открывающий тег для отслеживания вложенности.</param>
    /// <param name="closeTag">Парный закрывающий тег.</param>
    /// <returns>Индекс targetTag на первом уровне вложенности или -1, если не найден.</returns>
    private int FindTagAtDepth1(string template, int startPos, string targetTag, string openTag, string closeTag)
    {
        // Глубина вложенности 1 (уже внутри одного блока)
        int depth = 1;
        // Текущая позиция поиска
        int pos = startPos;
        
        while (pos < template.Length)
        {
            // Ищем ближайшие вхождения всех трёх тегов начиная с pos
            int nextOpen = template.IndexOf(openTag, pos, StringComparison.OrdinalIgnoreCase);
            int nextClose = template.IndexOf(closeTag, pos, StringComparison.OrdinalIgnoreCase);
            int nextTarget = template.IndexOf(targetTag, pos, StringComparison.OrdinalIgnoreCase);

            // Исходный тег считаем за openTag, если его нет (-1), то тега пока нет
            int nearest = nextOpen;
            string? nearestType = nextOpen != -1 ? "open" : null;

            // Если closeTag ближе openTag то он становится ближайшим
            if (nextClose != -1 && (nearest == -1 || nextClose < nearest))
            {
                nearest = nextClose;
                nearestType = "close";
            }
            // Если targetTag ближе всех предыдущих, то он становится ближайшим
            if (nextTarget != -1 && (nearest == -1 || nextTarget < nearest))
            {
                nearest = nextTarget;
                nearestType = "target";
            }

            // Если тега не осталось, то targetTag на первом уровне не найден
            if (nearest == -1) return -1;
            
            switch (nearestType)
            {
                // Если ближайший тег открывающий, то это вложенный блок, увеличиваем глубину и прыгаем за него
                case "open":
                    depth++;
                    pos = nearest + openTag.Length;
                    break;
                // Если ближайший тег закрывающий, то закрылся текущий или вложенный блок, уменьшаем глубину
                case "close":
                    depth--;
                    if (depth == 0) return -1;  // блок закрылся — $else на первом уровне уже не встретится
                    pos = nearest + closeTag.Length;
                    break;
                // Если ближайший это targetTag
                default:
                {
                    // Если мы на первом уровне то возвращаем его позицию
                    if (depth == 1) return nearest;
                    // targetTag относится к вложенному блоку, пропускаем
                    pos = nearest + targetTag.Length;
                    break;
                }
            }
        }

        // Дошли до конца и targetTag на первом уровне не найден
        return -1;
    }
}