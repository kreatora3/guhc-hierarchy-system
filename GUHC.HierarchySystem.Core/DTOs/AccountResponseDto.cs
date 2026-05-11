namespace GUHC.HierarchySystem.Core.DTOs;

public class AccountResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int Depth { get; set; }
}