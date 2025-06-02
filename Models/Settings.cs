namespace GPIMSWeb.Models
{
    public class SystemSettings
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class SettingsViewModel
    {
        public int EquipmentCount { get; set; }
        public int ChannelsPerEquipment { get; set; }
        public string DefaultLanguage { get; set; }
        public string DateFormat { get; set; }
    }
}