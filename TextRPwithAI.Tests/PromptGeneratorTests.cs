using System;
using System.IO;
using Xunit;
using TextRPwithAI;

namespace TextRPwithAI.Tests;

/// <summary>
/// Класс с модульными тестами для проверки работы класса PromptGenerator.
/// </summary>
public class PromptGeneratorTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly string _testPromptPath;
    private readonly string _testStoryPath;
    private readonly string _sampleFilePath;

    public PromptGeneratorTests()
    {
        // Создаем временную директорию для тестов
        _testBasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _testPromptPath = Path.Combine(_testBasePath, "Промты");
        _testStoryPath = Path.Combine(_testBasePath, "Сюжеты");
        
        Directory.CreateDirectory(_testPromptPath);
        Directory.CreateDirectory(_testStoryPath);

        // Инициализируем пути в статическом классе
        PromptGenerator.InitializePaths(_testBasePath);

        // Создаем Sample.txt для тестов
        _sampleFilePath = Path.Combine(_testBasePath, "Sample.txt");
        File.WriteAllText(_sampleFilePath, "Начало шаблона.\n*****\nКонец шаблона.");
    }

    /// <summary>
    /// Очищаем временные директории после каждого теста.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }

    /// <summary>
    /// Тест проверяет успешную генерацию промпта и сохранение иерархии папок.
    /// </summary>
    [Fact]
    public void GeneratePrompt_ShouldCreateNewFileWithCorrectContentAndHierarchy()
    {
        // Arrange
        var testFileName = "Quest1.txt";
        var nestedDir = Path.Combine(_testStoryPath, "Глава 1", "Дополнительные");
        Directory.CreateDirectory(nestedDir);
        
        var sourceFilePath = Path.Combine(nestedDir, testFileName);
        var storyContent = "Это сюжет первой главы.";
        File.WriteAllText(sourceFilePath, storyContent);

        // Act
        var resultPath = PromptGenerator.GeneratePrompt(testFileName, _sampleFilePath);

        // Assert
        Assert.NotNull(resultPath);
        Assert.True(File.Exists(resultPath), "Файл не был создан.");
        
        var expectedRelativePath = Path.Combine("Глава 1", "Дополнительные", $"Промт. {testFileName}");
        var expectedTargetPath = Path.Combine(_testPromptPath, expectedRelativePath);
        
        Assert.Equal(expectedTargetPath, resultPath);
        
        var resultContent = File.ReadAllText(resultPath);
        Assert.Contains(storyContent, resultContent);
        Assert.DoesNotContain("*****", resultContent);
        Assert.Contains("Начало шаблона.", resultContent);
    }

    /// <summary>
    /// Тест проверяет поведение при отсутствии файла сюжета. Метод должен вернуть null.
    /// </summary>
    [Fact]
    public void GeneratePrompt_ReturnsNull_WhenStoryFileNotFound()
    {
        // Act
        var resultPath = PromptGenerator.GeneratePrompt("NonExistentFile.txt", _sampleFilePath);

        // Assert
        Assert.Null(resultPath);
    }

    /// <summary>
    /// Тест проверяет выброс исключения при отсутствии файла шаблона (Sample.txt).
    /// </summary>
    [Fact]
    public void GeneratePrompt_ThrowsFileNotFoundException_WhenSampleIsMissing()
    {
        // Arrange
        var invalidSamplePath = Path.Combine(_testBasePath, "MissingSample.txt");
        
        var tempFile = Path.Combine(_testStoryPath, "ValidQuest.txt");
        File.WriteAllText(tempFile, "Сюжет.");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => 
            PromptGenerator.GeneratePrompt("ValidQuest.txt", invalidSamplePath));
    }

    /// <summary>
    /// Тест проверяет, что мета-теги корректно объединяются и вставляются перед строкой "Я играю за".
    /// </summary>
    [Fact]
    public void ProcessStoryContent_ShouldCombineMetaAndInsertBeforeAnchor()
    {
        // Arrange
        string inputContent = @"Обычный текст начала.
<Мета: Первая информация>
Продолжение текста.
<Мета: Вторая информация>
Завершающий текст.
Я играю за Героя.
Конец лора.";

        string expectedPart = @"<Мета: Первая информация

Вторая информация>

Я играю за Героя.";

        // Act
        string result = PromptGenerator.ProcessStoryContent(inputContent);

        // Assert
        Assert.DoesNotContain("<Мета: Первая информация>", result);
        Assert.DoesNotContain("<Мета: Вторая информация>", result);
        // Проверяем, что объединенный блок стоит прямо перед "Я играю за"
        Assert.Contains(expectedPart.Replace("\r", ""), result.Replace("\r", ""));
    }
}