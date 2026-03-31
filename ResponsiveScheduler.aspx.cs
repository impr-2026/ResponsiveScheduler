using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Web.Services;
using System.Web.UI.WebControls;

namespace CalendarScheduler
{
    public partial class ResponsiveScheduler : System.Web.UI.Page
    {
        private TimeSpan startTime { get { return TimeSpan.Parse(txtStartTime.Text); } }
        private TimeSpan endTime { get { return TimeSpan.Parse(txtEndTime.Text); } }
        private int slotMinutes { get { return ViewState["slotMinutes"] != null ? (int)ViewState["slotMinutes"] : 15; } set { ViewState["slotMinutes"] = value; } }
        private DateTime currentStartDate { get { return ViewState["currentStartDate"] != null ? (DateTime)ViewState["currentStartDate"] : DateTime.Today; } set { ViewState["currentStartDate"] = value; } }
        private DateTime currentEndDate { get { return ViewState["currentEndDate"] != null ? (DateTime)ViewState["currentEndDate"] : DateTime.Today; } set { ViewState["currentEndDate"] = value; } }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                GenerateSchedulerView(DateTime.Today, DateTime.Today.AddDays(6));
            }
        }

        protected void ddlSlotMinutes_SelectedIndexChanged(object sender, EventArgs e)
        {
            slotMinutes = int.Parse(ddlSlotMinutes.SelectedValue);
            GenerateSchedulerView(currentStartDate, currentEndDate);
        }

        protected void btnApplyTime_Click(object sender, EventArgs e)
        {
            GenerateSchedulerView(currentStartDate, currentEndDate);
        }

        protected void btnDay_Click(object sender, EventArgs e)
        {
            GenerateSchedulerView(DateTime.Today, DateTime.Today);
        }
        protected void btnWeek_Click(object sender, EventArgs e)
        {
            DateTime start = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            GenerateSchedulerView(start, start.AddDays(6));
        }
        protected void btnMonth_Click(object sender, EventArgs e)
        {
            DateTime start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            GenerateSchedulerView(start, start.AddMonths(1).AddDays(-1));
        }
        protected void btnCustom_Click(object sender, EventArgs e)
        {
            GenerateSchedulerView(DateTime.Parse(txtFrom.Text), DateTime.Parse(txtTo.Text));
        }

        private void GenerateSchedulerView(DateTime startDate, DateTime endDate)
        {
            phCalendar.Controls.Clear();
            currentStartDate = startDate;
            currentEndDate = endDate;
            int totalDays = (endDate - startDate).Days + 1;
            Table table = new Table { CssClass = "calendar-table" };

            // Header
            TableRow header = new TableRow();
            header.Cells.Add(new TableCell { Text = "Time" });
            for (int i = 0; i < totalDays; i++)
            {
                DateTime day = startDate.AddDays(i);
                TableCell cell = new TableCell { Text = day.ToString("ddd dd/MM"), CssClass = "resizable-col" };
                cell.Attributes["data-date"] = day.ToString("yyyy-MM-dd");
                if (day.Date == DateTime.Today) cell.CssClass += " current-day";
                header.Cells.Add(cell);
            }
            table.Rows.Add(header);

            int pixelsPerSlot = 20;
            for (TimeSpan t = startTime; t < endTime; t = t.Add(TimeSpan.FromMinutes(slotMinutes)))
            {
                TableRow row = new TableRow();
                row.Cells.Add(new TableCell { Text = DateTime.Today.Add(t).ToString("hh:mm tt"), CssClass = "time-cell" });

                for (int i = 0; i < totalDays; i++)
                {
                    TableCell cell = new TableCell();
                    cell.Style["position"] = "relative";
                    cell.Attributes["data-time"] = t.ToString(@"hh\:mm");
                    cell.Attributes["data-date"] = startDate.AddDays(i).ToString("yyyy-MM-dd");
                    cell.Attributes["onclick"] = "highlightSlot(this);";

                    Panel container = new Panel { CssClass = "day-container" };
                    cell.Controls.Add(container);

                    DateTime slotDateTime = startDate.AddDays(i).Add(t);
                    if (slotDateTime >= DateTime.Now && slotDateTime < DateTime.Now.AddMinutes(slotMinutes))
                        cell.CssClass += " current-slot";

                    row.Cells.Add(cell);
                }
                table.Rows.Add(row);
            }

            // Load events
            var events = DataAccess.GetEventsInRange(startDate, endDate);
            RenderEvents(events, table, pixelsPerSlot);

            phCalendar.Controls.Add(table);
        }
        private void RenderEvents(List<CalendarEvent> events, Table table, int pixelsPerSlot)
        {
            if (events == null || table.Rows.Count < 2) return;

            TableRow header = table.Rows[0];

            foreach (var ev in events)
            {
                // Find the correct column for this event
                int colIndex = -1;
                for (int c = 1; c < header.Cells.Count; c++)
                {
                    if (DateTime.Parse(header.Cells[c].Attributes["data-date"]).Date == ev.StartTime.Date)
                    {
                        colIndex = c;
                        break;
                    }
                }
                if (colIndex == -1) continue;

                // Calculate top position and height in pixels
                int top = (int)((ev.StartTime.TimeOfDay - startTime).TotalMinutes / slotMinutes) * pixelsPerSlot;
                int height = (int)((ev.EndTime - ev.StartTime).TotalMinutes / slotMinutes) * pixelsPerSlot;

                // Determine status class
                string statusClass = "status-pending";
                if (!string.IsNullOrEmpty(ev.Status))
                {
                    string st = ev.Status.ToLower();
                    if (st == "pending") statusClass = "status-pending";
                    else if (st == "confirmed") statusClass = "status-confirmed";
                    else if (st == "cancelled") statusClass = "status-cancelled";
                }

                // Build HTML for the event
                string divHtml = string.Format(
                    "<div class='event-box {0}' data-id='{1}' style='top:{2}px;height:{3}px;left:2px;right:2px;position:absolute;'>{4}</div>",
                    statusClass, ev.Id, top, height, ev.Title
                );

                // Add to the container inside the cell
                TableCell cell = table.Rows[1].Cells[colIndex]; // Make sure Rows[1] exists
                if (cell.Controls.Count > 0)
                {
                    Panel container = cell.Controls[0] as Panel;
                    if (container != null)
                    {
                        Literal lit = new Literal { Text = divHtml };
                        container.Controls.Add(lit);
                    }
                }
            }
        }

        //private void RenderEvents(List<CalendarEvent> events, Table table, int pixelsPerSlot)
        //{
        //    TableRow header = table.Rows[0];
        //    foreach (var ev in events)
        //    {
        //        int colIndex = -1;
        //        for (int c = 1; c < header.Cells.Count; c++)
        //        {
        //            if (DateTime.Parse(header.Cells[c].Attributes["data-date"]).Date == ev.StartTime.Date)
        //            {
        //                colIndex = c;
        //                break;
        //            }
        //        }
        //        if (colIndex == -1) continue;

        //        int top = (int)((ev.StartTime.TimeOfDay - startTime).TotalMinutes / slotMinutes) * pixelsPerSlot;
        //        int height = (int)((ev.EndTime - ev.StartTime).TotalMinutes / slotMinutes) * pixelsPerSlot;

        //        Panel div = new Panel { CssClass = "event-box" };
        //        div.Style["top"] = top + "px";
        //        div.Style["height"] = height + "px";
        //        div.Style["left"] = "2px"; div.Style["right"] = "2px";
        //        div.Attributes["data-id"] = ev.Id.ToString();
        //        div.Controls.Add(new Literal { Text = ev.Title });

        //        switch (ev.Status.ToLower())
        //        {
        //            case "pending": div.CssClass += " status-pending"; break;
        //            case "confirmed": div.CssClass += " status-confirmed"; break;
        //            case "cancelled": div.CssClass += " status-cancelled"; break;
        //        }

        //        Panel container = table.Rows[1].Cells[colIndex].Controls[0] as Panel;
        //        container.Controls.Add(div);
        //    }
        //}

        //[WebMethod]
        //public static void SaveEvent(string title, string date, string time, string status)
        //{
        //    DateTime start = DateTime.Parse(date + " " + time);
        //    DateTime end = start.AddMinutes(30); // default 30 mins
        //    using (var conn = new SqlConnection("YOUR_CONNECTION_STRING"))
        //    {
        //        string sql = "INSERT INTO Events(Title,StartTime,EndTime,Status) VALUES(@title,@start,@end,@status)";
        //        SqlCommand cmd = new SqlCommand(sql, conn);
        //        cmd.Parameters.AddWithValue("@title", title);
        //        cmd.Parameters.AddWithValue("@start", start);
        //        cmd.Parameters.AddWithValue("@end", end);
        //        cmd.Parameters.AddWithValue("@status", status);
        //        conn.Open(); cmd.ExecuteNonQuery();
        //    }
        //}
        [WebMethod]
        public static void SaveEvent(string title, string date, string startTime, string endTime, string status)
        {
            string connString = WebConfigurationManager.ConnectionStrings["CalendarDB"].ConnectionString;
            DateTime start = DateTime.Parse(date + " " + startTime);
            DateTime end = DateTime.Parse(date + " " + endTime);
            using (var conn = new SqlConnection(connString))
            {
                string sql = "INSERT INTO Events(Title,StartTime,EndTime,Status) VALUES(@title,@start,@end,@status)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                cmd.Parameters.AddWithValue("@status", status);
                conn.Open(); cmd.ExecuteNonQuery();
            }
        }
    }
}