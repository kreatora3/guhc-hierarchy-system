using GUHC.HierarchySystem.Core.DTOs;
using GUHC.HierarchySystem.Core.Entities;
using GUHC.HierarchySystem.Infrastructure.Data;
using GUHC.HierarchySystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GUHC.HierarchySystem.Tests;

public class AccountServiceTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    #region CreateAccountAsync Tests

    [Fact]
    public async Task CreateAccountAsync_WithValidName_CreatesRootAccount()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        // Act
        var result = await service.CreateAccountAsync("Root Account", null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Root Account", result.Name);
        Assert.Null(result.ParentId);
        Assert.Equal(1, result.Depth);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateAccountAsync_WithValidParent_CreatesChildAccount()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        // Create root account
        var root = await service.CreateAccountAsync("Root", null);

        // Act
        var result = await service.CreateAccountAsync("Child", root.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Child", result.Name);
        Assert.Equal(root.Id, result.ParentId);
        Assert.Equal(2, result.Depth);
    }

    [Fact]
    public async Task CreateAccountAsync_WithInvalidParentId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAccountAsync("Orphan", 999));
    }

    [Fact]
    public async Task CreateAccountAsync_ExceedingMaxDepth_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        // Create a chain of accounts to reach max depth
        int parentId = (await service.CreateAccountAsync("Level1", null)).Id;
        parentId = (await service.CreateAccountAsync("Level2", parentId)).Id;
        parentId = (await service.CreateAccountAsync("Level3", parentId)).Id;
        parentId = (await service.CreateAccountAsync("Level4", parentId)).Id;
        parentId = (await service.CreateAccountAsync("Level5", parentId)).Id;

        // Act & Assert - attempting to create at depth 6
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAccountAsync("Level6", parentId));
    }

    [Fact]
    public async Task CreateAccountAsync_WithZeroOrNegativeParentId_CreatesRootAccount()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        // Act
        var result1 = await service.CreateAccountAsync("Root1", 0);
        var result2 = await service.CreateAccountAsync("Root2", -1);

        // Assert
        Assert.Null(result1.ParentId);
        Assert.Equal(1, result1.Depth);
        Assert.Null(result2.ParentId);
        Assert.Equal(1, result2.Depth);
    }

    #endregion

    #region GetAccountAsync Tests

    [Fact]
    public async Task GetAccountAsync_WithValidId_ReturnsAccount()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);
        var created = await service.CreateAccountAsync("Test Account", null);

        // Act
        var result = await service.GetAccountAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Test Account", result.Name);
        Assert.Equal(1, result.Depth);
    }

    [Fact]
    public async Task GetAccountAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        // Act
        var result = await service.GetAccountAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region MoveAccountAsync Tests

    [Fact]
    public async Task MoveAccountAsync_ToValidParent_MovesAccount()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        var root = await service.CreateAccountAsync("Root", null);
        var acc1 = await service.CreateAccountAsync("Acc1", root.Id);
        var acc2 = await service.CreateAccountAsync("Acc2", root.Id);

        // Act
        await service.MoveAccountAsync(acc2.Id, acc1.Id);
        var moved = await service.GetAccountAsync(acc2.Id);

        // Assert
        Assert.NotNull(moved);
        Assert.Equal(acc1.Id, moved!.ParentId);
        Assert.Equal(3, moved.Depth);
    }

    [Fact]
    public async Task MoveAccountAsync_ToNull_ThrowsExceptionForRootAccount()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        var root = await service.CreateAccountAsync("Root", null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.MoveAccountAsync(root.Id, null));
    }

    [Fact]
    public async Task MoveAccountAsync_ToDescendant_ThrowsExceptionCycle()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        var root = await service.CreateAccountAsync("Root", null);
        var child = await service.CreateAccountAsync("Child", root.Id);
        var grandchild = await service.CreateAccountAsync("Grandchild", child.Id);

        // Act & Assert - trying to move root to its grandchild
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.MoveAccountAsync(root.Id, grandchild.Id));
    }

    [Fact]
    public async Task MoveAccountAsync_ExceedingDepth_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        int parentId = (await service.CreateAccountAsync("L1", null)).Id;
        parentId = (await service.CreateAccountAsync("L2", parentId)).Id;
        parentId = (await service.CreateAccountAsync("L3", parentId)).Id;
        parentId = (await service.CreateAccountAsync("L4", parentId)).Id;
        var level5 = (await service.CreateAccountAsync("L5", parentId)).Id;

        var independent = await service.CreateAccountAsync("Ind", null);

        // Act & Assert - moving independent to level5 would make it depth 6
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.MoveAccountAsync(independent.Id, level5));
    }

    #endregion

    #region DeleteAccountAsync Tests

    [Fact]
    public async Task DeleteAccountAsync_WithoutChildren_DeletesAccount()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        var root = await service.CreateAccountAsync("Root", null);
        var child = await service.CreateAccountAsync("Child", root.Id);

        // Act
        await service.DeleteAccountAsync(child.Id);
        var deleted = await service.GetAccountAsync(child.Id);

        // Assert
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithChildren_ReassignsChildren()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        var root = await service.CreateAccountAsync("Root", null);
        var middle = await service.CreateAccountAsync("Middle", root.Id);
        var child1 = await service.CreateAccountAsync("Child1", middle.Id);
        var child2 = await service.CreateAccountAsync("Child2", middle.Id);

        // Act
        await service.DeleteAccountAsync(middle.Id);

        // Assert
        var updatedChild1 = await service.GetAccountAsync(child1.Id);
        var updatedChild2 = await service.GetAccountAsync(child2.Id);

        Assert.NotNull(updatedChild1);
        Assert.NotNull(updatedChild2);
        Assert.Equal(root.Id, updatedChild1!.ParentId);
        Assert.Equal(root.Id, updatedChild2!.ParentId);
        Assert.Equal(2, updatedChild1.Depth);
        Assert.Equal(2, updatedChild2.Depth);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteAccountAsync(999));
    }

    [Fact]
    public async Task DeleteAccountAsync_DeletesRootAndReassignsAllChildren()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        var root = await service.CreateAccountAsync("Root", null);
        var child1 = await service.CreateAccountAsync("Child1", root.Id);
        var child2 = await service.CreateAccountAsync("Child2", root.Id);
        var grandchild = await service.CreateAccountAsync("Grandchild", child1.Id);

        // Act
        await service.DeleteAccountAsync(root.Id);

        // Assert
        var deletedRoot = await service.GetAccountAsync(root.Id);
        var orphanChild1 = await service.GetAccountAsync(child1.Id);
        var orphanChild2 = await service.GetAccountAsync(child2.Id);

        Assert.Null(deletedRoot);
        Assert.NotNull(orphanChild1);
        Assert.NotNull(orphanChild2);
        Assert.Null(orphanChild1!.ParentId);
        Assert.Null(orphanChild2!.ParentId);
        Assert.Equal(1, orphanChild1.Depth);
        Assert.Equal(1, orphanChild2.Depth);
    }

    #endregion

    #region GetSubtreeAsync Tests

    [Fact]
    public async Task GetSubtreeAsync_WithSingleAccount_ReturnsTreeWithNoChildren()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        var root = await service.CreateAccountAsync("Root", null);

        // Act
        var result = await service.GetSubtreeAsync(root.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(root.Id, result.Id);
        Assert.Equal("Root", result.Name);
        Assert.Equal(1, result.Depth);
        Assert.Empty(result.Children);
    }

    [Fact]
    public async Task GetSubtreeAsync_WithMultipleLevels_ReturnsNestedTree()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        var root = await service.CreateAccountAsync("Root", null);
        var child1 = await service.CreateAccountAsync("Child1", root.Id);
        var child2 = await service.CreateAccountAsync("Child2", root.Id);
        var grandchild = await service.CreateAccountAsync("Grandchild", child1.Id);

        // Act
        var result = await service.GetSubtreeAsync(root.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Children.Count);
        
        var firstChild = result.Children.FirstOrDefault(c => c.Id == child1.Id);
        Assert.NotNull(firstChild);
        Assert.Single(firstChild!.Children);
        Assert.Equal(grandchild.Id, firstChild.Children[0].Id);
        Assert.Equal(3, firstChild.Children[0].Depth);
    }

    [Fact]
    public async Task GetSubtreeAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        // Act
        var result = await service.GetSubtreeAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSubtreeAsync_FromMiddleNode_ReturnsSubtreeOnly()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        var root = await service.CreateAccountAsync("Root", null);
        var middle = await service.CreateAccountAsync("Middle", root.Id);
        var child1 = await service.CreateAccountAsync("Child1", middle.Id);
        var child2 = await service.CreateAccountAsync("Child2", middle.Id);

        // Act
        var result = await service.GetSubtreeAsync(middle.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(middle.Id, result.Id);
        Assert.Equal(2, result.Depth);
        Assert.Equal(2, result.Children.Count);
        Assert.All(result.Children, c => Assert.Equal(3, c.Depth));
    }

    #endregion

    #region Depth Calculation Tests

    [Fact]
    public async Task DepthCalculation_MultiLevelHierarchy_CalculatesCorrectly()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new AccountService(context);

        // Act - Create a hierarchy and verify depths
        var l1 = await service.CreateAccountAsync("L1", null);
        var l2 = await service.CreateAccountAsync("L2", l1.Id);
        var l3 = await service.CreateAccountAsync("L3", l2.Id);
        var l4 = await service.CreateAccountAsync("L4", l3.Id);
        var l5 = await service.CreateAccountAsync("L5", l4.Id);

        // Assert
        Assert.Equal(1, l1.Depth);
        Assert.Equal(2, l2.Depth);
        Assert.Equal(3, l3.Depth);
        Assert.Equal(4, l4.Depth);
        Assert.Equal(5, l5.Depth);
    }

    #endregion
}
