using ClinicFlow.Api.Infrastructure.Auth;
using ClinicFlow.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicFlow.Api.Features.Auth;

public record RefreshTokenCommand(string RefreshTokenValue) : IRequest<RefreshTokenResponse>;

public record RefreshTokenResponse(string AccessToken, string RefreshToken);

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IJwtTokenService _tokenService;

    public RefreshTokenHandler(IRefreshTokenService refreshTokenService, IJwtTokenService tokenService)
    {
        _refreshTokenService = refreshTokenService;
        _tokenService = tokenService;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _refreshTokenService.ValidateAndConsumeAsync(request.RefreshTokenValue, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = await _refreshTokenService.GenerateAsync(user, cancellationToken);

        return new RefreshTokenResponse(newAccessToken, newRefreshToken);
    }
}