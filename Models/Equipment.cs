namespace GPIMSWeb.Models
{
    public class Equipment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string Version { get; set; }
        public EquipmentStatus Status { get; set; }
        public List<Channel> Channels { get; set; } = new List<Channel>();
        public List<CanLinData> CanLinData { get; set; } = new List<CanLinData>();
        public List<AuxData> AuxData { get; set; } = new List<AuxData>();
        public List<Alarm> Alarms { get; set; } = new List<Alarm>();
    }

    public enum EquipmentStatus
    {
        Idle = 0,
        Running = 1,
        Error = 2,
        Updating = 3
    }

    public class Channel
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public int ChannelNumber { get; set; }
        public ChannelStatus Status { get; set; }
        public ChannelMode Mode { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Capacity { get; set; }
        public double Power { get; set; }
        public double Energy { get; set; }
        public string ScheduleName { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    public enum ChannelStatus
    {
        Idle = 0,
        Active = 1,
        Error = 2,
        Pause = 3
    }

    public enum ChannelMode
    {
        Rest = 0,
        Charge = 1,
        Discharge = 2,
        CV = 3,
        CC = 4
    }

    public class CanLinData
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public string Name { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double CurrentValue { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    public class AuxData
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public string SensorId { get; set; }
        public string Name { get; set; }
        public AuxDataType Type { get; set; }
        public double Value { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    public enum AuxDataType
    {
        Voltage = 0,
        Temperature = 1,
        NTC = 2
    }

    public class ChamberChillerData
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public double Value { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    public class Alarm
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public string Message { get; set; }
        public AlarmLevel Level { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCleared { get; set; }
        public DateTime? ClearedAt { get; set; }
        public string ClearedBy { get; set; }
    }

    public enum AlarmLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }
}