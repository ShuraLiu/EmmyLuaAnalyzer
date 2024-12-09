using EmmyLua.CodeAnalysis.Compilation.Search;
using EmmyLua.CodeAnalysis.Diagnostics;
using EmmyLua.CodeAnalysis.Workspace;
using EmmyLua.Configuration;

namespace EmmyLua.Cli.Linter;

public class Linter(CheckOptions options)
{
    public int Run()
    {
        var workspacePath = options.Workspace;
        var fileList = options.files.ToList();
        var settingManager = new SettingManager();
        settingManager.CustomSettingPath = options.Config;
        settingManager.Watch(workspacePath);
        var luaWorkspace = LuaProject.Create(workspacePath, settingManager.GetLuaFeatures());
        var foundedError = false;
        var searchContext = new SearchContext(luaWorkspace.Compilation, new SearchContextFeatures());
        var documents = luaWorkspace.AllDocuments.Where(doc =>
        {
            if (fileList.Count > 0)
            {
                return fileList.Contains(doc.Path);
            }
            
            foreach (var root in luaWorkspace.Features.ThirdPartyRoots)
            {
                if (doc.Path.StartsWith(root))
                {
                    return false;
                }
            }
            return true;
        }).ToList();
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
                Console.WriteLine($"{location}: {diagnostic.Severity}: {diagnostic.Message} ({diagnostic.Code})");
            }
        }

        Console.WriteLine("Check done!");
        return foundedError ? 1 : 0;
    }
}