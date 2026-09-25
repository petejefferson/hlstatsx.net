// HLStatsX.NET - site JavaScript

// Auto-wrap all data/stats tables in a scrollable container for mobile
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var tables = document.querySelectorAll('.data-table, .stats-table, .livestats-table');
        tables.forEach(function (table) {
            var parent = table.parentNode;
            if (!parent) return;
            // Skip if already inside a .table-responsive
            if (parent.classList.contains('table-responsive')) return;
            // Skip if the immediate parent already provides horizontal scrolling (inline style)
            if (parent.style.overflowX === 'auto' || parent.style.overflowX === 'scroll') return;
            var wrapper = document.createElement('div');
            wrapper.className = 'table-responsive';
            parent.insertBefore(wrapper, table);
            wrapper.appendChild(table);
        });
    });
}());

// Sortable tables: click any <th> in a table.sortable to sort by that column
(function () {
    function cellValue(row, idx) {
        var cell = row.cells[idx];
        if (!cell) return '';
        var raw = cell.getAttribute('data-sort') || cell.textContent || '';
        var n = parseFloat(raw.replace(/[^0-9.\-]/g, ''));
        return isNaN(n) ? raw.trim().toLowerCase() : n;
    }

    function attachSortable(table) {
        var ths = table.querySelectorAll('thead th');
        ths.forEach(function (th, idx) {
            th.style.cursor = 'pointer';
            var asc = true;
            th.addEventListener('click', function () {
                var tbody = table.querySelector('tbody');
                var rows = Array.from(tbody.querySelectorAll('tr'));
                rows.sort(function (a, b) {
                    var av = cellValue(a, idx), bv = cellValue(b, idx);
                    if (av < bv) return asc ? -1 : 1;
                    if (av > bv) return asc ? 1 : -1;
                    return 0;
                });
                rows.forEach(function (r) { tbody.appendChild(r); });
                ths.forEach(function (h) { h.textContent = h.textContent.replace(/ [▲▼]$/, ''); });
                th.textContent += asc ? ' ▲' : ' ▼';
                asc = !asc;
            });
        });
    }

    document.querySelectorAll('table.sortable').forEach(attachSortable);
}());

// Profile page tab switching
(function () {
    var tabs = document.getElementById('profileTabs');
    if (!tabs) return;

    var links = tabs.querySelectorAll('.tab-link');
    var panels = document.querySelectorAll('.tab-panel');
    var panelContainer = panels.length ? panels[0].parentElement : null;
    var maxPanelHeight = 0;

    function activateTab(hash) {
        var target = hash && hash.length > 1 ? hash.substring(1) : null;
        var panel = target ? document.getElementById(target) : null;
        if (!panel) return false;

        // Lock container height before hiding panels to prevent page jolt.
        panels.forEach(function (p) {
            if (p.offsetHeight > maxPanelHeight) maxPanelHeight = p.offsetHeight;
        });
        if (panelContainer && maxPanelHeight) {
            panelContainer.style.minHeight = maxPanelHeight + 'px';
        }

        links.forEach(function (l) { l.classList.remove('active'); });
        panels.forEach(function (p) { p.classList.remove('active'); p.style.display = 'none'; });

        var link = tabs.querySelector('a[href="' + hash + '"]');
        if (link) link.classList.add('active');
        panel.classList.add('active');
        panel.style.display = 'block';
        return true;
    }

    links.forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            activateTab(this.getAttribute('href'));
        });
    });

    if (!activateTab(window.location.hash)) {
        var firstLink = tabs.querySelector('.tab-link');
        if (firstLink) activateTab(firstLink.getAttribute('href'));
    }
}());
