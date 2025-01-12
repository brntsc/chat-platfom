using Web.Models;

public class ChatRoomViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public string ImagePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto CreatedBy { get; set; }
    public List<UserDto> Users { get; set; }
}

public class UserDto
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string? ProfileImage { get; set; }
}

public class ChatRoomRequestDto
{
    public int Id { get; set; }
    public UserDto User { get; set; }
    public ChatRoomViewModel ChatRoom { get; set; }
    public string Message { get; set; }
    public DateTime RequestDate { get; set; }
    public RequestStatus Status { get; set; }
}