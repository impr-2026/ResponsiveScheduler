using CalendarScheduler;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Default : System.Web.UI.Page
{
    // Configurable slot interval and time range
    private int slotMinutes = 10;
    private TimeSpan startTime = TimeSpan.FromHours(8);
    private TimeSpan endTime = TimeSpan.FromHours(17);

    // Keep track of current view range
    private DateTime currentStartDate;
    private DateTime currentEndDate;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            slotMinutes = int.Parse(ddlSlotMinutes.SelectedValue);
            DateTime startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            DateTime endOfWeek = startOfWeek.AddDays(6);
            GenerateSchedulerView(startOfWeek, endOfWeek);
        }
    }

    // Apply time range
    protected void btnApplyTime_Click(object sender, EventArgs e)
    {
        TimeSpan.TryParse(txtStartTime.Text, out startTime);
        TimeSpan.TryParse(txtEndTime.Text, out endTime);
        slotMinutes = int.Parse(ddlSlotMinutes.SelectedValue);
        GenerateSchedulerView(currentStartDate, currentEndDate);
    }

    // Day/Week/Month/Custom buttons
    protected void btnDay_Click(object sender, EventArgs e) { GenerateSchedulerView(DateTime.Today, DateTime.Today); }
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
        DateTime from = DateTime.Parse(txtFrom.Text);
        DateTime to = DateTime.Parse(txtTo.Text);
        GenerateSchedulerView(from, to);
    }

    //private int slotMinutes
    //{
    //    get
    //    {
    //        if (ViewState["slotMinutes"] != null)
    //            return (int)ViewState["slotMinutes"];
    //        return 15; // default
    //    }
    //    set
    //    {
    //        ViewState["slotMinutes"] = value;
    //    }
    //}
    protected void ddlSlotMinutes_SelectedIndexChanged(object sender, EventArgs e)
   {
       // Save selected value to ViewState-backed property
       //if (int.TryParse(ddlSlotMinutes.SelectedValue, out int minutes))
       // {
       // slotMinutes = minutes;
       //}
       //else
       //{
       //   slotMinutes = 15; // fallback
       // }

    // Re-render the scheduler with the current view
    GenerateSchedulerView(currentStartDate, currentEndDate);
   }

    // Scheduler rendering
    private void GenerateSchedulerView(DateTime startDate, DateTime endDate){
    phCalendar.Controls.Clear();
    currentStartDate = startDate;
    currentEndDate = endDate;
    int totalDays = (endDate - startDate).Days + 1;
    Table table = new Table { CssClass="calendar-table" };

    // Header
    TableRow header = new TableRow();
    header.Cells.Add(new TableCell{ Text="Time" });
    for(int i=0;i<totalDays;i++){
        DateTime day = startDate.AddDays(i);
        TableCell cell = new TableCell{ Text=day.ToString("ddd dd/MM"), CssClass="resizable-col" };
        cell.Attributes["data-date"]=day.ToString("yyyy-MM-dd");
        if(day.Date==DateTime.Today) cell.CssClass+=" current-day";
        header.Cells.Add(cell);
    }
    table.Rows.Add(header);

    int pixelsPerSlot = 20;
    for(TimeSpan t=startTime;t<endTime;t=t.Add(TimeSpan.FromMinutes(slotMinutes))){
        TableRow row = new TableRow();
        row.Cells.Add(new TableCell{ Text=DateTime.Today.Add(t).ToString("hh:mm tt"), CssClass="time-cell" });
        for(int i=0;i<totalDays;i++){
            TableCell cell = new TableCell();
            cell.Style["position"] = "relative";
            cell.Attributes["data-time"]=t.ToString(@"hh\:mm");
            cell.Attributes["onclick"]="highlightSlot(this);";

            Panel container = new Panel{ CssClass="day-container" };
            cell.Controls.Add(container);

            DateTime slotDateTime = startDate.AddDays(i).Add(t);
            if(slotDateTime>=DateTime.Now && slotDateTime<DateTime.Now.AddMinutes(slotMinutes))
                cell.CssClass+=" current-slot";

            row.Cells.Add(cell);
        }
        table.Rows.Add(row);
    }

    // Render events
    var events = DataAccess.GetEventsInRange(startDate, endDate);
    RenderEvents(events, table, pixelsPerSlot);

    phCalendar.Controls.Add(table);
}

    // RenderEvents
    private void RenderEvents(List<CalendarEvent> events, Table table, int pixelsPerSlot)
    {
        TableRow header = table.Rows[0];
        foreach (var ev in events)
        {
            int colIndex = -1;
            for (int c = 1; c < header.Cells.Count; c++)
            {
                DateTime cellDate = DateTime.Parse(header.Cells[c].Attributes["data-date"]);
                if (cellDate.Date == ev.StartTime.Date) { colIndex = c; break; }
            }
            if (colIndex == -1) continue;

            int top = (int)((ev.StartTime.TimeOfDay - startTime).TotalMinutes / slotMinutes) * pixelsPerSlot;
            int height = (int)((ev.EndTime - ev.StartTime).TotalMinutes / slotMinutes) * pixelsPerSlot;

            Panel div = new Panel { CssClass = "event-box" };
            switch (ev.Status.ToLower())
            {
                case "pending": div.CssClass += " status-pending"; break;
                case "confirmed": div.CssClass += " status-confirmed"; break;
                case "cancelled": div.CssClass += " status-cancelled"; break;
            }
            div.Style["top"] = top + "px";
            div.Style["height"] = height + "px";
            div.Style["left"] = "2px"; div.Style["right"] = "2px";
            div.Attributes["data-id"] = ev.Id.ToString();
            div.Controls.Add(new Literal { Text = ev.Title });

            Panel container = table.Rows[1].Cells[colIndex].Controls[0] as Panel;
            container.Controls.Add(div);
        }
    }
}
