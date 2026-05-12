using GUHC.HierarchySystem.Core.DTOs;
using GUHC.HierarchySystem.Core.Entities;
using GUHC.HierarchySystem.Core.Services;
using GUHC.HierarchySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GUHC.HierarchySystem.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _context;
    private const int MaxDepth = 5;

    public AccountService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AccountResponseDto> CreateAccountAsync(string name, int? parentId)
    {
        // Normalize invalid parent IDs to null so root creation works as expected.
        if (parentId <= 0)
            parentId = null;

        int depth = 1;
        if (parentId.HasValue)
        {
            var parentAccount = await _context.Accounts.FindAsync(parentId.Value);
            if (parentAccount == null)
                throw new InvalidOperationException($"Parent account with ID {parentId} not found.");

            // Calculate parent's depth
            int parentDepth = await CalculateDepthAsync(parentId.Value);
            depth = parentDepth + 1;
        }

        // Validate depth constraint
        if (depth > MaxDepth)
            throw new InvalidOperationException($"Cannot create account: depth {depth} exceeds maximum allowed depth of {MaxDepth}.");

        var account = new Account
        {
            Name = name,
            ParentAccountId = parentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return new AccountResponseDto
        {
            Id = account.Id,
            Name = account.Name,
            ParentId = account.ParentAccountId,
            Depth = depth
        };
    }

    public async Task MoveAccountAsync(int accountId, int? newParentId)
    {
        var account = await _context.Accounts.FindAsync(accountId);
        if (account == null)
            throw new InvalidOperationException($"Account with ID {accountId} not found.");

        // Validate: cannot move root account
        if (account.ParentAccountId == null)
            throw new InvalidOperationException("Root account cannot be moved.");

        // Validate: cannot move to a descendant (would create cycle)
        if (await WouldCreateCycleAsync(accountId, newParentId))
            throw new InvalidOperationException("Cannot move account: new parent is a descendant of this account.");

        // Calculate new depth
        int newDepth = 1;
        if (newParentId.HasValue)
        {
            var newParent = await _context.Accounts.FindAsync(newParentId.Value);
            if (newParent == null)
                throw new InvalidOperationException($"New parent account with ID {newParentId} not found.");

            newDepth = await CalculateDepthAsync(newParentId.Value) + 1;
        }

        // Validate new depth constraint
        if (newDepth > MaxDepth)
            throw new InvalidOperationException($"Cannot move account: new depth {newDepth} exceeds maximum allowed depth of {MaxDepth}.");

        // Update account's parent
        account.ParentAccountId = newParentId;
        account.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccountAsync(int id)
    {
        var account = await _context.Accounts
            .Include(a => a.ChildAccounts)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (account == null)
            throw new InvalidOperationException($"Account with ID {id} not found.");

        // Reassign children to deleted account's parent
        foreach (var child in account.ChildAccounts)
        {
            child.ParentAccountId = account.ParentAccountId;
            child.UpdatedAt = DateTime.UtcNow;
        }

        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync();
    }

    public async Task<AccountResponseDto?> GetAccountAsync(int id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account == null)
            return null;

        int depth = await CalculateDepthAsync(id);
        return new AccountResponseDto
        {
            Id = account.Id,
            Name = account.Name,
            ParentId = account.ParentAccountId,
            Depth = depth
        };
    }

    public async Task<AccountTreeResponseDto?> GetSubtreeAsync(int accountId)
    {
        var rootAccount = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (rootAccount == null)
            return null;

        int depth = await CalculateDepthAsync(accountId);

        var subtreeAccounts = await LoadSubtreeAccountsAsync(accountId);
        var childrenLookup = subtreeAccounts
            .GroupBy(a => a.ParentAccountId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return BuildTree(rootAccount, depth, childrenLookup);
    }

    private async Task<List<Account>> LoadSubtreeAccountsAsync(int rootAccountId)
    {
        if (_context.Database.IsRelational())
        {
            var sql = @";WITH RecursiveAccounts AS
            (
                SELECT *
                FROM Accounts
                WHERE ParentAccountId = {0}
                UNION ALL
                SELECT a.*
                FROM Accounts a
                INNER JOIN RecursiveAccounts r ON a.ParentAccountId = r.Id
            )
            SELECT *
            FROM RecursiveAccounts";

            return await _context.Accounts
                .FromSqlRaw(sql, rootAccountId)
                .AsNoTracking()
                .ToListAsync();
        }

        var descendants = new List<Account>();
        var parentIds = new List<int> { rootAccountId };

        while (parentIds.Count > 0)
        {
            var children = await _context.Accounts
                .AsNoTracking()
                .Where(a => a.ParentAccountId.HasValue && parentIds.Contains(a.ParentAccountId.Value))
                .ToListAsync();

            if (children.Count == 0)
                break;

            descendants.AddRange(children);
            parentIds = children.Select(a => a.Id).ToList();
        }

        return descendants;
    }

    private AccountTreeResponseDto BuildTree(Account account, int depth, Dictionary<int, List<Account>> childrenLookup)
    {
        var dto = new AccountTreeResponseDto
        {
            Id = account.Id,
            Name = account.Name,
            Depth = depth,
            Children = new List<AccountTreeResponseDto>()
        };

        if (!childrenLookup.TryGetValue(account.Id, out var childAccounts))
            return dto;

        foreach (var child in childAccounts)
        {
            dto.Children.Add(BuildTree(child, depth + 1, childrenLookup));
        }

        return dto;
    }

    private async Task<int> CalculateDepthAsync(int? accountId)
    {
        if (!accountId.HasValue)
            return 0;

        var account = await _context.Accounts.FindAsync(accountId.Value);
        if (account == null)
            return 0;

        if (!account.ParentAccountId.HasValue)
            return 1;

        return 1 + await CalculateDepthAsync(account.ParentAccountId);
    }

    private async Task<bool> WouldCreateCycleAsync(int accountId, int? newParentId)
    {
        if (!newParentId.HasValue)
            return false;

        // Check if newParentId is a descendant of accountId
        var parentId = newParentId.Value;
        var visited = new HashSet<int>();

        while (parentId != 0)
        {
            if (parentId == accountId)
                return true; // Cycle detected

            if (visited.Contains(parentId))
                break; // Prevent infinite loop in case of existing cycles

            visited.Add(parentId);

            var parent = await _context.Accounts.FindAsync(parentId);
            if (parent?.ParentAccountId == null)
                break;

            parentId = parent.ParentAccountId.Value;
        }

        return false;
    }
}
