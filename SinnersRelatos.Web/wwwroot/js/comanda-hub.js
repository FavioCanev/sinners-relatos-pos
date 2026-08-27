window.comandaHub = (function () {
    let connection = null;
    let starting = null;
    const CLAVE_ESTACION = "sinnersRelatos.esEstacionImpresion";

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
        conn.on(eventName, (...args) => dotnetHelper.invokeMethodAsync(methodName, ...args));
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

    return { subscribe, esEstacionDeImpresion, marcarEstacionDeImpresion, imprimirSilencioso };
})();
