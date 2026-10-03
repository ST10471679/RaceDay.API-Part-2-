using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;
using Xunit;

namespace RaceDay.Tests;

public class AuthServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        // Seed roles for foreign key dependencies
        context.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "User" }
        );
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailIsUnique()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context);

        var registerDto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Password = "Password123!",
            RoleId = 2
        };

        // Act
        var result = await authService.RegisterAsync(registerDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("User", result.RoleName);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnNull_WhenEmailAlreadyExists()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context);

        var registerDto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "duplicate@example.com",
            Password = "Password123!",
            RoleId = 2
        };

        await authService.RegisterAsync(registerDto);

        // Act
        var result = await authService.RegisterAsync(registerDto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnUser_WhenCredentialsAreValid()
    {
        // Arrange
        var context = GetInMemoryDbContext();
        var authService = new AuthService(context);

        var registerDto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Password = "SecurePassword123!",
            RoleId = 2
        };

        await authService.RegisterAsync(registerDto);

        var loginDto = new LoginDto
        {
            Email = "john@example.com",
            Password = "SecurePassword123!"
        };

        // Act
        var result = await authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("john@example.com", result.Email);
    }
}