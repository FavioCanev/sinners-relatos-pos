window.dashboardCharts = (function () {
    const instancias = {};

    function renderLinea(canvasId, etiquetas, datos, color) {
        destruir(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        instancias[canvasId] = new Chart(ctx, {
            type: "line",
            data: {
                labels: etiquetas,
                datasets: [{
                    label: "Ventas (S/)",
                    data: datos,
                    borderColor: color,
                    backgroundColor: color + "33",
                    fill: true,
                    tension: 0.25,
                    pointRadius: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true } }
            }
        });
    }

    function renderBarras(canvasId, etiquetas, datos, color) {
        destruir(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        instancias[canvasId] = new Chart(ctx, {
            type: "bar",
            data: {
                labels: etiquetas,
                datasets: [{
                    label: "Unidades vendidas",
                    data: datos,
                    backgroundColor: color
                }]
            },
            options: {
                indexAxis: "y",
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { x: { beginAtZero: true } }
            }
        });
    }

    function renderBarrasVerticales(canvasId, etiquetas, datos, color) {
        destruir(canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        instancias[canvasId] = new Chart(ctx, {
            type: "bar",
            data: {
                labels: etiquetas,
                datasets: [{
                    label: "Disponibles para preparar",
                    data: datos,
                    backgroundColor: color
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
            }
        });
    }

    function destruir(canvasId) {
        if (instancias[canvasId]) {
            instancias[canvasId].destroy();
            delete instancias[canvasId];
        }
    }

    return { renderLinea, renderBarras, renderBarrasVerticales };
})();
