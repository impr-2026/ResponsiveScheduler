<%@ Page Language="C#" AutoEventWireup="true" CodeFile="FullCalendarExample.cs" Inherits="CalendarScheduler.FullCalendarExample" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>Scheduler with Blink & Sound</title>

    <link href="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.8/index.global.min.css" rel="stylesheet" />
    <script src="https://cdn.jsdelivr.net/npm/fullcalendar@6.1.8/index.global.min.js"></script>

    <style>
        body { margin:20px; font-family: Arial; }
        #calendar { max-width: 1100px; margin:auto; }

        .controls { text-align:center; margin-bottom:10px; }

        .fc-timegrid-slot { height: 45px !important; }
        .fc-timegrid-slot-lane { border-top: 1px solid #ccc !important; }
        .current-slot { background-color: rgba(255,235,59,0.25) !important; }
        .fc-timegrid-now-indicator-line { border-top: 3px solid red !important; }
        .fc-event { border-radius: 8px !important; padding: 4px !important; font-size: 12px !important; overflow: hidden !important; }
        .locked-event { opacity: 0.6; cursor: not-allowed !important; }
        .progress-bar { height: 5px; background:#eee; border-radius:4px; margin-top:2px; }
        .progress-fill { height:100%; border-radius:4px; }
        .progress-text { font-size:10px; text-align:right; }
        @keyframes pulseGlow { 0%{box-shadow:0 0 0px rgba(76,175,80,0.7);} 50%{box-shadow:0 0 10px rgba(76,175,80,1);} 100%{box-shadow:0 0 0px rgba(76,175,80,0.7);} }
        .blink-event { animation: pulseGlow 1.2s infinite; }
    </style>
</head>
<body>

<div class="controls">
    Slot:
    <select id="slotDuration">
        <option value="00:10:00">10 min</option>
        <option value="00:15:00" selected>15 min</option>
        <option value="00:30:00">30 min</option>
        <option value="01:00:00">60 min</option>
    </select>

    From: <input type="date" id="fromDate">
    To: <input type="date" id="toDate">
    <button id="applyRange">Apply</button>
</div>

<div id="calendar"></div>

<!-- 🔊 Audio for notifications -->
<%--<audio id="notifSound" src="https://www.soundjay.com/button/beep-07.mp3" preload="auto"></audio>--%>

<script>
    let calendar;
    const slotSelector = document.getElementById('slotDuration');
   /* const notifSound = document.getElementById('notifSound');*/
    let notifiedTasks = {}; // track which tasks have triggered sound

    renderCalendar(slotSelector.value);

    slotSelector.addEventListener('change', () => renderCalendar(slotSelector.value));

    document.getElementById('applyRange').onclick = () => {
        const from = document.getElementById('fromDate').value;
        const to = document.getElementById('toDate').value;
        if (!from || !to) { alert('Select dates'); return; }
        const start = new Date(from);
        const end = new Date(to);
        end.setDate(end.getDate() + 1);
        renderCalendar(slotSelector.value, start, end);
    };

    function renderCalendar(slotDuration, start = null, end = null) {
        if (calendar) calendar.destroy();
        notifiedTasks = {};

        calendar = new FullCalendar.Calendar(document.getElementById('calendar'), {
            initialView: start ? 'customRange' : 'timeGridWeek',
            slotDuration: slotDuration,
            slotLabelInterval: slotDuration,
            slotMinTime: "00:00:00",
            slotMaxTime: "24:00:00",
            expandRows: true,
            allDaySlot: false,
            nowIndicator: true,
            scrollTime: new Date().toISOString().substring(11, 19),
            editable: true,
            headerToolbar: { left: 'prev,next today', center: 'title', right: 'dayGridMonth,timeGridWeek,timeGridDay' },
            views: { customRange: { type: 'timeGrid', duration: { days: 1 } } },
            visibleRange: start ? { start, end } : undefined,
            eventAllow: (dropInfo, event) => event.extendedProps.status === 'draft',
            datesSet: () => {
                setTimeout(() => {
                    document.querySelectorAll('.current-slot').forEach(el => el.classList.remove('current-slot'));
                    const now = new Date();
                    const h = now.getHours(), m = now.getMinutes();
                    document.querySelectorAll('.fc-timegrid-slot').forEach(slot => {
                        const time = slot.getAttribute('data-time');
                        if (!time) return;
                        const [sh, sm] = time.split(':').map(Number);
                        if (sh === h && Math.abs(sm - m) < 15) slot.classList.add('current-slot');
                    });
                }, 200);
            },
            events: function (fetchInfo, successCallback) {
                fetch('FullCalendarExample.aspx/GetEvents', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ start: fetchInfo.startStr, end: fetchInfo.endStr })
                })
                    .then(res => res.json())
                    .then(data => {
                        const now = new Date();
                        const events = (data.d || []).map(ev => {
                            const s = new Date(ev.start);
                            const e = new Date(ev.end);
                            const status = (ev.status || '').toLowerCase();
                            let color = '#4CAF50';
                            if (status === 'pending') color = '#FFC107';
                            if (status === 'cancelled') color = '#9E9E9E';
                            if (status === 'draft') color = '#90A4AE';
                            const total = (e - s) / 60000;
                            let elapsed = (now - s) / 60000;
                            elapsed = Math.max(0, Math.min(total, elapsed));
                            const percent = total > 0 ? Math.round((elapsed / total) * 100) : 0;
                            const isRunning = now >= s && now <= e && status !== 'completed';

                            //// 🔊 Sound notification for start
                            //if (isRunning && !notifiedTasks[ev.id]) {
                            //    notifSound.play();
                            //    notifiedTasks[ev.id] = true;
                            //}

                            return {
                                id: ev.id,
                                title: ev.title,
                                start: s,
                                end: e,
                                editable: status === 'draft',
                                classNames: [status !== 'draft' ? 'locked-event' : '', isRunning ? 'blink-event' : ''],
                                extendedProps: { percent, color, status, isRunning }
                            };
                        });
                        successCallback(events);
                    });
            },
            eventContent: function (arg) {
                const p = arg.event.extendedProps.percent;
                const c = arg.event.extendedProps.color;
                const el = document.createElement('div');
                el.innerHTML = `
                <div><b>${arg.event.title}</b></div>
                <div class="progress-bar">
                    <div class="progress-fill" style="width:${p}%;background:${c}"></div>
                </div>
                <div class="progress-text">${p}%</div>
            `;
                return { domNodes: [el] };
            },
            eventDrop: function (info) {
                if (info.event.extendedProps.status !== 'draft') { alert('Only Draft can move'); info.revert(); return; }
                fetch('FullCalendarExample.aspx/UpdateEvent', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: info.event.id, start: info.event.start.toISOString(), end: info.event.end.toISOString() })
                });
            }
        });

        calendar.render();

        // Live progress + blink + sound
        setInterval(() => {
            const now = new Date();
            calendar.getEvents().forEach(e => {
                const s = new Date(e.start);
                const en = new Date(e.end);
                let total = (en - s) / 60000;
                let elapsed = (now - s) / 60000;
                elapsed = Math.max(0, Math.min(total, elapsed));
                const percent = total > 0 ? Math.round((elapsed / total) * 100) : 0;
                e.setExtendedProp('percent', percent);

                // 🔊 Play sound if newly started
                if (now >= s && now <= en && !notifiedTasks[e.id]) {
                    notifSound.play();
                    notifiedTasks[e.id] = true;
                }
            });
        }, 10000); // check every 10 seconds
    }
</script>

</body>
</html>