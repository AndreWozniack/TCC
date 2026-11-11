using AuthFix;
using Xunit;

namespace AuthFix.Tests;

public class ValidationTests
{
    [Fact]
    public void EmptyPatchIsNotValid()
    {
        var result = PatchValidator.Validate("", "", "", "", "");
        Assert.Equal("não executada", result.Security);
        Assert.Equal("patch ausente", result.Status);
    }
}

public class ValidatorIntegrationTests
{
    [Fact]
    public void RejectsPatchChangingAnotherFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "authfix-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Git(root, "init", "-q");
            File.WriteAllText(Path.Combine(root, "Other.cs"), "class Before {}\n");
            Git(root, "add", ".");
            Git(root, "-c", "user.name=Test", "-c", "user.email=test@example.invalid", "commit", "-qm", "fixture");
            File.WriteAllText(Path.Combine(root, "Other.cs"), "class After {}\n");
            var diff = Git(root, "diff");
            Git(root, "restore", "Other.cs");
            var result = PatchValidator.Validate(root, "HEAD", "Endpoint.cs", root, diff);
            Assert.Equal("arquivos não permitidos", result.Status);
            Assert.Equal("não executada", result.Security);
            Assert.Equal("class Before {}\n", File.ReadAllText(Path.Combine(root, "Other.cs")));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string Git(string cwd, params string[] arguments)
    {
        var info = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(info)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, error);
        return output;
    }
}
