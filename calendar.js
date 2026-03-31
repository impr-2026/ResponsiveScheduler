$(function () {
    // Draggable / resizable events
    $(".event-box").draggable({ grid: [0, 20], containment: ".calendar-table" });
    $(".event-box").resizable({ handles: "s", grid: 20 });

    // Clickable slot
    $(".day-container").click(function (e) {
        $(".day-container").removeClass("slot-selected");
        $(this).addClass("slot-selected");
    });
});