namespace SinnersRelatos.Web.Models;

public static class TiposAccionAuditoria
{
    public const string Login = "Login";
    public const string LoginFallido = "Login fallido";

    public const string CrearUsuario = "Crear usuario";
    public const string ActualizarUsuario = "Actualizar usuario";
    public const string CambiarPassword = "Cambiar contraseña";
    public const string CambiarEstadoUsuario = "Cambiar estado de usuario";
    public const string ConfigurarPreguntaSeguridad = "Configurar pregunta de seguridad";
    public const string RestablecerPasswordPorPregunta = "Restablecer contraseña por pregunta de seguridad";
    public const string RestablecerPasswordFallido = "Intento fallido de recuperación de contraseña";

    public const string CrearCategoria = "Crear categoría";
    public const string ActualizarCategoria = "Actualizar categoría";
    public const string CambiarEstadoCategoria = "Cambiar estado de categoría";

    public const string CrearProducto = "Crear producto";
    public const string ActualizarProducto = "Actualizar producto";
    public const string CambiarEstadoProducto = "Cambiar estado de producto";

    public const string CrearGrupoModificador = "Crear grupo modificador";
    public const string ActualizarGrupoModificador = "Actualizar grupo modificador";
    public const string EliminarGrupoModificador = "Eliminar grupo modificador";
    public const string CrearOpcionModificador = "Crear opción modificador";
    public const string ActualizarOpcionModificador = "Actualizar opción modificador";
    public const string CambiarEstadoOpcionModificador = "Cambiar estado de opción modificador";

    public const string CrearIngrediente = "Crear ingrediente";
    public const string ActualizarIngrediente = "Actualizar ingrediente";
    public const string AjustarStock = "Ajustar stock";
    public const string CambiarEstadoIngrediente = "Cambiar estado de ingrediente";

    public const string ConfirmarPedido = "Confirmar pedido";
    public const string ForzarVenta = "Forzar venta sin stock";
    public const string AnularPedido = "Anular pedido";
    public const string CerrarPedido = "Cerrar cuenta / liberar mesa";
    public const string FusionarMesas = "Fusionar mesas";
}
