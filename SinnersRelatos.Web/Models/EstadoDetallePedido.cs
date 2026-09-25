namespace SinnersRelatos.Web.Models;

// Valores explícitos: nunca reordenar ni reutilizar un número ya asignado, porque el estado se
// guarda como int en la base de datos (ver migración AgregarEstadoEnPreparacionDetallePedido).
public enum EstadoDetallePedido
{
    Recibido = 0,
    EnPreparacion = 1,
    Listo = 2
}
