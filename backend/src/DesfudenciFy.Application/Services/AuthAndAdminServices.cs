using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Application.Common;
using DesfudenciFy.Application.DTOs;
using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DesfudenciFy.Application.Services;

public class AuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IAppDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Email ou senha inválidos.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            _jwtTokenService.GenerateToken(user),
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString());
    }
}

public class UserService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IAppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .OrderBy(u => u.Email)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.IsActive, u.Role.ToString(), u.LastLoginAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new AppException("Este email já está em uso.");
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            throw new AppException("Perfil inválido.");
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            IsActive = true
        };

        _db.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return new UserDto(user.Id, user.Email, user.FullName, user.IsActive, user.Role.ToString(), user.LastLoginAt);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Usuário não encontrado.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id, cancellationToken))
        {
            throw new AppException("Este email já está em uso.");
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            throw new AppException("Perfil inválido.");
        }

        user.Email = email;
        user.FullName = request.FullName.Trim();
        user.IsActive = request.IsActive;
        user.Role = role;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = _passwordHasher.Hash(request.Password);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new UserDto(user.Id, user.Email, user.FullName, user.IsActive, user.Role.ToString(), user.LastLoginAt);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Usuário não encontrado.");
        _db.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class BankAccountService
{
    private readonly IAppDbContext _db;

    public BankAccountService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BankAccountDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.BankAccounts.OrderBy(x => x.Name)
            .Select(x => new BankAccountDto(x.Id, x.Name, x.Description, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<BankAccountDto> CreateAsync(UpsertBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new BankAccount
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            IsActive = request.IsActive
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new BankAccountDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }

    public async Task<BankAccountDto> UpdateAsync(Guid id, UpsertBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.BankAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Conta bancária não encontrada.");
        entity.Name = request.Name.Trim();
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return new BankAccountDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.BankAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Conta bancária não encontrada.");
        if (await _db.Investments.AnyAsync(i => i.BankAccountId == id, cancellationToken))
        {
            throw new AppException("Não é possível excluir uma conta bancária vinculada a investimentos.");
        }

        _db.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class InvestmentTypeService
{
    private readonly IAppDbContext _db;

    public InvestmentTypeService(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<InvestmentTypeDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.InvestmentTypes.OrderBy(x => x.Name)
            .Select(x => new InvestmentTypeDto(x.Id, x.Name, x.Description, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<InvestmentTypeDto> CreateAsync(UpsertInvestmentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new InvestmentType
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            IsActive = request.IsActive
        };
        _db.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new InvestmentTypeDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }

    public async Task<InvestmentTypeDto> UpdateAsync(Guid id, UpsertInvestmentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.InvestmentTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Tipo de investimento não encontrado.");
        entity.Name = request.Name.Trim();
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return new InvestmentTypeDto(entity.Id, entity.Name, entity.Description, entity.IsActive);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.InvestmentTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Tipo de investimento não encontrado.");
        if (await _db.Investments.AnyAsync(i => i.InvestmentTypeId == id, cancellationToken))
        {
            throw new AppException("Não é possível excluir um tipo de investimento vinculado a investimentos.");
        }

        _db.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
