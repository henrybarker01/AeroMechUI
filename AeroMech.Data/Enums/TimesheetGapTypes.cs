using System.ComponentModel;

namespace AeroMech.Data.Enums
{
    public enum TimesheetGapTypes
    {
        Admin,
        General,
        Leave,
        Procurement,

        [Description("Public Holiday")]
        PublicHoliday,

        [Description("Sick Leave")]
        SickLeave,
        Standby,
        Traveling
    }
}
