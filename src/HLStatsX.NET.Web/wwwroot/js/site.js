// HLStatsX.NET - site JavaScript

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
