namespace SinnersRelatos.Web.Components;

// Formatea cuánto tiempo lleva una mesa ocupada, para MapaMesas y TomarPedido. Compartido en un
// solo lugar para no repetir la misma cuenta en ambas páginas.
public static class FormateoTiempo
{
    public static string TiempoTranscurrido(DateTime desde)
    {
        var transcurrido = DateTime.Now - desde;
        if (transcurrido < TimeSpan.Zero)
            transcurrido = TimeSpan.Zero;

        return transcurrido.TotalHours >= 1
            ? $"{(int)transcurrido.TotalHours}h {transcurrido.Minutes:00}min"
            : $"{(int)transcurrido.TotalMinutes} min";
    }
}
