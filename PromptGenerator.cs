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
    /// Относительный под-путь внутри папки YandexDisk до каталога с данными игры.
    /// </summary>
    private static readonly string _yandexSubPath = Path.Combine("НРИ", "Игры с нейросетью");

    /// <summary>
    /// Базовый путь к каталогу с данными игры.
    /// Вычисляется автоматически через поиск папки YandexDisk по стандартным путям.
    /// Пустая строка означает, что путь не найден и требуется ручная инициализация.
    /// </summary>
    private static string _basePath = FindBasePathStandard() ?? string.Empty;

    /// <summary>
    /// Путь к каталогу с промптами.
    /// </summary>
    private static string _promptPath = Path.Combine(_basePath, "Промты");

    /// <summary>
    /// Путь к каталогу с сюжетами.
    /// </summary>
    private static string _storyPath = Path.Combine(_basePath, "Сюжеты");

    /// <summary>
    /// Путь к файлу шаблона Sample.txt.
    /// </summary>
    private static string _sampleFilePath = Path.Combine(_basePath, "Образец.txt");

    /// <summary>
    /// Возвращает true, если базовый путь был успешно найден и установлен.
    /// </summary>
    public static bool IsBasePathFound => !string.IsNullOrEmpty(_basePath);

    /// <summary>
    /// Ищет папку YandexDisk по стандартным путям: домашняя директория пользователя,
    /// корни фиксированных дисков и вложенные папки Clouds на каждом диске.
    /// </summary>
    /// <returns>Полный путь к базовому каталогу игры, либо null если папка не найдена.</returns>
    private static string? FindBasePathStandard()
    {
        var yandexFolderNames = new[] { "Yandex.Disk", "YandexDisk", "Яндекс.Диск" };

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var searchRoots = new List<string> { userProfile };

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            searchRoots.Add(drive.RootDirectory.FullName);
            searchRoots.Add(Path.Combine(drive.RootDirectory.FullName, "Clouds"));
        }

        foreach (var root in searchRoots)
        {
            foreach (var name in yandexFolderNames)
            {
                string candidate = Path.Combine(root, name);
                if (Directory.Exists(candidate))
                    return Path.Combine(candidate, _yandexSubPath);
            }
        }

        return null;
    }

    /// <summary>
    /// Инициализирует пути, вычисляя местоположение папки YandexDisk по пути переданного файла.
    /// Поднимается вверх по дереву директорий, пока не найдёт папку с именем YandexDisk или аналогичным.
    /// </summary>
    /// <param name="filePath">Абсолютный путь к файлу, переданному из командной строки или контекстного меню.</param>
    /// <returns>true, если папка YandexDisk найдена и пути инициализированы; false иначе.</returns>
    public static bool InitializePathsFromFile(string filePath)
    {
        var yandexFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Yandex.Disk", "YandexDisk", "Яндекс.Диск" };

        string? dirPath = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (dirPath == null) return false;

        var dir = new DirectoryInfo(dirPath);
        while (dir != null)
        {
            if (yandexFolderNames.Contains(dir.Name))
            {
                InitializePaths(Path.Combine(dir.FullName, _yandexSubPath));
                return true;
            }
            dir = dir.Parent!;
        }
        return false;
    }

    /// <summary>
    /// Выполняет полный рекурсивный поиск папки YandexDisk по всем фиксированным дискам системы.
    /// Может занять значительное время — используется как последний вариант при поиске.
    /// </summary>
    /// <returns>true, если папка найдена и пути инициализированы; false иначе.</returns>
    public static bool InitializePathsWithFullSearch()
    {
        var yandexFolderNames = new[] { "Yandex.Disk", "YandexDisk", "Яндекс.Диск" };

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            var found = SearchDirectoryRecursive(drive.RootDirectory, yandexFolderNames);
            if (found != null)
            {
                InitializePaths(Path.Combine(found, _yandexSubPath));
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Рекурсивно ищет директорию с одним из указанных имён, пропуская папки без доступа.
    /// </summary>
    /// <param name="dir">Директория, с которой начинается поиск.</param>
    /// <param name="targetNames">Массив искомых имён директорий.</param>
    /// <returns>Полный путь к найденной директории, либо null.</returns>
    private static string? SearchDirectoryRecursive(DirectoryInfo dir, string[] targetNames)
    {
        try
        {
            foreach (var subDir in dir.EnumerateDirectories())
            {
                if (targetNames.Contains(subDir.Name, StringComparer.OrdinalIgnoreCase))
                    return subDir.FullName;

                var result = SearchDirectoryRecursive(subDir, targetNames);
                if (result != null)
                    return result;
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        return null;
    }

    /// <summary>
    /// Устанавливает пользовательские пути. Полезно для переопределения в модульных тестах.
    /// </summary>
    /// <param name="basePath">Новый базовый путь к папкам.</param>
    public static void InitializePaths(string basePath)
    {
        _basePath = basePath;
        _promptPath = Path.Combine(basePath, "Промты");
        _storyPath = Path.Combine(basePath, "Сюжеты");
        _sampleFilePath = Path.Combine(basePath, "Образец.txt");
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
    public static string? GeneratePrompt(string fileName, string? sampleFilePath = null, bool overwrite = true)
    {
        if ( sampleFilePath is null)
        {
            sampleFilePath = _sampleFilePath;
        }

        if (!Directory.Exists(_storyPath))
            throw new DirectoryNotFoundException($"Каталог сюжетов не найден по пути: {_storyPath}");

        // Ищем файл во всех вложенных папках директории Сюжеты
        string[] foundFiles = Directory.GetFiles(_storyPath, fileName, SearchOption.AllDirectories);
        if (foundFiles.Length == 0)
            return null; // Файл не найден

        string sourceFilePath = foundFiles[0];

        return GeneratePromptFromPath(sourceFilePath, sampleFilePath, overwrite);
    }

    /// <summary>
    /// Создает промпт из указанного абсолютного пути файла сюжета.
    /// </summary>
    /// <param name="absolutePath">Абсолютный путь к файлу сюжета.</param>
    /// <param name="sampleFilePath">Путь к файлу шаблона Sample.txt.</param>
    /// <returns>Полный путь к созданному файлу промпта, либо null, если исходный файл сюжета не найден.</returns>
    /// <exception cref="FileNotFoundException">Выбрасывается, если не найден шаблон Sample.txt.</exception>
    public static string? GeneratePromptFromPath(string absolutePath, string? sampleFilePath = null, bool overwrite = true)
    {
        if ( sampleFilePath is null)
        {
            sampleFilePath = _sampleFilePath;
        }

        if (!File.Exists(absolutePath))
            return null;

        if (!File.Exists(sampleFilePath))
            throw new FileNotFoundException($"Шаблон не найден по пути: {sampleFilePath}. Убедитесь, что Sample.txt лежит там же, где выполняется код.");

        string sampleContent = File.ReadAllText(sampleFilePath);
        string storyContent = File.ReadAllText(absolutePath);

        // Обрабатываем перенос сеттинга
        var settingMatch = Regex.Match(storyContent, @"^Сеттинг:.*", RegexOptions.Multiline);
        if (settingMatch.Success)
        {
            sampleContent = sampleContent.Replace("Сеттинг: ***", settingMatch.Value.TrimEnd());
            storyContent = Regex.Replace(storyContent, $@"^{Regex.Escape(settingMatch.Value)}(\r?\n){{0,2}}", string.Empty, RegexOptions.Multiline);
        }

        // Вместо ***** вставляем содержимое найденного сюжета
        string generatedContent = sampleContent.Replace("*****", storyContent);

        // Получаем путь относительно _storyPath (для проверки, внутри ли он этой папки)
        string relativeFilePath = Path.GetRelativePath(_storyPath, absolutePath);

        string targetFilePath;
        string newFileName = $"Промт. {Path.GetFileName(absolutePath)}";

        // Если файл внутри папки с сюжетами - сохраняем в папку Промты с иерархией
        if (!relativeFilePath.StartsWith("..") && !Path.IsPathRooted(relativeFilePath))
        {
            string parentDir = Path.GetDirectoryName(relativeFilePath) ?? string.Empty;
            string newRelativeFilePath = string.IsNullOrEmpty(parentDir) ? newFileName : Path.Combine(parentDir, newFileName);
            targetFilePath = Path.Combine(_promptPath, newRelativeFilePath);
        }
        else
        {
            // Если файл вне папки сюжетов - сохраняем промпт в ту же директорию, где и сюжет
            string parentDir = Path.GetDirectoryName(absolutePath) ?? string.Empty;
            targetFilePath = Path.Combine(parentDir, newFileName);
        }

        string? targetDir = Path.GetDirectoryName(targetFilePath);
        if (targetDir != null && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        if (File.Exists(targetFilePath) && !overwrite)
        {
            return targetFilePath;
        }

        File.WriteAllText(targetFilePath, generatedContent);

        return targetFilePath;
    }
}