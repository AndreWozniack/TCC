using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("documents")]
[Authorize]
public sealed class DocumentsController(DocumentStore store, IAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await authorization.AuthorizeAsync(User, store.Current, "DocumentOwner");
        return result.Succeeded ? Ok(store.Current.Content) : Forbid();
    }

    [HttpPut]
    public IActionResult Update([FromBody] string content)
    {
        store.Current = store.Current with { Content = content };
        return NoContent();
    }
}
