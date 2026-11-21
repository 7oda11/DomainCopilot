using DomainCopilot.Api.Contracts.Authentication;
using DomainCopilot.Application.Features.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DomainCopilot.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(
    RegisterUserHandler registerUser,
    LoginUserHandler loginUser) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await registerUser.Handle(
                new RegisterUserRequest(request.Email, request.Password), cancellationToken);

            return StatusCode(StatusCodes.Status201Created,
                new RegisterResponse(result.Id, result.Email, result.Role));
        }
        catch (EmailAlreadyRegisteredException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Email is already registered."
            });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginUser.Handle(new LoginUserRequest(request.Email, request.Password), cancellationToken);
        if (result is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid email or password."
            });
        }

        return Ok(new LoginResponse(result.AccessToken, result.TokenType, result.ExpiresAtUtc));
    }
}
