using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TextRPwithAI;

/// <summary>
/// Статический класс для генерации промптов для текстовой ролевой игры.
/// </summary>
public static class PromptGenerator
{
    /// <summary>
    /// Базовый путь к каталогу с данными игры.
    /// </summary>
    private static string _basePath = @"D:\Clouds\YandexDisk\Игры с нейросетью";

    /// <summary>
    /// Путь к каталогу с промптами.
    /// </summary>
    private static string _promptPath = Path.Combine(_basePath, "Промты");

    /// <summary>
    /// Путь к каталогу с сюжетами.
    /// </summary>
    private static string _storyPath = Path.Combine(_basePath, "Сюжеты");

    /// <summary>
    /// Устанавливает пользовательские пути. Полезно для переопределения в модульных тестах.
    /// </summary>
    /// <param name="basePath">Новый базовый путь к папкам.</param>
    public static void InitializePaths(string basePath)
    {
        _basePath = basePath;
        _promptPath = Path.Combine(basePath, "Промты");
        _storyPath = Path.Combine(basePath, "Сюжеты");
    }

    /// <summary>
    /// Возвращает список всех файлов сюжетов относительно папки Сюжеты.
    /// </summary>
    /// <returns>Массив относительных путей к файлам сюжетов.</returns>
    public static string[] GetAvailableStories()
    {
        if (!Directory.Exists(_storyPath))
            return Array.Empty<string>();

        return Directory.GetFiles(_storyPath, "*.txt", SearchOption.AllDirectories)
                        .Select(p => Path.GetRelativePath(_storyPath, p))
                        .ToArray();
    }

    /// <summary>
    /// Находит файл с указанным именем в каталоге Сюжеты, 
    /// читает шаблон Sample.txt, объединяет их и сохраняет результат 
    /// в каталоге Промты, соблюдая исходную иерархию папок.
    /// </summary>
    /// <param name="fileName">Имя файла сюжета для поиска (например, "Story.txt").</param>
    /// <param name="sampleFilePath">Путь к файлу шаблона Sample.txt.</param>
    /// <returns>Полный путь к созданному файлу промпта, либо null, если исходный файл сюжета не найден.</returns>
    /// <exception cref="DirectoryNotFoundException">Выбрасывается, если не существует папка с сюжетами.</exception>
    /// <exception cref="FileNotFoundException">Выбрасывается, если не найден шаблон Sample.txt.</exception>
    public static string? GeneratePrompt(string fileName, string sampleFilePath = "Sample.txt")
    {
        if (!Directory.Exists(_storyPath))
            throw new DirectoryNotFoundException($"Каталог сюжетов не найден по пути: {_storyPath}");

        if (!File.Exists(sampleFilePath))
            throw new FileNotFoundException($"Шаблон не найден по пути: {sampleFilePath}. Убедитесь, что Sample.txt лежит там же, где выполняется код.");

        // Ищем файл во всех вложенных папках директории Сюжеты
        string[] foundFiles = Directory.GetFiles(_storyPath, fileName, SearchOption.AllDirectories);
        if (foundFiles.Length == 0)
            return null; // Файл не найден

        string sourceFilePath = foundFiles[0];

        string sampleContent = File.ReadAllText(sampleFilePath);
        string storyContent = File.ReadAllText(sourceFilePath);

        // Обрабатываем сюжет (объединяем мета-абзацы и переносим их)
        storyContent = ProcessStoryContent(storyContent);

        // Вместо ***** вставляем содержимое найденного сюжета
        string generatedContent = sampleContent.Replace("*****", storyContent);

        // Получаем относительный путь к файлу (чтобы сохранить иерархию папок)
        string relativeFilePath = Path.GetRelativePath(_storyPath, sourceFilePath);

        // Устанавливаем новое имя файла "Промт. {Имя}"
        string parentDir = Path.GetDirectoryName(relativeFilePath) ?? string.Empty;
        string newFileName = $"Промт. {fileName}";
        string newRelativeFilePath = string.IsNullOrEmpty(parentDir) ? newFileName : Path.Combine(parentDir, newFileName);

        // Формируем финальный путь в папке Промты
        string targetFilePath = Path.Combine(_promptPath, newRelativeFilePath);

        string? targetDir = Path.GetDirectoryName(targetFilePath);
        if (targetDir != null && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        File.WriteAllText(targetFilePath, generatedContent);

        return targetFilePath;
    }

    /// <summary>
    /// Обрабатывает содержимое сюжета: объединяет мета-теги и вставляет их перед строкой "Я играю за".
    /// </summary>
    /// <param name="content">Исходный текст сюжета.</param>
    /// <returns>Обработанный текст.</returns>
    public static string ProcessStoryContent(string content)
    {
        string metaPattern = @"<Мета:\s*(.+?)\s*>";
        var matches = Regex.Matches(content, metaPattern, RegexOptions.Singleline);
        
        if (matches.Count == 0)
        {
            return content;
        }

        var metaContents = new List<string>();
        foreach (Match match in matches)
        {
            metaContents.Add(match.Groups[1].Value.Trim());
        }

        // Удаляем оригинальные блоки <Мета: ...>
        string processedContent = Regex.Replace(content, metaPattern, string.Empty, RegexOptions.Singleline);

        // Убираем множественные пустые строки, которые могли остаться после удаления
        processedContent = Regex.Replace(processedContent, @"\n[ \t]*\n[ \t]*\n", "\n\n", RegexOptions.Multiline);

        // Формируем объединенный блок
        string combinedMeta = $"<Мета: {string.Join(Environment.NewLine + Environment.NewLine, metaContents)}>";

        string anchorPattern = @"^(?=Я играю за)";
        var anchorRegex = new Regex(anchorPattern, RegexOptions.Multiline);
        
        // Вставляем перед "Я играю за" (если найдено) или в конец файла
        if (anchorRegex.IsMatch(processedContent))
        {
            processedContent = anchorRegex.Replace(processedContent, combinedMeta + Environment.NewLine + Environment.NewLine, 1);
        }
        else
        {
            processedContent = processedContent.TrimEnd() + Environment.NewLine + Environment.NewLine + combinedMeta;
        }

        return processedContent.Trim();
    }
}