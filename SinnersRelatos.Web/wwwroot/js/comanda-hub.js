window.comandaHub = (function () {
    let connection = null;
    let starting = null;
    const CLAVE_ESTACION = "sinnersRelatos.esEstacionImpresion";

    // Cada suscripción se registra con un token propio para poder desuscribirla más tarde con
    // conn.off(eventName, handler) — sin esto, cada navegación (Mesas → Pedido → Mesas → Pedido)
    // dejaba un handler apuntando a un componente Blazor ya destruido, y con el tiempo cada
    // evento del hub terminaba invocando decenas de referencias muertas.
    let contadorSuscripciones = 0;
    const suscripciones = new Map();

    async function ensureConnected() {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            return connection;
        }

        if (!connection) {
            connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/comanda")
                .withAutomaticReconnect()
                .build();
        }

        if (!starting) {
            starting = connection.start().catch(err => {
                starting = null;
                throw err;
            });
        }

        await starting;
        return connection;
    }

    async function subscribe(eventName, dotnetHelper, methodName) {
        const conn = await ensureConnected();
        const handler = (...args) => dotnetHelper.invokeMethodAsync(methodName, ...args);
        conn.on(eventName, handler);

        const token = ++contadorSuscripciones;
        suscripciones.set(token, { eventName, handler });
        return token;
    }

    function unsubscribe(token) {
        const suscripcion = suscripciones.get(token);
        if (!suscripcion) return;

        suscripciones.delete(token);
        if (connection) {
            connection.off(suscripcion.eventName, suscripcion.handler);
        }
    }

    function esEstacionDeImpresion() {
        return localStorage.getItem(CLAVE_ESTACION) === "true";
    }

    function marcarEstacionDeImpresion(valor) {
        localStorage.setItem(CLAVE_ESTACION, valor ? "true" : "false");
    }

    function imprimirSilencioso(pedidoId) {
        const iframe = document.createElement("iframe");
        iframe.style.display = "none";
        iframe.src = `/print/comprobante/${pedidoId}?autoprint=true`;
        document.body.appendChild(iframe);
        setTimeout(() => iframe.remove(), 15000);
    }

    return { subscribe, unsubscribe, esEstacionDeImpresion, marcarEstacionDeImpresion, imprimirSilencioso };
})();
