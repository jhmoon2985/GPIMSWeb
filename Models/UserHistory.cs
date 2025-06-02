namespace GPIMSWeb.Models
{
    public class UserHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public User User { get; set; }
    }

    public class UserHistoryViewModel
    {
        public string Username { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}