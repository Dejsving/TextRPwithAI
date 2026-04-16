using System;
using System.IO;
using TextCopy;
using TextRPwithAI;
using Spectre.Console;

Console.Clear();
AnsiConsole.Write(
    new FigletText("Prompt Gen")
        .LeftJustified()
        .Color(Color.Blue));

string selectedStory;
string? resultPath = null;
bool isInteractive = args.Length == 0;

if (!isInteractive)
{
    // Получаем файл из контекстного меню или командной строки
    selectedStory = args[0];
    AnsiConsole.MarkupLine($"Получен файл: [yellow]{selectedStory}[/]");
    
    try
    {
        // Передаем абсолютный путь к файлу напрямую
        resultPath = PromptGenerator.GeneratePromptFromPath(selectedStory);
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"\n[bold red]Произошла ошибка при генерации:[/]");
        AnsiConsole.WriteException(ex);
    }
}
else
{
    var stories = PromptGenerator.GetAvailableStories();

    if (stories.Length == 0)
    {
        AnsiConsole.MarkupLine("[red]Сюжеты не найдены![/] Убедитесь, что папка 'Сюжеты' существует и содержит txt файлы.");
        Console.ReadKey();
        return;
    }

    selectedStory = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Выберите [green]сюжет[/] для генерации (нажмите [yellow]Enter[/] для выбора):")
            .PageSize(10)
            .MoreChoicesText("[grey](Используйте стрелки Вверх/Вниз для навигации)[/]")
            .AddChoices(stories));

    AnsiConsole.MarkupLine($"Выбран сюжет: [yellow]{selectedStory}[/]");

    try
    {
        // Извлекаем имя файла из пути для интерактивного режима
        string fileName = Path.GetFileName(selectedStory);
        resultPath = PromptGenerator.GeneratePrompt(fileName);
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"\n[bold red]Произошла ошибка при генерации:[/]");
        AnsiConsole.WriteException(ex);
    }
}

if (resultPath != null)
{
    try
    {
        string promptContent = File.ReadAllText(resultPath);
        ClipboardService.SetText(promptContent);
        
        AnsiConsole.MarkupLine($"\n[bold green]Успех![/]");
        AnsiConsole.MarkupLine($"Промпт сохранен в: [blue]{resultPath}[/]");
        AnsiConsole.MarkupLine("[bold yellow]Содержимое промпта скопировано в буфер обмена![/]");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"\n[bold green]Успех![/]");
        AnsiConsole.MarkupLine($"Промпт сохранен в: [blue]{resultPath}[/]");
        AnsiConsole.MarkupLine($"[red]Не удалось скопировать в буфер обмена:[/] {ex.Message}");
    }
}
else if (!isInteractive && resultPath == null)
{
    AnsiConsole.MarkupLine("\n[red]Ошибка:[/] Не удалось сгенерировать промпт по указанному пути.");
}
else if (isInteractive && resultPath == null)
{
    AnsiConsole.MarkupLine("\n[red]Ошибка:[/] Файл не найден.");
}

AnsiConsole.MarkupLine("\n[grey]Нажмите любую клавишу для выхода...[/]");
Console.ReadKey();
