using GUHC.HierarchySystem.Core.DTOs;
using GUHC.HierarchySystem.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GUHC.HierarchySystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto)
    {
        try
        {
            var created = await _accountService.CreateAccountAsync(dto.Name, dto.ParentId);
            return CreatedAtAction(nameof(GetAccount), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAccount(int id)
    {
        try
        {
            var account = await _accountService.GetAccountAsync(id);
            if (account == null)
                return NotFound();

            return Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/move")]
    public async Task<IActionResult> MoveAccount(int id, [FromBody] MoveAccountDto dto)
    {
        try
        {
            await _accountService.MoveAccountAsync(id, dto.NewParentId);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/tree")]
    public async Task<IActionResult> GetSubtree(int id)
    {
        try
        {
            var subtree = await _accountService.GetSubtreeAsync(id);
            if (subtree == null)
                return NotFound();

            return Ok(subtree);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        try
        {
            await _accountService.DeleteAccountAsync(id);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
