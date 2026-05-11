namespace GUHC.HierarchySystem.Core.Entities;

public class Account
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int? ParentAccountId { get; set; }
    
    public Account? ParentAccount { get; set; }
    
    public ICollection<Account> ChildAccounts { get; set; } = new List<Account>();
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}
