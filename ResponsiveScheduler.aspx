<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ResponsiveScheduler.aspx.cs" Inherits="CalendarScheduler.ResponsiveScheduler" %>

<%--<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ResponsiveScheduler.Default" %>--%>

<!DOCTYPE html>
<html>
<head id="Head1" runat="server">
    <title>Responsive Scheduler</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdnjs.cloudflare.com/ajax/libs/jqueryui/1.13.2/jquery-ui.min.css" rel="stylesheet" />
    <script src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.0/jquery.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/jqueryui/1.13.2/jquery-ui.min.js"></script>
     <link href="calendar.css" rel="stylesheet" />
    <style>
        .calendar-table { width:100%; table-layout:fixed; border-collapse:collapse; }
        .calendar-table th, .calendar-table td { border:1px solid #ddd; padding:0; position:relative; }
        .time-cell { width:60px; background:#f8f9fa; text-align:center; font-weight:bold; }
        .day-container { position:relative; height:20px; cursor:pointer; }
        .current-slot { background:#fff3cd; }
        /*.slot-selected { background:#f5f10a; }*/
        .slot-selected {
            background-color: #ffeb3b; /* Yellow highlight */
            transition: background-color 0.3s;
        }
        .event-box { position:absolute; color:#fff; border-radius:4px; font-size:12px; padding:2px; cursor:move; overflow:hidden; }
        .status-pending { background-color:#17a2b8; }
        .status-confirmed { background-color:#28a745; }
        .status-cancelled { background-color:#dc3545; }
       
.day-container {
    cursor: pointer;
}
/* Make header cells resizable */
.resizable-col {
    position: relative;
    overflow: hidden;
}

/* Drag handle */
.resizable-col::after {
    content: "";
    position: absolute;
    right: 0;
    top: 0;
    width: 5px;
    height: 100%;
    cursor: col-resize;
    z-index: 10;
    background-color: transparent; /* invisible but clickable */
}

/* ----------------------------
  Dialog title styling
---------------------------- */
.custom-dialog .ui-dialog-titlebar {
    background-color: #2699b3;   /* Green header */
    color: white;                /* White text */
    font-size: 18px;
    font-weight: bold;
    text-align: center;
    border: none;
    border-radius: 6px 6px 0 0;
    padding: 8px 10px;
}

/* ----------------------------
  Dialog buttons styling
---------------------------- */
.custom-dialog .ui-dialog-buttonpane button {
    background-color: #2699b3;
    color: white;
    border: none;
    padding: 6px 12px;
    margin: 2px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
}

.custom-dialog .ui-dialog-buttonpane button:hover {
    background-color: #2699b3;
}

/* ----------------------------
  Panel container
---------------------------- */
.popup-panel {
    border: 1px solid #ccc;
    border-radius: 6px;
    background-color: #f9f9f9;
    padding: 15px;
    box-shadow: 0 2px 6px rgba(0,0,0,0.1);
    margin-top: 5px;
}

/* Panel header */
.popup-panel .panel-header {
    font-size: 16px;
    font-weight: bold;
    margin-bottom: 10px;
    color: #333;
    border-bottom: 1px solid #ddd;
    padding-bottom: 5px;
}

/* Panel body labels and inputs */
.popup-panel .panel-body label {
    display: block;
    margin-top: 10px;
    margin-bottom: 5px;
    font-weight: 500;
    color: #555;
}

/* Text inputs and textarea styling */
.popup-panel .panel-body input,
.popup-panel .panel-body textarea {
    width: 100%;
    padding: 8px 10px;
    border: 1px solid #ccc;
    border-radius: 4px;
    box-sizing: border-box;
    font-size: 14px;
    font-family: inherit;
}

/* Textarea specific tweaks */
.popup-panel .panel-body textarea {
    resize: vertical; /* allow vertical resizing */
    min-height: 60px;
}
/* Close button on top-right */
.popup-panel .panel-close {
    float: right;
    font-size: 20px;
    font-weight: bold;
    cursor: pointer;
    color: #999;
    margin-top: -2px;
}

.popup-panel .panel-close:hover {
    color: #333;
}

/* Panel footer buttons */
.popup-panel .panel-footer {
    margin-top: 15px;
    text-align: right;
}

.popup-panel .panel-footer button {
    background-color: #2699b3;
    color: white;
    border: none;
    padding: 6px 12px;
    margin-left: 5px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
}

.popup-panel .panel-footer button:hover {
    background-color: #2699b3;
}

.popup-panel .btn-cancel {
    background-color: #2699b3;
}

.popup-panel .btn-cancel:hover {
    background-color: #2699b3;
}
    </style>
    <script>
        // Highlight clicked slot
        function highlightSlot(cell) {
            $(".day-container").removeClass("slot-selected");
            $(cell).find(".day-container").addClass("slot-selected");
            var date = $(cell).data("date");
            var time = $(cell).data("time");
            openEventPopup(date, time);
        }

        // Open popup to create/edit event
        function openEventPopup(date, startTime, endTime) {
            var popup = $("<div title='Create Event'></div>");
            popup.html(
                "<div class='popup-panel'>" +
                    "<div class='panel-header'>" +
                        "Task Details" +
                        "<span class='panel-close'>&times;</span>" + // close button
                    "</div>" +
                    "<div class='panel-body'>" +
                        "<label>Task:</label>" +
                        "<input type='text' id='eventTitle' class='form-control' /><br/>" +

                        "<label>Description:</label>" +
                        "<textarea id='eventDescription' class='form-control' rows='4' placeholder='Enter description'></textarea>" +
                    "</div>" +
                    "<div class='panel-footer'>" +  // buttons inside panel
                        "<button class='btn-save'>Save</button>" +
                        "<button class='btn-cancel'>Cancel</button>" +
                    "</div>" +
                "</div>"
            );

            popup.dialog({
                modal: true,
                width: 400,
                dialogClass: "custom-dialog",
                open: function () {
                    // remove default jQuery UI close button
                    $(this).parent().find(".ui-dialog-titlebar-close").hide();

                    // close button click
                    $(".panel-close").click(function () {
                        popup.dialog("close");
                    });

                    // Save button click
                    $(".btn-save").click(function () {
                        var title = $("#eventTitle").val();
                        var description = $("#eventDescription").val();

                        $.ajax({
                            type: "POST",
                            url: "Default.aspx/SaveEvent",
                            data: JSON.stringify({
                                title: title,
                                date: date,
                                startTime: startTime,
                                endTime: endTime,
                                description: description
                            }),
                            contentType: "application/json; charset=utf-8",
                            dataType: "json",
                            success: function () {
                                location.reload();
                            }
                        });
                        popup.dialog("close");
                    });

                    // Cancel button click
                    $(".btn-cancel").click(function () {
                        popup.dialog("close");
                    });
                }
            });
        }
       

        $(function () {
            var current = $(".current-slot:first");
            if (current.length) $('html, body').animate({ scrollTop: current.offset().top - 100 }, 500);

            $(".event-box").draggable({ grid: [0, 20], containment: ".calendar-table" })
                            .resizable({ handles: "s", grid: 20 });
        });
    </script>
    <script>
        $(function () {
            $(".event-box").draggable({
                grid: [0, 20],
                containment: ".calendar-table",
                stop: function (event, ui) {
                    // You can call AJAX here to save new position
                    var id = $(this).data("id");
                    // Calculate new start/end based on ui.position.top
                }
            }).resizable({
                handles: "s",
                grid: 20,
                stop: function (event, ui) {
                    var id = $(this).data("id");
                    // Calculate new end time based on new height and save via AJAX
                }
            });
        });
</script>
    <script>
        var isSelecting = false;
        var startCell = null;
        var selectedCells = [];

        $(document).ready(function() {

            // Start selection
            $(".day-container").on("mousedown", function(e) {
                isSelecting = true;
                startCell = this;
                selectedCells = [this];
                $(this).addClass("slot-selected");
                e.preventDefault(); // prevent text selection
            });

            // Mouseover while selecting
            $(".day-container").on("mouseover", function() {
                if (isSelecting) {
                    if (!selectedCells.includes(this)) {
                        $(this).addClass("slot-selected");
                        selectedCells.push(this);
                    }
                }
            });

            // Finish selection
            $(document).on("mouseup", function() {
                if (isSelecting) {
                    isSelecting = false;
                    if (selectedCells.length > 0) {
                        // Get start and end times from data attributes
                        var firstCell = $(selectedCells[0]);
                        var lastCell = $(selectedCells[selectedCells.length - 1]);
                       
                        var date = firstCell.closest("td").data("date");
                        var startTime = firstCell.data("time");
                        var endTime = lastCell.data("time");

                        // Open popup with highlighted slots
                        openEventPopup(date, startTime, endTime);

                        // Remove highlight after popup
                        $(".slot-selected").removeClass("slot-selected");
                        selectedCells = [];
                    }
                }
            });

        });
</script>
    
</head>
<body>
    <form id="form1" runat="server" class="container mt-3">
        <div class="mb-2">
            Start: <asp:TextBox ID="txtStartTime" runat="server" Text="08:00" Width="60px"></asp:TextBox>
            End: <asp:TextBox ID="txtEndTime" runat="server" Text="17:00" Width="60px"></asp:TextBox>
            Slot: <asp:DropDownList ID="ddlSlotMinutes" runat="server" AutoPostBack="true" 
                    OnSelectedIndexChanged="ddlSlotMinutes_SelectedIndexChanged">
                <asp:ListItem Text="10" Value="10" />
                <asp:ListItem Text="15" Value="15" Selected="True" />
                <asp:ListItem Text="30" Value="30" />
            </asp:DropDownList>
            <asp:Button ID="btnApplyTime" runat="server" Text="Apply" CssClass="btn btn-primary btn-sm" OnClick="btnApplyTime_Click" />
        </div>

        <div class="mb-2">
            <asp:Button ID="btnDay" runat="server" Text="Day" CssClass="btn btn-secondary btn-sm" OnClick="btnDay_Click" />
            <asp:Button ID="btnWeek" runat="server" Text="Week" CssClass="btn btn-secondary btn-sm" OnClick="btnWeek_Click" />
            <asp:Button ID="btnMonth" runat="server" Text="Month" CssClass="btn btn-secondary btn-sm" OnClick="btnMonth_Click" />
            From: <asp:TextBox ID="txtFrom" runat="server" Width="100px"></asp:TextBox>
            To: <asp:TextBox ID="txtTo" runat="server" Width="100px"></asp:TextBox>
            <asp:Button ID="btnCustom" runat="server" Text="Go" CssClass="btn btn-primary btn-sm" OnClick="btnCustom_Click" />
        </div>

        <asp:PlaceHolder ID="phCalendar" runat="server"></asp:PlaceHolder>
    </form>
    <script src="calendar.js"></script>
</body>
</html>