using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
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
        public  DataTable ToDataTable<T>(List<T> items)
        {
            DataTable table = new DataTable(typeof(T).Name);

            if (items == null || items.Count == 0)
                return table;

            // Get all the properties of the object
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo prop in properties)
            {
                // Create a column for each property
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (T item in items)
            {
                var values = new object[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    values[i] = properties[i].GetValue(item, null) ?? DBNull.Value;
                }
                table.Rows.Add(values);
            }

            return table;
        }

        private void GenerateSchedulerView(DateTime startDate, DateTime endDate)
        {
            phCalendar.Controls.Clear();
            currentStartDate = startDate;
            currentEndDate = endDate;
            int totalDays = (endDate - startDate).Days + 1;

            Table table = new Table { CssClass = "calendar-table", Width = Unit.Percentage(100) };

            // Header row
            TableRow header = new TableRow();
            TableCell timeHeader = new TableCell { Text = "Time" };
            timeHeader.Width = Unit.Pixel(80);
            header.Cells.Add(timeHeader);

            for (int i = 0; i < totalDays; i++)
            {
                DateTime day = startDate.AddDays(i);
                TableCell cell = new TableCell { Text = day.ToString("ddd dd/MM"), CssClass = "resizable-col" };
                cell.Attributes["data-date"] = day.ToString("yyyy-MM-dd");

                if (day.Date == DateTime.Today)
                {
                    cell.CssClass += " current-day";
                    cell.Width = Unit.Pixel(120); // wider current day
                }

                header.Cells.Add(cell);
            }
            table.Rows.Add(header);

            int pixelsPerSlot = 80; // each row height

            // Time rows
            for (TimeSpan t = startTime; t < endTime; t = t.Add(TimeSpan.FromMinutes(slotMinutes)))
            {
                TableRow row = new TableRow();
                row.Height = Unit.Pixel(pixelsPerSlot);

                TableCell timeCell = new TableCell { Text = DateTime.Today.Add(t).ToString("hh:mm tt"), CssClass = "time-cell" };
                timeCell.Width = Unit.Pixel(80);
                row.Cells.Add(timeCell);

                for (int i = 0; i < totalDays; i++)
                {
                    DateTime day = startDate.AddDays(i);
                    TableCell cell = new TableCell();
                    cell.Style["position"] = "relative";
                    cell.Attributes["data-time"] = t.ToString(@"hh\:mm");
                    cell.Attributes["data-date"] = day.ToString("yyyy-MM-dd");

                    if (day.Date == DateTime.Today)
                        cell.Style["width"] = "120px";

                    Panel container = new Panel { CssClass = "day-container" };
                    container.Style["position"] = "relative"; // needed for absolute event blocks
                    cell.Controls.Add(container);

                    row.Cells.Add(cell);
                }

                table.Rows.Add(row);
            }

            // Load events
            var events = DataAccess.GetEventsInRange(startDate, endDate);
            DataTable dtEvents = ToDataTable(events);
            RenderEvents(dtEvents, table, pixelsPerSlot);

            phCalendar.Controls.Add(table);
        }
        private void RenderEvents(DataTable events, Table table, int pixelsPerSlot)
        {
            if (events == null || events.Rows.Count == 0 || table.Rows.Count < 2) return;

            TableRow header = table.Rows[0];
            DateTime now = DateTime.Now;

            foreach (DataRow ev in events.Rows)
            {
                DateTime startTimeEv = Convert.ToDateTime(ev["StartTime"]);
                DateTime endTimeEv = Convert.ToDateTime(ev["EndTime"]);
                string status = ev["Status"] != DBNull.Value ? ev["Status"].ToString().ToLower() : "";
                string title = ev["Title"] != DBNull.Value ? ev["Title"].ToString() : "";

                // Find column index
                int colIndex = -1;
                for (int c = 1; c < header.Cells.Count; c++)
                {
                    string dateStr = header.Cells[c].Attributes["data-date"];
                    if (string.IsNullOrEmpty(dateStr)) continue;
                    if (DateTime.Parse(dateStr).Date == startTimeEv.Date)
                    {
                        colIndex = c;
                        break;
                    }
                }
                if (colIndex == -1) continue;

                // Calculate top position in pixels
                double totalTop = 0;
                int startRow = -1;
                for (int r = 1; r < table.Rows.Count; r++)
                {
                    TableCell cell = table.Rows[r].Cells[colIndex];
                    string slotTimeStr = cell.Attributes["data-time"];
                    if (string.IsNullOrEmpty(slotTimeStr)) continue;

                    TimeSpan slotTime = TimeSpan.Parse(slotTimeStr);
                    DateTime slotDateTime = startTimeEv.Date.Add(slotTime);

                    if (startRow == -1 && slotDateTime <= startTimeEv && startTimeEv < slotDateTime.Add(TimeSpan.FromMinutes(slotMinutes)))
                    {
                        startRow = r;
                        double minutesIntoSlot = (startTimeEv - slotDateTime).TotalMinutes;
                        totalTop = (r - 1) * pixelsPerSlot + (minutesIntoSlot / slotMinutes) * pixelsPerSlot;
                        break;
                    }
                }
                if (startRow == -1) continue;

                // Calculate height in pixels
                double eventDurationMinutes = (endTimeEv - startTimeEv).TotalMinutes;
                double height = (eventDurationMinutes / slotMinutes) * pixelsPerSlot;
                if (height < 20) height = 20;

                // Colors and progress
                string progressColor = "#4CAF50";
                string titleColor = "#000";
                if (status == "pending") { progressColor = "#FFC107"; titleColor = "#FF9800"; }
                else if (status == "cancelled") { progressColor = "#9E9E9E"; titleColor = "#757575"; }
                else if (status == "completed") { progressColor = "#4CAF50"; titleColor = "#2E7D32"; }
                if (now > endTimeEv && status != "completed") { progressColor = "#f44336"; titleColor = "#D32F2F"; }

                double totalMinutes = (endTimeEv - startTimeEv).TotalMinutes;
                double elapsedMinutes = (now - startTimeEv).TotalMinutes;
                if (elapsedMinutes < 0) elapsedMinutes = 0;
                if (elapsedMinutes > totalMinutes) elapsedMinutes = totalMinutes;
                int progressPercent = totalMinutes > 0 ? (int)((elapsedMinutes / totalMinutes) * 100) : 0;

                bool isInProgress = now >= startTimeEv && now <= endTimeEv && status != "completed";
                string blinkClass = isInProgress ? "blink" : "";

                // Add to first row's container
                TableCell firstCell = table.Rows[1].Cells[colIndex];
                if (firstCell.Controls.Count > 0)
                {
                    Panel container = firstCell.Controls[0] as Panel;
                    if (container != null)
                    {
                        string divHtml = @"
                        <div class='event-box {blinkClass}' style='position:absolute;top:{totalTop}px;height:{height}px;left:2px;right:2px;
                            border:1px solid #ccc;border-radius:6px;background:#fff;overflow:hidden;'>
                            <div style='height:20px;font-size:12px;font-weight:bold;padding-left:4px;color:{titleColor};
                                line-height:20px;overflow:hidden;text-overflow:ellipsis;'>{title}</div>
                            <div style='position:absolute;bottom:4px;left:4px;right:4px;height:4px;background:#eee;border-radius:2px;'>
                                <div style='height:4px;width:{progressPercent}%;background:{progressColor};border-radius:2px;
                                    transition:width 0.5s;'></div>
                            </div>
                        </div>";
                      container.Controls.Add(new Literal { Text = divHtml });
                    }
                }
            }
        }
        //=========================
        // 1. Generate Scheduler View
        //=========================
        //private void GenerateSchedulerView(DateTime startDate, DateTime endDate)
        //{
        //    phCalendar.Controls.Clear();
        //    currentStartDate = startDate;
        //    currentEndDate = endDate;
        //    int totalDays = (endDate - startDate).Days + 1;
        //    Table table = new Table { CssClass = "calendar-table", Width = Unit.Percentage(100) };

        //    // Header row
        //    TableRow header = new TableRow();
        //    TableCell timeHeader = new TableCell { Text = "Time" };
        //    timeHeader.Width = Unit.Pixel(80);
        //    header.Cells.Add(timeHeader);

        //    for (int i = 0; i < totalDays; i++)
        //    {
        //        DateTime day = startDate.AddDays(i);
        //        TableCell cell = new TableCell { Text = day.ToString("ddd dd/MM"), CssClass = "resizable-col" };
        //        cell.Attributes["data-date"] = day.ToString("yyyy-MM-dd");

        //        if (day.Date == DateTime.Today)
        //        {
        //            cell.CssClass += " current-day";
        //            cell.Width = Unit.Pixel(120); // wider current day
        //        }

        //        header.Cells.Add(cell);
        //    }
        //    table.Rows.Add(header);

        //    int pixelsPerSlot = 80; // row height

        //    // Time rows
        //    for (TimeSpan t = startTime; t < endTime; t = t.Add(TimeSpan.FromMinutes(slotMinutes)))
        //    {
        //        TableRow row = new TableRow();
        //        row.Height = Unit.Pixel(pixelsPerSlot);

        //        TableCell timeCell = new TableCell { Text = DateTime.Today.Add(t).ToString("hh:mm tt"), CssClass = "time-cell" };
        //        timeCell.Width = Unit.Pixel(80);
        //        row.Cells.Add(timeCell);

        //        for (int i = 0; i < totalDays; i++)
        //        {
        //            DateTime day = startDate.AddDays(i);
        //            TableCell cell = new TableCell();
        //            cell.Style["position"] = "relative";
        //            cell.Attributes["data-time"] = t.ToString(@"hh\:mm");
        //            cell.Attributes["data-date"] = day.ToString("yyyy-MM-dd");

        //            if (day.Date == DateTime.Today)
        //                cell.Style["width"] = "120px";

        //            Panel container = new Panel { CssClass = "day-container" };
        //            cell.Controls.Add(container);

        //            row.Cells.Add(cell);
        //        }

        //        table.Rows.Add(row);
        //    }

        //    // Load events
        //   // DataTable dtevents = GetData(startDate, endDate);
        //    var events = DataAccess.GetEventsInRange(startDate, endDate);
        //    DataTable dtEvents = ToDataTable(events);
        //    RenderEvents(dtEvents, table, pixelsPerSlot);

        //    phCalendar.Controls.Add(table);
        //}



        //=========================
        // 2. Render Events
        //=========================
        //        private void RenderEvents(DataTable events, Table table, int pixelsPerSlot)
        //        {
        //            if (events == null || events.Rows.Count == 0 || table.Rows.Count < 2) return;

        //            TableRow header = table.Rows[0];
        //            DateTime now = DateTime.Now;

        //            foreach (DataRow ev in events.Rows)
        //            {
        //                DateTime startTimeEv = Convert.ToDateTime(ev["StartTime"]);
        //                DateTime endTimeEv = Convert.ToDateTime(ev["EndTime"]);
        //                string status = ev["Status"] != DBNull.Value ? ev["Status"].ToString().ToLower() : "";
        //                string title = ev["Title"] != DBNull.Value ? ev["Title"].ToString() : "";

        //                // Column index
        //                int colIndex = -1;
        //                for (int c = 1; c < header.Cells.Count; c++)
        //                {
        //                    string dateStr = header.Cells[c].Attributes["data-date"];
        //                    if (string.IsNullOrEmpty(dateStr)) continue;
        //                    if (DateTime.Parse(dateStr).Date == startTimeEv.Date)
        //                    {
        //                        colIndex = c;
        //                        break;
        //                    }
        //                }
        //                if (colIndex == -1) continue;

        //                // Start and end rows
        //                int startRow = -1, endRow = -1;
        //                for (int r = 1; r < table.Rows.Count; r++)
        //                {
        //                    TableCell cell = table.Rows[r].Cells[colIndex];
        //                    string slotTimeStr = cell.Attributes["data-time"];
        //                    if (string.IsNullOrEmpty(slotTimeStr)) continue;
        //                    TimeSpan slotTime = TimeSpan.Parse(slotTimeStr);
        //                    DateTime slotDateTime = startTimeEv.Date.Add(slotTime);

        //                    if (startRow == -1 && slotDateTime >= startTimeEv) startRow = r;
        //                    if (slotDateTime < endTimeEv) endRow = r;
        //                }
        //                if (startRow == -1 || endRow == -1) continue;

        //                int rowSpan = endRow - startRow + 1;

        //                // Colors
        //                string progressColor = "#4CAF50";
        //                string titleColor = "#000";
        //                if (status == "pending") { progressColor = "#FFC107"; titleColor = "#FF9800"; }
        //                else if (status == "cancelled") { progressColor = "#9E9E9E"; titleColor = "#757575"; }
        //                else if (status == "completed") { progressColor = "#4CAF50"; titleColor = "#2E7D32"; }
        //                if (now > endTimeEv && status != "completed") { progressColor = "#f44336"; titleColor = "#D32F2F"; }

        //                double totalMinutes = (endTimeEv - startTimeEv).TotalMinutes;
        //                double elapsedMinutes = (now - startTimeEv).TotalMinutes;
        //                if (elapsedMinutes < 0) elapsedMinutes = 0;
        //                if (elapsedMinutes > totalMinutes) elapsedMinutes = totalMinutes;
        //                int progressPercent = totalMinutes > 0 ? (int)((elapsedMinutes / totalMinutes) * 100) : 0;

        //                bool isInProgress = now >= startTimeEv && now <= endTimeEv && status != "completed";
        //                string blinkClass = isInProgress ? "blink" : "";

        //                // Event cell
        //                TableCell eventCell = new TableCell();
        //                eventCell.RowSpan = rowSpan;
        //                eventCell.CssClass = "event-cell";
        //                eventCell.Style.Add("position", "relative");
        //                eventCell.Style.Add("padding", "0");

        //                Panel container = new Panel { CssClass = "day-container" };
        //                eventCell.Controls.Add(container);

        //                // Adjust width for current day
        //                TableRow headerRow = table.Rows[0];
        //                if (headerRow.Cells[colIndex].CssClass.Contains("current-day"))
        //                    container.Style["width"] = "116px"; // slightly smaller than 120px
        //                else
        //                    container.Style["width"] = "100%";

        //                string divHtml = @"
        //<div class='event-box {blinkClass}' style='position:absolute;top:0;bottom:0;left:2px;right:2px;
        //    border:1px solid #ccc;border-radius:6px;background:#fff;overflow:hidden;'>
        //    <div style='height:20px;font-size:12px;font-weight:bold;padding-left:4px;color:{titleColor};
        //        line-height:20px;overflow:hidden;text-overflow:ellipsis;'>{title}</div>
        //    <div style='position:absolute;bottom:4px;left:4px;right:4px;height:4px;background:#eee;border-radius:2px;'>
        //        <div style='height:4px;width:{progressPercent}%;background:{progressColor};border-radius:2px;
        //            transition:width 0.5s;'></div>
        //    </div>
        //</div>";

        //                container.Controls.Add(new Literal { Text = divHtml });

        //                TableRow startTableRow = table.Rows[startRow];
        //                startTableRow.Cells.RemoveAt(colIndex);
        //                startTableRow.Cells.AddAt(colIndex, eventCell);

        //                for (int r = startRow + 1; r <= endRow; r++)
        //                {
        //                    table.Rows[r].Cells[colIndex].Visible = false;
        //                }
        //            }
        //        }

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