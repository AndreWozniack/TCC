using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication("piloto").AddScheme<AuthenticationSchemeOptions, PilotAuthentication>("piloto", _ => { });
builder.Services.AddAuthorization(options => options.AddPolicy("DocumentOwner", policy =>
    policy.Requirements.Add(new OwnerRequirement())));
builder.Services.AddSingleton<IAuthorizationHandler, OwnerHandler>();
builder.Services.AddSingleton<DocumentStore>();
builder.Services.AddControllers();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public sealed class PilotAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = Request.Headers["X-Pilot-User"].ToString();
        if (user is not ("alice" or "bob")) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

public sealed record Document(string Owner, string Content);
public sealed class DocumentStore
{
    public Document Current { get; set; } = new("alice", "inicial");
}
public sealed class OwnerRequirement : IAuthorizationRequirement { }
public sealed class OwnerHandler : AuthorizationHandler<OwnerRequirement, Document>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnerRequirement requirement, Document resource)
    {
        if (resource.Owner == context.User.FindFirstValue(ClaimTypes.NameIdentifier))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
