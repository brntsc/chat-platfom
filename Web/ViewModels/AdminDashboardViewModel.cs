using Web.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalRooms { get; set; }
    public int ActiveRooms { get; set; }
    public int PendingRequests { get; set; }
    public int TotalMessages { get; set; }
    public int TotalUsers { get; set; }
    public int OnlineUsers { get; set; }
    public List<ChatRoomViewModel> RecentRooms { get; set; }
    public List<ChatMessageViewModel> RecentMessages { get; set; }
} 