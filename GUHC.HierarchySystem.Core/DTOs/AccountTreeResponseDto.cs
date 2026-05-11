namespace GUHC.HierarchySystem.Core.DTOs;

public class AccountTreeResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Depth { get; set; }
    public List<AccountTreeResponseDto> Children { get; set; } = new List<AccountTreeResponseDto>();
}