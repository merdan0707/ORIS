namespace MiniTemplateEngine;

/// <summary>
/// Интерфейс, определяющий контракт для движка шаблонизации. 
/// </summary>
public interface ITemplateEngine
{
    /// <summary>
    /// Выполняет рендеринг шаблона, переданного в виде строки, используя переданную модель данных.
    /// </summary>
    /// <param name="template">Строка, содержащая шаблон с тегами.</param>
    /// <param name="model">Объект модели данных, свойства которого будут подставлены в шаблон.</param>
    /// <returns>Готовая строка с подставленными значениями.</returns>
    string RenderFromString(string template, object model);

    /// <summary>
    /// Загружает шаблон из файла, выполняет его рендеринг и возвращает результат в виде строки.
    /// </summary>
    /// <param name="filePath">Путь к файлу шаблона.</param>
    /// <param name="model">Объект модели данных.</param>
    /// <returns>Готовая строка с подставленными значениями.</returns>
    string RenderFromFile(string filePath, object model);

    /// <summary>
    /// Загружает шаблон из файла, выполняет рендеринг и сохраняет результат в указанный файл.
    /// </summary>
    /// <param name="inputFilePath">Путь к исходному файлу шаблона.</param>
    /// <param name="outputFilePath">Путь для сохранения готового файла.</param>
    /// <param name="model">Объект модели данных.</param>
    /// <returns>Готовая строка с подставленными значениями.</returns>
    string RenderToFile(string inputFilePath, string outputFilePath, object model);
}