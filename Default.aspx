<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="Default" %>
<!DOCTYPE html>
<html>
<head id="Head1" runat="server">
    <title>Weekly Calendar Scheduler</title>

    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet" />
<script src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.0/jquery.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/jqueryui/1.13.2/jquery-ui.min.js"></script>
<link href="https://cdnjs.cloudflare.com/ajax/libs/jqueryui/1.13.2/jquery-ui.min.css" rel="stylesheet" />

    <link href="calendar.css" rel="stylesheet" />
  
    <script type="text/javascript">
        function highlightSlot(cell) {
            // Remove previous selection
            var slots = document.getElementsByClassName("day-container");
            for (var i = 0; i < slots.length; i++) {
                slots[i].classList.remove("slot-selected");
            }

            // Highlight clicked slot
            var container = cell.querySelector(".day-container");
            if (container) container.classList.add("slot-selected");
        }
</script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="controls-panel mb-2">
    <asp:Label ID="lblStart" runat="server" Text="Start Time:"></asp:Label>
    <asp:TextBox ID="txtStartTime" runat="server" Width="60px" Text="08:00"></asp:TextBox>

    <asp:Label ID="lblEnd" runat="server" Text="End Time:"></asp:Label>
    <asp:TextBox ID="txtEndTime" runat="server" Width="60px" Text="17:00"></asp:TextBox>

    <asp:Label ID="lblSlot" runat="server" Text="Slot Minutes:"></asp:Label>
    <asp:DropDownList ID="ddlSlotMinutes" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlSlotMinutes_SelectedIndexChanged">
        <asp:ListItem Text="10" Value="10" />
        <asp:ListItem Text="15" Value="15" Selected="True" />
        <asp:ListItem Text="30" Value="30" />
    </asp:DropDownList>

    <asp:Button ID="btnApplyTime" runat="server" Text="Apply" CssClass="btn btn-primary btn-sm" OnClick="btnApplyTime_Click" />
    <asp:Button ID="btnDay" runat="server" Text="Day View" OnClick="btnDay_Click" CssClass="btn btn-secondary btn-sm" />
    <asp:Button ID="btnWeek" runat="server" Text="Week View" OnClick="btnWeek_Click" CssClass="btn btn-secondary btn-sm" />
    <asp:Button ID="btnMonth" runat="server" Text="Month View" OnClick="btnMonth_Click" CssClass="btn btn-secondary btn-sm" />

    <asp:Label ID="lblFrom" runat="server" Text="From:"></asp:Label>
    <asp:TextBox ID="txtFrom" runat="server" Width="100px"></asp:TextBox>
    <asp:Label ID="lblTo" runat="server" Text="To:"></asp:Label>
    <asp:TextBox ID="txtTo" runat="server" Width="100px"></asp:TextBox>
    <asp:Button ID="btnCustom" runat="server" Text="Go" OnClick="btnCustom_Click" CssClass="btn btn-primary btn-sm" />
</div>
<style>
.calendar-table { width:100%; table-layout: fixed; border-collapse: collapse; }
.calendar-table th, .calendar-table td { border:1px solid #ddd; padding:0; position:relative; }
.time-cell { width:60px; background:#f8f9fa; font-weight:bold; text-align:center; }
.day-container { width:100%; height:100%; cursor:pointer; position:relative; }
.current-slot { background:#fff3cd; }
.slot-selected { background:#ffeeba; }
.event-box { position:absolute; color:#fff; border-radius:4px; font-size:12px; padding:2px; cursor:move; overflow:hidden; }
.status-pending { background-color:#17a2b8; }
.status-confirmed { background-color:#28a745; }
.status-cancelled { background-color:#dc3545; }
</style>

<script>
    function highlightSlot(cell) {
        $(".day-container").removeClass("slot-selected");
        $(cell).find(".day-container").addClass("slot-selected");
    }

    $(function () {
        // Scroll to current slot on page load
        var current = $(".current-slot:first");
        if (current.length) $('html, body').animate({ scrollTop: current.offset().top - 100 }, 500);

        // Draggable & resizable events
        $(".event-box").draggable({
            grid: [0, 20],
            containment: ".calendar-table",
            stop: function (e, ui) { updateEventTime($(this).data("id"), ui.position.top); }
        }).resizable({
            handles: "s",
            grid: 20,
            stop: function (e, ui) { updateEventDuration($(this).data("id"), ui.size.height); }
        });
    });

    function updateEventTime(id, top) { console.log("Update Event " + id + " new top: " + top); }
    function updateEventDuration(id, height) { console.log("Update Event " + id + " new height: " + height); }
</script>
<asp:PlaceHolder ID="phCalendar" runat="server"></asp:PlaceHolder>
    </form>
    <script src="calendar.js"></script>
</body>
</html>