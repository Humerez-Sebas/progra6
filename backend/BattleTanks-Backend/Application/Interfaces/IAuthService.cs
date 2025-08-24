using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterDto registerDto);
    Task<User> LoginAsync(LoginDto loginDto);
    Task<UserDto> GetUserProfileAsync(Guid userId);
}