using System.ComponentModel.DataAnnotations;

public class AdminMessageViewModel
{
    [Required(ErrorMessage = "Mesaj alanı zorunludur")]
    public string Message { get; set; }
    
    public bool SendToAllRooms { get; set; } = false;
    
    public List<int>? SelectedRoomIds { get; set; }
} 