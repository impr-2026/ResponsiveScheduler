using System;
using System.Collections.Generic;
using System.Web.Services;
using System.Web.Script.Services;

namespace CalendarScheduler
{
    public partial class FullCalendarExample : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e) { }

        public class CalendarEvent
        {
            public string id { get; set; }
            public string title { get; set; }
            public string start { get; set; }
            public string end { get; set; }
            public bool allDay { get; set; }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static List<CalendarEvent> GetEvents(DateTime start, DateTime end)
        {
            // Sample events (replace with DB query)
            var events = new List<CalendarEvent>
            {
                new CalendarEvent { id="1", title="Team Meeting", start=start.AddHours(2).ToString("s"), end=start.AddHours(3).ToString("s"), allDay=false },
                new CalendarEvent { id="2", title="Client Call", start=start.AddDays(1).AddHours(1).ToString("s"), end=start.AddDays(1).AddHours(2).ToString("s"), allDay=false }
            };
            return events;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string UpdateEvent(string id, DateTime start, DateTime end)
        {
            // Update DB
            return @"Updated event {id} to start:{start} end:{end}";
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static string DeleteEvent(string id)
        {
            // Delete from DB
            return @"Deleted event {id}";
        }
    }
}