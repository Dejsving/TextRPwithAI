using System;
using System.IO;
using TextRPwithAI;
using Spectre.Console;

Console.Clear();
AnsiConsole.Write(
    new FigletText("Prompt Gen")
        .LeftJustified()
        .Color(Color.Blue));

var stories = PromptGenerator.GetAvailableStories();

if (stories.Length == 0)
{
    AnsiConsole.MarkupLine("[red]Сюжеты не найдены![/] Убедитесь, что папка Сюжеты существует и содержит txt файлы.");
    return;
}

var selectedStory = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Выберите [green]сюжет[/] для генерации (нажмите [yellow]Enter[/] для выбора):")
        .PageSize(10)
        .MoreChoicesText("[grey](Используйте стрелки Вверх/Вниз для навигации)[/]")
        .AddChoices(stories));

AnsiConsole.MarkupLine($"Выбран сюжет: [yellow]{selectedStory}[/]");

try
{
    // Извлекаем имя файла из пути, так как метод GeneratePrompt ищет по имени файла
    string fileName = Path.GetFileName(selectedStory);
    
    string? resultPath = PromptGenerator.GeneratePrompt(fileName);

    if (resultPath != null)
    {
        AnsiConsole.MarkupLine($"\n[bold green]Успех![/]");
        AnsiConsole.MarkupLine($"Промпт сохранен в: [blue]{resultPath}[/]");
    }
    else
    {
        AnsiConsole.MarkupLine("\n[red]Ошибка:[/] Файл не найден.");
    }
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"\n[bold red]Произошла ошибка при генерации:[/]");
    AnsiConsole.WriteException(ex);
}

AnsiConsole.MarkupLine("\n[grey]Нажмите любую клавишу для выхода...[/]");
Console.ReadKey();
