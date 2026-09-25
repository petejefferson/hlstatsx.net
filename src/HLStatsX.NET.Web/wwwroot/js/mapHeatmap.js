(function () {
    'use strict';

    var opts = window.heatmapOptions;
    if (!opts) return;

    var container = document.getElementById('heatmap-container');
    var img = document.getElementById('heatmap-mapimg');
    var statusEl = document.getElementById('heatmap-status');
    if (!container || !img || !statusEl) return;

    var viewport = document.getElementById('heatmap-viewport');

    var currentType = 'kills';
    var cachedData = null;
    var heatmapInstance = null;
    var lastAutoBounds = null;

    // Zoom / pan state
    var zoomScale = 1, panX = 0, panY = 0;
    var containerNW = 0, containerNH = 0;
    var dragging = false, dragStartX = 0, dragStartY = 0, dragPanX = 0, dragPanY = 0;

    function clampPan() {
        if (!viewport || !containerNW) return;
        var vw = viewport.offsetWidth;
        var vh = viewport.offsetHeight;
        var minScale = Math.max(vw / containerNW, vh / containerNH);
        if (zoomScale < minScale) zoomScale = minScale;
        panX = Math.min(0, Math.max(vw - containerNW * zoomScale, panX));
        panY = Math.min(0, Math.max(vh - containerNH * zoomScale, panY));
    }

    function applyTransform() {
        clampPan();
        container.style.transform = 'translate(' + panX + 'px,' + panY + 'px) scale(' + zoomScale + ')';
    }

    if (viewport) {
        viewport.addEventListener('wheel', function (e) {
            e.preventDefault();
            var factor = e.deltaY < 0 ? 1.25 : 1 / 1.25;
            var rect = viewport.getBoundingClientRect();
            var mx = e.clientX - rect.left;
            var my = e.clientY - rect.top;
            var newScale = zoomScale * factor;
            factor = newScale / zoomScale;
            panX = mx - (mx - panX) * factor;
            panY = my - (my - panY) * factor;
            zoomScale = newScale;
            applyTransform();
        }, { passive: false });

        viewport.addEventListener('mousedown', function (e) {
            dragging = true;
            dragStartX = e.clientX; dragStartY = e.clientY;
            dragPanX = panX; dragPanY = panY;
            viewport.style.cursor = 'grabbing';
            e.preventDefault();
        });
        window.addEventListener('mousemove', function (e) {
            if (!dragging) return;
            panX = dragPanX + (e.clientX - dragStartX);
            panY = dragPanY + (e.clientY - dragStartY);
            applyTransform();
        });
        window.addEventListener('mouseup', function () {
            if (!dragging) return;
            dragging = false;
            viewport.style.cursor = 'grab';
        });

        var zoomInBtn    = document.getElementById('zoom-in');
        var zoomOutBtn   = document.getElementById('zoom-out');
        var zoomResetBtn = document.getElementById('zoom-reset');

        function zoomCenter(factor) {
            var cx = viewport.offsetWidth  / 2;
            var cy = viewport.offsetHeight / 2;
            var newScale = zoomScale * factor;
            factor = newScale / zoomScale;
            panX = cx - (cx - panX) * factor;
            panY = cy - (cy - panY) * factor;
            zoomScale = newScale;
            applyTransform();
        }

        if (zoomInBtn)    zoomInBtn.addEventListener('click',    function () { zoomCenter(1.5); });
        if (zoomOutBtn)   zoomOutBtn.addEventListener('click',   function () { zoomCenter(1 / 1.5); });
        if (zoomResetBtn) zoomResetBtn.addEventListener('click', function () { zoomScale = 1; panX = 0; panY = 0; applyTransform(); });
    }

    // Transform using hlstats_Heatmap_Config calibration values.
    function calibratedTransform(points, cfg, displayW, displayH, naturalW, naturalH) {
        return points.map(function (p) {
            var wx = cfg.flipx ? -p.x : p.x;
            var wy = cfg.flipy ? -p.y : p.y;
            var px = (wx + cfg.xoffset) / cfg.scale;
            var py = (wy + cfg.yoffset) / cfg.scale;
            px = px * (displayW / naturalW);
            py = py * (displayH / naturalH);
            return { x: Math.round(px), y: Math.round(py), value: p.value };
        }).filter(function (p) {
            return p.x >= 0 && p.x <= displayW && p.y >= 0 && p.y <= displayH;
        });
    }

    // Stretch coordinate range to fill the canvas when no calibration config exists.
    function autoTransform(points, displayW, displayH) {
        if (!points.length) return [];
        var xs = points.map(function (p) { return p.x; });
        var ys = points.map(function (p) { return p.y; });
        var minX = Math.min.apply(null, xs), maxX = Math.max.apply(null, xs);
        var minY = Math.min.apply(null, ys), maxY = Math.max.apply(null, ys);
        lastAutoBounds = { minX: minX, maxX: maxX, minY: minY, maxY: maxY };
        var rangeX = maxX - minX || 1;
        var rangeY = maxY - minY || 1;
        var pad = 0.05;
        var inner = 1 - 2 * pad;
        return points.map(function (p) {
            return {
                x: Math.round(((p.x - minX) / rangeX) * displayW * inner + displayW * pad),
                y: Math.round(((p.y - minY) / rangeY) * displayH * inner + displayH * pad),
                value: p.value
            };
        });
    }

    // Apply the same transform used for data points to a single {x,y} world coordinate.
    function transformPoint(x, y, cfg, displayW, displayH, naturalW, naturalH) {
        if (cfg && naturalW) {
            var wx = cfg.flipx ? -x : x;
            var wy = cfg.flipy ? -y : y;
            return {
                x: Math.round((wx + cfg.xoffset) / cfg.scale * (displayW / naturalW)),
                y: Math.round((wy + cfg.yoffset) / cfg.scale * (displayH / naturalH))
            };
        }
        if (lastAutoBounds) {
            var b = lastAutoBounds;
            var rangeX = b.maxX - b.minX || 1, rangeY = b.maxY - b.minY || 1;
            var pad = 0.05, inner = 0.9;
            return {
                x: Math.round(((x - b.minX) / rangeX) * displayW * inner + displayW * pad),
                y: Math.round(((y - b.minY) / rangeY) * displayH * inner + displayH * pad)
            };
        }
        return null;
    }

    function placeMarkers(cfg, displayW, displayH, naturalW, naturalH) {
        container.querySelectorAll('.hm-pin').forEach(function (el) { el.remove(); });
        if (!opts.markers || !opts.markers.length) return;

        // No kill/death data loaded yet — seed auto-bounds from the markers themselves
        // so they always appear on screen even without calibration config or map data.
        if (!cfg && !lastAutoBounds) {
            var mxs = opts.markers.filter(function (m) { return m.x != null; }).map(function (m) { return m.x; });
            var mys = opts.markers.filter(function (m) { return m.y != null; }).map(function (m) { return m.y; });
            if (mxs.length) {
                var pad = 500;
                lastAutoBounds = {
                    minX: Math.min.apply(null, mxs) - pad,
                    maxX: Math.max.apply(null, mxs) + pad,
                    minY: Math.min.apply(null, mys) - pad,
                    maxY: Math.max.apply(null, mys) + pad
                };
            }
        }

        opts.markers.forEach(function (m) {
            if (m.x == null || m.y == null) return;
            var pos = transformPoint(m.x, m.y, cfg, displayW, displayH, naturalW, naturalH);
            if (!pos) return;
            var color = m.color || '#ff2200';
            var el = document.createElement('div');
            el.className = 'hm-pin';
            el.style.cssText = 'position:absolute;left:' + pos.x + 'px;top:' + pos.y + 'px;'
                + 'transform:translate(-50%,-50%);pointer-events:none;z-index:10;text-align:center;';
            el.innerHTML = '<svg width="36" height="36" viewBox="0 0 36 36" xmlns="http://www.w3.org/2000/svg">'
                + '<circle cx="18" cy="18" r="13" fill="' + color + '" fill-opacity="0.8" stroke="#fff" stroke-width="2.5"/>'
                + '<line x1="18" y1="3" x2="18" y2="33" stroke="#fff" stroke-width="2.5"/>'
                + '<line x1="3" y1="18" x2="33" y2="18" stroke="#fff" stroke-width="2.5"/>'
                + '</svg>'
                + (m.label ? '<div style="margin-top:1px;color:#fff;font-size:11px;font-weight:bold;'
                    + 'text-shadow:0 0 4px #000,0 0 4px #000,0 0 4px #000;white-space:nowrap;">'
                    + m.label + '</div>' : '');
            container.appendChild(el);
        });

    }

    function render(data, type) {
        if (!heatmapInstance) return;

        var points = type === 'deaths' ? data.deaths : data.kills;
        var displayW = container.offsetWidth;
        var displayH = container.offsetHeight;

        var mapped;
        if (data.config && img.naturalWidth) {
            mapped = calibratedTransform(points, data.config, displayW, displayH, img.naturalWidth, img.naturalHeight);
        } else {
            mapped = autoTransform(points, displayW, displayH);
        }

        if (!mapped.length) {
            statusEl.textContent = 'No ' + type + ' position data for this map.';
            heatmapInstance.setData({ max: 1, data: [] });
        } else {
            statusEl.textContent = data.config ? '' : 'Auto-calibrated (no config row in hlstats_Heatmap_Config)';
            var maxVal = mapped.reduce(function (m, p) { return p.value > m ? p.value : m; }, 1);
            heatmapInstance.setData({ max: maxVal, data: mapped });
        }

        placeMarkers(data.config || null, displayW, displayH, img.naturalWidth || 0, img.naturalHeight || 0);
    }

    function ensureHeatmapInstance() {
        if (heatmapInstance) return;
        containerNW = img.offsetWidth;
        containerNH = img.offsetHeight;
        container.style.width  = containerNW + 'px';
        container.style.height = containerNH + 'px';
        if (viewport) viewport.style.height = containerNH + 'px';
        heatmapInstance = h337.create({
            container: container,
            radius: 20,
            maxOpacity: 0.7,
            minOpacity: 0,
            blur: 0.85
        });
    }

    function setTab(type) {
        currentType = type;
        document.getElementById('tab-kills').classList.toggle('heatmap-tab-active',   type === 'kills');
        document.getElementById('tab-kills').classList.toggle('heatmap-tab-inactive',  type !== 'kills');
        document.getElementById('tab-deaths').classList.toggle('heatmap-tab-active',  type === 'deaths');
        document.getElementById('tab-deaths').classList.toggle('heatmap-tab-inactive', type !== 'deaths');
        if (cachedData) {
            render(cachedData, type);
        } else {
            fetchAndRender(type);
        }
    }

    function fetchAndRender(type) {
        var url = opts.apiUrl
            + '?game=' + encodeURIComponent(opts.game)
            + '&map=' + encodeURIComponent(opts.map)
            + '&type=both'
            + (opts.playerId ? '&playerId=' + opts.playerId : '');

        statusEl.textContent = 'Loading…';

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.error) {
                    statusEl.textContent = 'Error: ' + data.error;
                    return;
                }
                cachedData = data;
                ensureHeatmapInstance();
                render(data, type);
            })
            .catch(function () {
                statusEl.textContent = 'Failed to load heatmap data.';
            });
    }

    document.getElementById('tab-kills').addEventListener('click', function () { setTab('kills'); });
    document.getElementById('tab-deaths').addEventListener('click', function () { setTab('deaths'); });

    function init() {
        fetchAndRender('kills');
    }

    function start() {
        if (img.complete && img.naturalWidth) { init(); }
        else { img.addEventListener('load', init); }
    }

    if (opts.deferInit) {
        window.heatmapInit = start;
    } else {
        start();
    }
}());
