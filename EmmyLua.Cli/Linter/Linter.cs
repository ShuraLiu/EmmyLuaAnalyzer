using System.Collections.Immutable;
using EmmyLua.CodeAnalysis.Compilation.Search;
using EmmyLua.CodeAnalysis.Diagnostics;
using EmmyLua.CodeAnalysis.Document;
using EmmyLua.CodeAnalysis.Workspace;
using EmmyLua.Configuration;

namespace EmmyLua.Cli.Linter;

public class Linter(CheckOptions options)
{
    public int Run()
    {
        var workspacePath = options.Workspace;
        var fileList = new List<string>();
        if (options.FilelistPath.Length > 0)
        {
            File.ReadAllLines(options.FilelistPath).ToList().ForEach(file =>
            {
                file = file.Replace("/", "\\");
                fileList.Add(file);
            });
        }
        else
        {
            fileList.AddRange(options.files);
        }
        var settingManager = new SettingManager();
        settingManager.CustomSettingPath = options.Config;
        settingManager.Watch(workspacePath);
        var luaWorkspace = LuaProject.Create(workspacePath, settingManager.GetLuaFeatures());
        var foundedError = false;
        var searchContext = new SearchContext(luaWorkspace.Compilation, new SearchContextFeatures());
        List<LuaDocument> documents = new List<LuaDocument>();
        fileList.ForEach(file =>
        {
            var doc = luaWorkspace.GetDocumentByPath(file);
            if (doc != null)
            {
                documents.Add(doc);
            }
        });
        foreach (var document in documents)
        {
            var diagnostics = luaWorkspace.Compilation.GetDiagnostics(document.Id, searchContext);
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    foundedError = true;
                }

                var location = document.GetLocation(diagnostic.Range, 1);
                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Error:
                        Console.Error.WriteLine($"{location}: {diagnostic.Severity}: {diagnostic.Message} ({diagnostic.Code})");
                        break;
                    default:
                        Console.WriteLine($"{location}: {diagnostic.Severity}: {diagnostic.Message} ({diagnostic.Code})");
                        break;
                }
            }
        }

        Console.WriteLine("Check done!");
        return foundedError ? 1 : 0;
    }
}