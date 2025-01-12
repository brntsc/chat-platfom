public static class AvatarHelper
{
    public static string GetInitialsAvatar(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        
        var initial = name.Substring(0, 1).ToUpper();
        var backgroundColor = "#4CAF50"; // Yeşil renk
        
        return $"data:image/svg+xml;base64,{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($@"
            <svg xmlns='http://www.w3.org/2000/svg' width='40' height='40'>
                <circle cx='20' cy='20' r='20' fill='{backgroundColor}'/>
                <text x='20' y='25' font-family='Arial' font-size='20' fill='white' text-anchor='middle'>{initial}</text>
            </svg>"))}";
    }
} 