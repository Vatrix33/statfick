// The Statventure Editor (M11, Docs/Editor.md); its command line is EditorArguments'.
using Statventure.Editor;

var arguments = EditorArguments.Parse(args, AppContext.BaseDirectory);
if (arguments.Problem is { } problem)
{
    Console.Error.WriteLine($"Statventure Editor: {problem}\n{EditorArguments.Usage}");
    return 2;
}
using var editor = new EditorApplication(arguments.Game!, arguments.PlayerFolder);
editor.Run();
return 0;
