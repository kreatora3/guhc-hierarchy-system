namespace GUHC.HierarchySystem.Core.DTOs;

public class CreateAccountDto
{
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
}