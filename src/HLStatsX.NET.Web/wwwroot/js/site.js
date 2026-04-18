// HLStatsX.NET - site JavaScript

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

    links.forEach(function (link) {
        link.addEventListener('click', function (e) {
            e.preventDefault();
            var target = this.getAttribute('href').substring(1); // strip leading #

            links.forEach(function (l) { l.classList.remove('active'); });
            panels.forEach(function (p) { p.classList.remove('active'); p.style.display = 'none'; });

            this.classList.add('active');
            var panel = document.getElementById(target);
            if (panel) { panel.classList.add('active'); panel.style.display = 'block'; }
        });
    });
}());
