using System.ComponentModel.DataAnnotations.Schema;

namespace SinnersRelatos.Web.Models;

// Combo de productos (ej. "Combo Desayuno" = 1x Americano + 1x Sandwich) que cualquier usuario
// puede crear/editar desde la pantalla de Tomar Pedido, para agregar varios productos al carrito
// de un solo toque. No tiene precio propio: PrecioTotal se calcula siempre como la suma de sus
// productos al precio actual del catálogo, para que nunca quede desactualizado si esos precios
// cambian. No tiene Marca propia: sus productos pueden ser de Sinners, Relatos o de ambas (las
// dos marcas comparten mesas/personal), y se muestra igual en la pestaña "Combos" sin importar
// qué marca esté seleccionada en el resto de la pantalla.
public class Preset
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public ICollection<PresetProducto> Productos { get; set; } = [];

    [NotMapped]
    public decimal PrecioTotal => Productos.Sum(pp => pp.Producto.Precio * pp.Cantidad);
}
