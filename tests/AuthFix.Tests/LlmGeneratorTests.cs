using System.Net;
using System.Text;
using AuthFix;
using Xunit;

namespace AuthFix.Tests;

public class LlmGeneratorTests
{
    [Fact]
    public void GeneratesUnifiedDiffFromCorrectedSnippetInConditionA()
    {
        const string fullFile = """
            using Microsoft.AspNetCore.Authorization;
            using Microsoft.AspNetCore.Mvc;

            public class InformacoesController : ControllerBase
            {
                [HttpGet("admin-info")]
                public IActionResult InfoAdmin()
                {
                    return Ok("Segredo");
                }
            }
            """;
        const string methodOnly = """
            [HttpGet("admin-info")]
                public IActionResult InfoAdmin()
                {
                    return Ok("Segredo");
                }
            """;

        var fakeProvider = new FakeLlmProvider("anthropic:claude-sonnet-4-5", """
            {
              "fixedCode": "[Authorize(Policy = \"SomenteAdmin\")]\n    [HttpGet(\"admin-info\")]\n    public IActionResult InfoAdmin()\n    {\n        return Ok(\"Segredo\");\n    }",
              "explanation": "Adiciona a policy SomenteAdmin ao endpoint.",
              "references": ["Program.cs"]
            }
            """);

        var alert = new Alert("aspnet-controller-sem-autorizacao", "CWE-862", "Controllers/InformacoesController.cs", 6);
        var input = Experiment.BuildInput(alert, methodOnly, fullFile, [], "A");
        var generator = new LlmGenerator(fakeProvider, _ => fullFile);

        var report = generator.Generate(input);

        Assert.Equal("anthropic:claude-sonnet-4-5", report.Provider);
        Assert.Contains("diff --git a/Controllers/InformacoesController.cs b/Controllers/InformacoesController.cs", report.Proposal.Diff);
        Assert.Contains("+    [Authorize(Policy = \"SomenteAdmin\")]", report.Proposal.Diff);
        Assert.Equal("Adiciona a policy SomenteAdmin ao endpoint.", report.Proposal.Explanation);
        Assert.Contains("Program.cs", report.Proposal.References);
    }

    [Fact]
    public void FallsBackToMockGeneratorWhenProviderFails()
    {
        var failingProvider = new FailingLlmProvider();
        var fallback = new MockGenerator(new MockResponse("diff --git a/Api.cs b/Api.cs", "fallback", []));
        var generator = new LlmGenerator(failingProvider, _ => "class Api {}", fallback);
        var input = Experiment.BuildInput(new Alert("1", "CWE-862", "Api.cs", 1), "class Api {}", "class Api {}", [], "B");

        var report = generator.Generate(input);

        Assert.Equal("simulado", report.Provider);
        Assert.Equal("fallback", report.Proposal.Explanation);
    }

    [Fact]
    public void AnthropicProviderSendsHeadersAndExtractsText()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new StubHttpHandler(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            const string body = """
                {
                  "id": "msg_01",
                  "type": "message",
                  "role": "assistant",
                  "content": [
                    { "type": "text", "text": "{\"fixedCode\":\"code\",\"explanation\":\"ok\",\"references\":[]}" }
                  ]
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var provider = new AnthropicProvider("sk-ant-test-key", AnthropicModel.ClaudeSonnet4_5, httpClient);

        var response = provider.Complete("system prompt", "user prompt");

        Assert.Equal("anthropic:claude-sonnet-4-5", provider.Name);
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://api.anthropic.com/v1/messages", capturedRequest!.RequestUri!.ToString());
        Assert.Equal("sk-ant-test-key", capturedRequest.Headers.GetValues("x-api-key").Single());
        Assert.Equal("2023-06-01", capturedRequest.Headers.GetValues("anthropic-version").Single());
        Assert.Contains("\"output_config\":{\"format\":{\"type\":\"json_schema\",\"schema\":", capturedBody);
        Assert.Contains("\"additionalProperties\":false", capturedBody);
        Assert.Contains("fixedCode", response);
    }

    [Theory]
    [InlineData("claude-sonnet-4-5", AnthropicModel.ClaudeSonnet4_5)]
    [InlineData("claude-haiku-4-5", AnthropicModel.ClaudeHaiku4_5)]
    [InlineData("claude-opus-4-5", AnthropicModel.ClaudeOpus4_5)]
    [InlineData("ClaudeSonnet4_5", AnthropicModel.ClaudeSonnet4_5)]
    public void ParsesAnthropicModelEnumAndApiId(string input, AnthropicModel expected)
    {
        var parsed = AnthropicModelExtensions.Parse(input);
        Assert.Equal(expected, parsed);
        Assert.False(string.IsNullOrWhiteSpace(parsed.ToApiId()));
    }

    [Fact]
    public void DiffGeneratedByLlmGeneratorAppliesCleanlyWithGitApply()
    {
        var root = Path.Combine(Path.GetTempPath(), "authfix-llm-git-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string original = """
                using Microsoft.AspNetCore.Authorization;
                using Microsoft.AspNetCore.Mvc;

                namespace DemoApi.Controllers;

                [ApiController]
                [Route("api")]
                public class InformacoesController : ControllerBase
                {
                    // Falha proposital para a demonstração: falta a política SomenteAdmin.
                    [HttpGet("admin-info")]
                    public IActionResult InfoAdmin()
                    {
                        return Ok("Segredo");
                    }
                }
                """;
            Directory.CreateDirectory(Path.Combine(root, "Controllers"));
            var filePath = Path.Combine(root, "Controllers", "InformacoesController.cs");
            File.WriteAllText(filePath, original);

            var fakeProvider = new FakeLlmProvider("anthropic:claude-sonnet-4-5", """
                {
                  "fixedCode": "using Microsoft.AspNetCore.Authorization;\nusing Microsoft.AspNetCore.Mvc;\n\nnamespace DemoApi.Controllers;\n\n[ApiController]\n[Route(\"api\")]\npublic class InformacoesController : ControllerBase\n{\n    [Authorize(Policy = \"SomenteAdmin\")]\n    [HttpGet(\"admin-info\")]\n    public IActionResult InfoAdmin()\n    {\n        return Ok(\"Segredo\");\n    }\n}",
                  "explanation": "Reutiliza a policy SomenteAdmin.",
                  "references": ["Program.cs"]
                }
                """);

            var alert = new Alert("aspnet-controller-sem-autorizacao", "CWE-862", "Controllers/InformacoesController.cs", 11);
            var method = AuthorizationAnalyzer.FindEndpoint(original, 11);
            var input = Experiment.BuildInput(alert, method, original, [], "C");
            var generator = new LlmGenerator(fakeProvider, _ => original);

            var report = generator.Generate(input);
            var patchFile = Path.Combine(root, "fix.patch");
            File.WriteAllText(patchFile, report.Proposal.Diff);

            GitRepository.Execute(root, "init", "-q");
            GitRepository.Execute(root, "apply", "--check", "--", patchFile);
            GitRepository.Execute(root, "apply", "--", patchFile);

            var patched = File.ReadAllText(filePath);
            Assert.Contains("[Authorize(Policy = \"SomenteAdmin\")]", patched);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void GeneratorFactorySelectsLlmWhenApiKeyIsPresentAndMockOtherwise()
    {
        var root = Path.Combine(Path.GetTempPath(), "authfix-factory-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "resposta-simulada.json"),
                """{"diff":"diff","explanation":"mock","references":[]}""");

            var options = ScanOptions.Parse([]);
            var withKey = GeneratorFactory.Create(root, options, "sk-ant-key");
            var withoutKey = GeneratorFactory.Create(root, options, "");
            var forcedMock = GeneratorFactory.Create(root, ScanOptions.Parse(["--resposta", "resposta-simulada.json"]), "sk-ant-key");

            Assert.IsType<LlmGenerator>(withKey);
            Assert.IsType<MockGenerator>(withoutKey);
            Assert.IsType<MockGenerator>(forcedMock);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private sealed class FakeLlmProvider(string name, string response) : ILlmProvider
    {
        public string Name => name;
        public string Complete(string systemPrompt, string userPrompt) => response;
    }

    private sealed class FailingLlmProvider : ILlmProvider
    {
        public string Name => "anthropic:failing";
        public string Complete(string systemPrompt, string userPrompt) => throw new HttpRequestException("Sem conexão");
    }

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
