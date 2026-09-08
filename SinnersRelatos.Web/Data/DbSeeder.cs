using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Models;
using SinnersRelatos.Web.Services.Interfaces;

namespace SinnersRelatos.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        if (!await context.Usuarios.AnyAsync())
            await SeedAdminAsync(context, passwordHasher);

        if (!await context.Usuarios.AnyAsync(u => u.NombreUsuario == "mesero.pruebas"))
            await SeedMeseroPruebasAsync(context, passwordHasher);

        if (!await context.Categorias.AnyAsync())
            await SeedCatalogoAsync(context);

        if (!await context.Mesas.AnyAsync())
            await SeedMesasAsync(context);

        if (!await context.Ingredientes.AnyAsync())
            await SeedIngredientesYRecetasSinnersAsync(context);

        if (!await context.ConfiguracionSistema.AnyAsync())
            context.ConfiguracionSistema.Add(new ConfiguracionSistema());

        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        var empleado = new Empleado { Nombres = "Admin", Apellidos = "Sinners & Relatos" };
        var usuario = new Usuario
        {
            NombreUsuario = "admin",
            PasswordHash = passwordHasher.Hash("admin123"),
            Rol = RolUsuario.Administrador,
            Empleado = empleado
        };

        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
    }

    // Usuario ficticio e inactivo (no puede iniciar sesión) al que se le atribuyen los
    // pedidos generados desde Herramientas de Desarrollo, para poder identificarlos y
    // borrarlos sin afectar el historial de meseros reales.
    private static async Task SeedMeseroPruebasAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        var empleado = new Empleado { Nombres = "Mesero", Apellidos = "De Pruebas (simulado)" };
        var usuario = new Usuario
        {
            NombreUsuario = "mesero.pruebas",
            PasswordHash = passwordHasher.Hash(Guid.NewGuid().ToString("N")),
            Rol = RolUsuario.Mesero,
            Activo = false,
            Empleado = empleado
        };

        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCatalogoAsync(AppDbContext context)
    {
        var categorias = new Dictionary<string, Categoria>();
        var productos = new Dictionary<string, Producto>();

        Categoria Cat(string nombre)
        {
            if (!categorias.TryGetValue(nombre, out var categoria))
            {
                categoria = new Categoria { Nombre = nombre };
                categorias[nombre] = categoria;
            }
            return categoria;
        }

        void Add(Categoria categoria, Marca marca, DestinoPreparacion destino, params (string Clave, string Nombre, decimal Precio, string? Descripcion)[] items)
        {
            foreach (var item in items)
            {
                productos[item.Clave] = new Producto
                {
                    Nombre = item.Nombre,
                    Precio = item.Precio,
                    Descripcion = item.Descripcion,
                    Marca = marca,
                    DestinoPreparacion = destino,
                    Categoria = categoria
                };
            }
        }

        // ================= SINNERS =================

        Add(Cat("Café Clásico"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.cafe.espresso", "Espresso", 6.00m, "30 ml de puro café"),
            ("sin.cafe.doppio", "Doppio", 8.00m, "Espresso doble"),
            ("sin.cafe.americano", "Americano", 7.00m, "Espresso + agua"),
            ("sin.cafe.bombon", "Bombón", 9.00m, "Espresso + leche condensada + espuma de leche"),
            ("sin.cafe.capuccino", "Capuccino", 9.00m, "Espresso + leche texturizada"),
            ("sin.cafe.capuccino_caramelo", "Capuccino de Caramelo", 10.00m, "Espresso + leche texturizada + caramelo"),
            ("sin.cafe.capuccino_menta", "Capuccino de Menta", 10.00m, "Espresso + leche texturizada + crema de menta"),
            ("sin.cafe.moccacino", "Moccacino", 12.00m, "Espresso + leche + chocolate"),
            ("sin.cafe.affogato", "Affogato", 12.00m, "Espresso + helado"),
            ("sin.cafe.coldbrew", "Cold Brew", 9.00m, "Extracción de café en frío"));

        Add(Cat("Bebidas a Base de Café"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.frio.iced_capuccino", "Iced Capuccino", 10.00m, null),
            ("sin.frio.iced_mocaccino", "Iced Mocaccino", 12.00m, null),
            ("sin.frio.orange_coffee", "Orange Coffee", 8.00m, null),
            ("sin.frio.lemon_coffee", "Lemon Coffee", 7.00m, null),
            ("sin.frio.coffee_tonic", "Coffee Tonic", 10.00m, "Espresso + tónica"),
            ("sin.frio.espresso_ginger", "Espresso Ginger", 8.00m, "Espresso + ginger ale + hielo"));

        Add(Cat("Bebidas Calientes"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.caliente.chocolate_leche", "Chocolate con Leche", 10.00m, null),
            ("sin.caliente.te_cedron", "Té de Cedrón", 6.00m, null),
            ("sin.caliente.te_aromatico", "Té Aromático", 6.00m, "Canela + clavo + miel"),
            ("sin.caliente.te_tropical", "Té Tropical", 6.00m, "Jamaica + naranja + miel"),
            ("sin.caliente.te_piteado", "Té Piteado", 12.00m, "Té a elegir + pisco"));

        Add(Cat("Tés en Jarra"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.jarra.te_cedron", "Té de Cedrón (Jarra)", 16.00m, null),
            ("sin.jarra.te_aromatico", "Té Aromático (Jarra)", 16.00m, null),
            ("sin.jarra.te_tropical", "Té Tropical (Jarra)", 16.00m, null),
            ("sin.jarra.te_piteado", "Té Piteado (Jarra)", 30.00m, null));

        Add(Cat("Acompañamientos"), Marca.Sinners, DestinoPreparacion.Cocina,
            ("sin.acomp.choriperucho", "Choriperucho", 12.00m, "Chorizo artesanal colorado"),
            ("sin.acomp.choriargento", "Choriargento", 15.00m, "Chorizo artesanal parrillero"),
            ("sin.acomp.hamburguesa", "Hamburguesa", 15.00m, "Blend de cortes nacionales e importados"),
            ("sin.acomp.sandwich_panceta", "Sandwich Panceta", 15.00m, "Panceta ahumada + jamón + piña"),
            ("sin.acomp.salchipapa_panceta", "Salchipapa de Panceta Ahumada", 15.00m, "Panceta ahumada + mix de papas"),
            ("sin.acomp.salchipapa_chorizo", "Salchipapa de Chorizo", 17.00m, "Chorizo artesanal + mix de papas"),
            ("sin.acomp.salchipapa_entrana", "Salchipapa de Entraña", 20.00m, "Entraña a la parrilla + mix de papas"));

        Add(Cat("Pizzas y Empanadas"), Marca.Sinners, DestinoPreparacion.Cocina,
            ("sin.pizza.americana", "Pizza Americana", 12.00m, null),
            ("sin.pizza.chorizo", "Pizza de Chorizo", 12.00m, null),
            ("sin.pizza.hawaiana", "Pizza Hawaiana", 14.00m, null),
            ("sin.pizza.cabanossi", "Pizza de Cabanossi", 14.00m, null),
            ("sin.pizza.cabanossi_pina", "Pizza de Cabanossi con Piña", 16.00m, null),
            ("sin.pizza.parrillera", "Pizza Parrillera", 18.00m, null),
            ("sin.empanada.carne", "Empanada de Carne", 5.00m, null),
            ("sin.empanada.jamon_queso", "Empanada de Jamón y Queso", 5.00m, null));

        Add(Cat("Frappés"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.frappe.clasico", "Frappé Clásico", 12.00m, null),
            ("sin.frappe.caramelo", "Frappé Caramelo", 13.00m, null),
            ("sin.frappe.chocolate", "Frappé Chocolate", 13.00m, null),
            ("sin.frappe.oreo", "Frappé Oreo", 14.00m, null));

        Add(Cat("Jugos"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.jugo.papaya", "Jugo de Papaya", 8.00m, null),
            ("sin.jugo.pina", "Jugo de Piña", 8.00m, null),
            ("sin.jugo.maracuya", "Jugo de Maracuyá", 7.00m, null),
            ("sin.jugo.surtido", "Jugo Surtido", 9.00m, null));

        Add(Cat("Batidos"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.batido.platano", "Batido de Plátano", 10.00m, "Fruta + leche + helado"),
            ("sin.batido.fresa", "Batido de Fresa", 12.00m, "Fruta + leche + helado"),
            ("sin.batido.arandano", "Batido de Arándano", 12.00m, "Fruta + leche + helado"),
            ("sin.batido.especial", "Batido Especial", 14.00m, "Frutas a elección + leche + helado + avena"));

        Add(Cat("Limonadas"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.limonada.clasica", "Limonada Clásica", 7.00m, null),
            ("sin.limonada.frutos_rojos", "Limonada de Frutos Rojos", 8.00m, null),
            ("sin.limonada.durazno", "Limonada de Durazno", 9.00m, null));

        Add(Cat("Otras Bebidas"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.otras.agua", "Agua Personal", 3.00m, null),
            ("sin.otras.gaseosa", "Gaseosa Personal", 5.00m, null),
            ("sin.otras.redbull", "Red Bull", 14.00m, null));

        Add(Cat("Cócteles"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.coctel.kingston_negroni", "Kingston Negroni", 30.00m, "Ron Jamaiquino + Campari + Vermouth"),
            ("sin.coctel.sinners_margarita", "Sinners Margarita", 35.00m, "Tequila + triple sec + syrup de canela"),
            ("sin.coctel.casino_menta", "Casino de Menta", 25.00m, "Crema de menta + triple sec + crema de coco + leche"),
            ("sin.coctel.old_fashioned", "Old Fashioned", 25.00m, "Whisky + amargo + azúcar"),
            ("sin.coctel.margarita", "Margarita", 30.00m, "Tequila + triple sec"),
            ("sin.coctel.negroni", "Negroni", 30.00m, "Campari + Gin + vermouth"),
            ("sin.coctel.charro_negro", "Charro Negro", 25.00m, "Tequila + limón + coca"),
            ("sin.coctel.godfather", "Godfather", 25.00m, "Whisky + amaretto"),
            ("sin.coctel.pisco_sour", "Pisco Sour", 30.00m, "Pisco + zumo de limón + clara"),
            ("sin.coctel.gin_tonic", "Gin Tonic", 25.00m, "Ginebra + agua tónica"),
            ("sin.coctel.orgasmo", "Orgasmo", 30.00m, "Kalúa + baileys + amaretto"),
            ("sin.coctel.whisky_sour", "Whisky Sour", 35.00m, "Whisky + sirope + zumo de limón + clara"),
            ("sin.coctel.pina_colada", "Piña Colada", 25.00m, "Ron + crema de coco + piña"),
            ("sin.coctel.algarrobina", "Algarrobina", 25.00m, "Pisco + crema de leche + algarrobina"),
            ("sin.coctel.capitan", "Capitán", 25.00m, "Pisco + vermouth + amargo"),
            ("sin.coctel.fernandito", "Fernandito", 25.00m, "Fernet + coca cola"),
            ("sin.coctel.chilcano_afrutado", "Chilcano Afrutado", 20.00m, null),
            ("sin.coctel.chilcano_clasico", "Chilcano Clásico", 16.00m, null),
            ("sin.coctel.mojito_afrutado", "Mojito Afrutado", 20.00m, null),
            ("sin.coctel.mojito_clasico", "Mojito Clásico", 16.00m, null),
            ("sin.coctel.cuba", "Cuba", 15.00m, "Ron + coca cola"));

        Add(Cat("Cócteles con Café"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.coctelcafe.irlandes", "Café Irlandés", 35.00m, "Whisky + Espresso + leche cremada"),
            ("sin.coctelcafe.espresso_martini", "Espresso Martini", 35.00m, "Vodka + licor de café + Syrup"),
            ("sin.coctelcafe.shakerato_baileys", "Shakerato Baileys", 35.00m, "Kalúa + baileys + amaretto"),
            ("sin.coctelcafe.cafe_sour", "Café Sour", 35.00m, "Espresso + sirope + zumo de limón + clara"),
            ("sin.coctelcafe.cafe_negroni", "Café Negroni", 30.00m, "Campari + Gin vermouth + Espresso corto"),
            ("sin.coctelcafe.old_coffee_fashion", "Old Coffee Fashion", 25.00m, "Ron + amargo + almíbar de café"),
            ("sin.coctelcafe.cafe_mexicano", "Café Mexicano", 35.00m, "Tequila + licor de café + americano + crema"),
            ("sin.coctelcafe.gin_coffee_tonic", "Gin Coffee Tonic", 35.00m, "Gin + espresso + tónica"),
            ("sin.coctelcafe.ginger_ale_coffee", "Ginger Ale Coffee", 30.00m, "Vodka + espresso syrup + ginger ale"),
            ("sin.coctelcafe.ruso_negro", "Ruso Negro", 20.00m, "Vodka + licor de café"),
            ("sin.coctelcafe.ruso_blanco", "Ruso Blanco", 25.00m, "Vodka + licor de café + crema de leche"),
            ("sin.coctelcafe.toro_ruso", "Toro Ruso", 30.00m, "Vodka + Espresso + ginger ale"));

        Add(Cat("Cervezas Personales"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.cerveza.pilsen", "Pilsen", 10.00m, null),
            ("sin.cerveza.cusquena_dorada", "Cusqueña Dorada", 10.00m, null),
            ("sin.cerveza.cusquena_trigo", "Cusqueña Trigo", 10.00m, null),
            ("sin.cerveza.corona", "Corona", 13.00m, null),
            ("sin.cerveza.stella", "Stella Artois", 13.00m, null),
            ("sin.cerveza.budweiser", "Budweiser", 13.00m, null),
            ("sin.cerveza.heineken", "Heineken", 13.00m, null));

        Add(Cat("Chopp"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.chopp.clasico", "Chopp Clásico", 12.00m, "500 ml de Cerveza Pilsen heladita"),
            ("sin.chopp.sinners", "Chopp Sinners", 15.00m, "630 ml de Cerveza Heineken heladita"));

        Add(Cat("Santo Pecado (Cerveza Artesanal Sinners)"), Marca.Sinners, DestinoPreparacion.Barra,
            ("sin.santopecado.doble_ipa", "Doble IPA", 15.00m, "ABV 9% / IBU 75"),
            ("sin.santopecado.red_ale", "Red Ale", 15.00m, "ABV 6% / IBU 25"),
            ("sin.santopecado.american_stout", "American Stout", 15.00m, "ABV 8% / IBU 50"),
            ("sin.santopecado.barley_wine", "Barley Wine", 15.00m, "ABV 13% / IBU 40"),
            ("sin.santopecado.hoja_coca", "Hoja de Coca", 15.00m, "ABV 5.5% / IBU 15"),
            ("sin.santopecado.maiz_morado", "Maíz Morado", 15.00m, "ABV 6.5% / IBU 20"));

        // ================= RELATOS =================

        Add(Cat("Entradas"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.entrada.empanadas_1", "Empanadas Clásica (1 unidad)", 8.00m, "Ozobuco cocinado lentamente por tres horas, receta original de Relatos"),
            ("rel.entrada.empanadas_2", "Empanadas Clásica (2 unidades)", 15.00m, "Ozobuco cocinado lentamente por tres horas, receta original de Relatos"),
            ("rel.entrada.chorizo", "Chorizo Artesanal (2 unidades)", 30.00m, "Escoge entre tradicional, finas hierbas o mermelada de ají limo"),
            ("rel.entrada.champinones_parrilla", "Champiñones a la Parrilla", 22.00m, null),
            ("rel.entrada.champinones_ajillo", "Champiñones al Ajillo", 22.00m, "Champiñones salteados en mantequilla con ajo, crema de leche y vino blanco acompañado de tostadas."));

        Add(Cat("Cortes de Res - Argentina"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.res.arg.picana", "Tapa de Cuadril / Picaña", 70.00m, "Corte jugoso y con buena grasa."),
            ("rel.res.arg.bife_angosto", "Bife de Chorizo / Bife Angosto", 55.00m, "Textura firme y gran sabor."),
            ("rel.res.arg.bife_ancho", "Baby Beef / Bife Ancho", 65.00m, "Mayor marmoleado y suavidad."),
            ("rel.res.arg.colita_cuadril", "Colita de Cuadril", 55.00m, "Corte tierno y magro."),
            ("rel.res.arg.entrana", "Entraña", 130.00m, "Corte delgado y de sabor intenso."));

        Add(Cat("Cortes de Res - Brasil"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.res.bra.picana", "Tapa de Cuadril / Picaña", 55.00m, "Corte jugoso y con buena grasa."),
            ("rel.res.bra.bife_angosto", "Bife de Chorizo / Bife Angosto", 48.00m, "Textura firme y gran sabor."),
            ("rel.res.bra.bife_ancho", "Baby Beef / Bife Ancho", 55.00m, "Mayor marmoleado y suavidad."),
            ("rel.res.bra.colita_cuadril", "Colita de Cuadril", 50.00m, "Corte tierno y magro."));

        Add(Cat("Cortes de Res - Perú"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.res.per.picana", "Tapa de Cuadril / Picaña", 50.00m, "Corte jugoso y con buena grasa."),
            ("rel.res.per.bife_nacional", "Bife Nacional", 45.00m, "Textura firme y gran sabor."),
            ("rel.res.per.entrana_nacional", "Entraña Nacional", 35.00m, "Corte delgado y de sabor intenso."),
            ("rel.res.per.asado_tira", "Asado de Tira", 45.00m, "Corte tierno y magro."),
            ("rel.res.per.churrasco", "Churrasco", 35.00m, "Corte fino con marmoleado y sabor intenso."));

        Add(Cat("Cortes de Res - USA"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.res.usa.picana", "Tapa de Cuadril / Picaña", 75.00m, "Corte jugoso y con buena grasa."),
            ("rel.res.usa.bife_angosto", "Bife de Chorizo / Bife Angosto", 100.00m, "Textura firme y gran sabor."),
            ("rel.res.usa.bife_ancho", "Baby Beef / Bife Ancho", 115.00m, "Mayor marmoleado y suavidad."),
            ("rel.res.usa.colita_cuadril", "Colita de Cuadril", 70.00m, "Corte tierno y magro."),
            ("rel.res.usa.entrana", "Entraña", 160.00m, "Corte delgado y de sabor intenso."));

        Add(Cat("Otras Preparaciones"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.otras.lomo_champinones", "Lomo en Salsa de Champiñones", 50.00m, "Acompañado de papa coctel salteadas en chimichurri y choclo."),
            ("rel.otras.brochetas_lomo", "Brochetas de Lomo", 40.00m, "Tiernos trozos de lomo y vegetales de estación, a la parrilla."),
            ("rel.otras.tartare_lomo", "Tartare de Lomo", 40.00m, "Finos cortes de lomo, salsa holandesa, queso parmesano y rebanadas de pan."));

        Add(Cat("Cortes de Cerdo"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.cerdo.chuleta", "Chuleta de Cerdo", 35.00m, null),
            ("rel.cerdo.panceta", "Panceta Crocante", 35.00m, null),
            ("rel.cerdo.costillas", "Costillas de Cerdo", 35.00m, null),
            ("rel.cerdo.costillas_bbq", "Costillas en Salsa BBQ", 40.00m, null));

        Add(Cat("Cortes de Ave"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.ave.pechuga", "Pechuga a la Parrilla", 30.00m, "Acompañado de guarnición a elección."),
            ("rel.ave.milanesa_pollo", "Milanesa de Pollo", 35.00m, "Acompañado de guarnición a elección."),
            ("rel.ave.brochetas_pollo", "Brochetas de Pollo", 30.00m, "Tiernos trozos de pechuga y vegetales de estación, a la parrilla."),
            ("rel.ave.milanesa_napolitana", "Milanesa Napolitana", 45.00m, "Clásico corte de milanesa, gratinado con salsa de tomate casera y abundante queso mozarella gratinado."));

        Add(Cat("Marinos"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.marino.pulpo_parrilla", "Pulpo a la Parrilla", 42.00m, "Acompañado de papa coctel salteadas en chimichurri y choclo."),
            ("rel.marino.pulpo_anticuchero", "Pulpo Anticuchero", 45.00m, "Macerado en salsa de anticucho acompañado de papa coctel salteada en chimichurri y choclo."),
            ("rel.marino.langostinos_ajillo", "Langostinos al Ajillo", 26.00m, "Langostinos salteados en mantequilla con ajo, crema de leche, vino blanco y perejil, acompañado de tostadas."),
            ("rel.marino.langostinos_centenario", "Langostinos Centenario", 30.00m, "Langostinos empanizados, crocantes con salsa de maracuyá."));

        Add(Cat("Guarniciones"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.guarnicion.papas_fritas", "Papas Fritas", 8.00m, null),
            ("rel.guarnicion.papas_coctel", "Papas Cóctel Salteadas", 8.00m, null),
            ("rel.guarnicion.ensalada_fresca", "Ensalada Fresca", 8.00m, null),
            ("rel.guarnicion.verduras_grilladas", "Verduras Grilladas", 8.00m, null),
            ("rel.guarnicion.arroz_choclo", "Arroz con Choclo", 8.00m, null));

        Add(Cat("Para Compartir"), Marca.Relatos, DestinoPreparacion.Cocina,
            ("rel.compartir.parrilla_mixta", "Parrilla Mixta", 180.00m, "Bife Angosto o Bife Ancho Brasilero, Asado de Tira, Pechuga, Brochetas de Lomo, 1 Porción de Chorizo, 2 Guarnición (Ensalada Fresca y Papa salteada)."),
            ("rel.compartir.parrilla_super_clasico", "Parrilla Súper Clásico", 240.00m, "Picaña Argentina o Brasileña, Bife Ancho Brasileño, Bife Angosto Argentino, 1 Porción de Chorizo, Brochetas de Lomo, 2 Guarnición (Ensalada Fresca y Papa frita o salteada)."),
            ("rel.compartir.parrilla_usa", "Parrilla USA", 300.00m, "Picaña Angus, Bife Angosto o Bife Ancho, Colita de Cuadril, Pechuga, 1 Porción de Chorizo, 2 Guarnición (Ensalada Fresca y Papa frita o salteada)."),
            ("rel.compartir.parrilla_relatos", "Parrilla Relatos", 270.00m, "Picaña Angus USA o Picaña Argentina, Bife Ancho Brasileño, Bife Angosto Argentino, Churrasco, Pechuga, 1 Porción de Chorizo, 2 Guarnición (Ensalada Fresca y Papa frita o salteada)."));

        Add(Cat("Bebidas sin Alcohol"), Marca.Relatos, DestinoPreparacion.Barra,
            ("rel.sinalcohol.agua_san_luis", "Agua San Luis 330ml", 4.00m, null),
            ("rel.sinalcohol.inkacola", "InkaCola 330ml", 4.00m, null),
            ("rel.sinalcohol.cocacola", "CocaCola 330ml", 5.00m, null),
            ("rel.sinalcohol.maracuya_clasica_vaso", "Maracuyá Clásica (Vaso)", 5.00m, null),
            ("rel.sinalcohol.maracuya_clasica_jarra", "Maracuyá Clásica (Jarra 1L)", 16.00m, null),
            ("rel.sinalcohol.maracuya_hierba_luisa_vaso", "Maracuyá con Hierba Luisa (Vaso)", 6.00m, null),
            ("rel.sinalcohol.maracuya_hierba_luisa_jarra", "Maracuyá con Hierba Luisa (Jarra 1L)", 18.00m, null),
            ("rel.sinalcohol.limonada_clasica_vaso", "Limonada Clásica (Vaso)", 7.00m, null),
            ("rel.sinalcohol.limonada_clasica_jarra", "Limonada Clásica (Jarra 1L)", 16.00m, null),
            ("rel.sinalcohol.limonada_hierba_luisa_vaso", "Limonada con Hierba Luisa (Vaso)", 7.00m, null),
            ("rel.sinalcohol.limonada_hierba_luisa_jarra", "Limonada con Hierba Luisa (Jarra 1L)", 18.00m, null),
            ("rel.sinalcohol.limonada_frutos_rojos_vaso", "Limonada con Frutos Rojos (Vaso)", 8.00m, null),
            ("rel.sinalcohol.limonada_frutos_rojos_jarra", "Limonada con Frutos Rojos (Jarra 1L)", 20.00m, null));

        Add(Cat("Bebidas con Alcohol"), Marca.Relatos, DestinoPreparacion.Barra,
            ("rel.conalcohol.pisco_sour", "Pisco Sour", 35.00m, "Pisco + sirope simple + zumo de limón + clara de huevo"),
            ("rel.conalcohol.tinto_verano_copa", "Tinto de Verano (Copa)", 25.00m, "Vino tinto + evervess + naranja"),
            ("rel.conalcohol.tinto_verano_jarra", "Tinto de Verano (Jarra 1L)", 55.00m, "Vino tinto + evervess + naranja"),
            ("rel.conalcohol.tinto_afrodisiaco_copa", "Tinto Afrodisíaco (Copa)", 30.00m, "Vino blanco + evervess + frutos"),
            ("rel.conalcohol.tinto_afrodisiaco_jarra", "Tinto Afrodisíaco (Jarra 1L)", 65.00m, "Vino blanco + evervess + frutos"),
            ("rel.conalcohol.kingston_negroni", "Kingston Negroni", 35.00m, "Variante de negroni con ron jamaiquino + campari + vermouth"),
            ("rel.conalcohol.whisky_sour", "Whisky Sour", 40.00m, "Whisky bourbon + sirope simple + zumo de limón + clara de huevo"),
            ("rel.conalcohol.fernandito", "Fernandito", 30.00m, "Fernet + coca cola"));

        context.Categorias.AddRange(categorias.Values);
        context.Productos.AddRange(productos.Values);
        await context.SaveChangesAsync();

        // ================= MODIFICADORES =================

        var grupoLeche = new GrupoModificador { Nombre = "Tipo de Leche", EsObligatorio = true, PermiteMultiple = false };
        var opcionesLeche = new[]
        {
            new OpcionModificador { Nombre = "Leche Entera", PrecioAdicional = 0m, GrupoModificador = grupoLeche },
            new OpcionModificador { Nombre = "Leche Deslactosada", PrecioAdicional = 0m, GrupoModificador = grupoLeche }
        };

        var grupoChorizo = new GrupoModificador { Nombre = "Tipo de Chorizo", EsObligatorio = true, PermiteMultiple = false };
        var opcionesChorizo = new[]
        {
            new OpcionModificador { Nombre = "Tradicional", PrecioAdicional = 0m, GrupoModificador = grupoChorizo },
            new OpcionModificador { Nombre = "Finas Hierbas", PrecioAdicional = 0m, GrupoModificador = grupoChorizo },
            new OpcionModificador { Nombre = "Mermelada de Ají Limo", PrecioAdicional = 0m, GrupoModificador = grupoChorizo }
        };

        var grupoGuarnicion = new GrupoModificador { Nombre = "Guarnición", EsObligatorio = true, PermiteMultiple = false };
        var opcionesGuarnicion = new[]
        {
            new OpcionModificador { Nombre = "Papas Fritas", PrecioAdicional = 0m, GrupoModificador = grupoGuarnicion },
            new OpcionModificador { Nombre = "Papas Cóctel Salteadas", PrecioAdicional = 0m, GrupoModificador = grupoGuarnicion },
            new OpcionModificador { Nombre = "Ensalada Fresca", PrecioAdicional = 0m, GrupoModificador = grupoGuarnicion },
            new OpcionModificador { Nombre = "Verduras Grilladas", PrecioAdicional = 0m, GrupoModificador = grupoGuarnicion },
            new OpcionModificador { Nombre = "Arroz con Choclo", PrecioAdicional = 0m, GrupoModificador = grupoGuarnicion }
        };

        var grupoExtraGuarnicion = new GrupoModificador { Nombre = "Extra Guarnición", EsObligatorio = false, PermiteMultiple = true };
        var opcionesExtraGuarnicion = new[]
        {
            new OpcionModificador { Nombre = "Puré de Camote", PrecioAdicional = 10.00m, GrupoModificador = grupoExtraGuarnicion },
            new OpcionModificador { Nombre = "Puré de Papa", PrecioAdicional = 10.00m, GrupoModificador = grupoExtraGuarnicion },
            new OpcionModificador { Nombre = "Porción de Papa Salteada con Choclo", PrecioAdicional = 12.00m, GrupoModificador = grupoExtraGuarnicion },
            new OpcionModificador { Nombre = "Papa al Plomo", PrecioAdicional = 8.00m, GrupoModificador = grupoExtraGuarnicion }
        };

        context.GruposModificadores.AddRange(grupoLeche, grupoChorizo, grupoGuarnicion, grupoExtraGuarnicion);
        context.OpcionesModificadores.AddRange(opcionesLeche);
        context.OpcionesModificadores.AddRange(opcionesChorizo);
        context.OpcionesModificadores.AddRange(opcionesGuarnicion);
        context.OpcionesModificadores.AddRange(opcionesExtraGuarnicion);
        await context.SaveChangesAsync();

        // ================= VÍNCULOS PRODUCTO <-> GRUPO MODIFICADOR =================

        var vinculos = new List<ProductoGrupoModificador>();

        void Vincular(GrupoModificador grupo, params string[] claves)
        {
            foreach (var clave in claves)
                vinculos.Add(new ProductoGrupoModificador { Producto = productos[clave], GrupoModificador = grupo });
        }

        // Tipo de Leche: Café Clásico + Frappés + Batidos
        // Espresso, Doppio, Americano, Affogato y Cold Brew quedan fuera: no llevan leche.
        Vincular(grupoLeche,
            "sin.cafe.bombon",
            "sin.cafe.capuccino", "sin.cafe.capuccino_caramelo", "sin.cafe.capuccino_menta",
            "sin.cafe.moccacino",
            "sin.frappe.clasico", "sin.frappe.caramelo", "sin.frappe.chocolate", "sin.frappe.oreo",
            "sin.batido.platano", "sin.batido.fresa", "sin.batido.arandano", "sin.batido.especial");

        // Tipo de Chorizo: Chorizo Artesanal
        Vincular(grupoChorizo, "rel.entrada.chorizo");

        // Guarnición + Extra Guarnición: Cortes de Cerdo + Pechuga/Milanesa de Pollo
        var productosConGuarnicion = new[]
        {
            "rel.cerdo.chuleta", "rel.cerdo.panceta", "rel.cerdo.costillas", "rel.cerdo.costillas_bbq",
            "rel.ave.pechuga", "rel.ave.milanesa_pollo"
        };
        Vincular(grupoGuarnicion, productosConGuarnicion);
        Vincular(grupoExtraGuarnicion, productosConGuarnicion);

        context.ProductosGruposModificadores.AddRange(vinculos);
        await context.SaveChangesAsync();
    }

    private static async Task SeedMesasAsync(AppDbContext context)
    {
        var mesas = new List<Mesa>();

        for (var numero = 1; numero <= 7; numero++)
            mesas.Add(new Mesa { Marca = Marca.Sinners, Tipo = TipoMesa.Mesa, Numero = numero });

        for (var numero = 1; numero <= 6; numero++)
            mesas.Add(new Mesa { Marca = Marca.Sinners, Tipo = TipoMesa.Barra, Numero = numero });

        for (var numero = 1; numero <= 7; numero++)
            mesas.Add(new Mesa { Marca = Marca.Relatos, Tipo = TipoMesa.Mesa, Numero = numero });

        context.Mesas.AddRange(mesas);
        await context.SaveChangesAsync();
    }

    // Recetas tomadas de la planilla de costeo de Sinners. Solo cubre productos de Sinners
    // (la planilla no incluye Relatos). Stock inicial en 0: hay que cargar el stock real
    // desde la pantalla de Ingredientes antes de operar.
    private static async Task SeedIngredientesYRecetasSinnersAsync(AppDbContext context)
    {
        var ingredientesData = new (string Nombre, string Unidad)[]
        {
            ("Aceite", "ml"),
            ("Agua Personal con Gas", "ml"),
            ("Agua Tónica", "ml"),
            ("Agua", "ml"),
            ("Algarrobina (Licor)", "ml"),
            ("Amaretto", "ml"),
            ("Angostura (Amargo)", "ml"),
            ("Arándano", "g"),
            ("Azúcar Blanca", "g"),
            ("Azúcar", "g"),
            ("Baileys", "ml"),
            ("Barquillos", "unidad"),
            ("Cabanossi", "g"),
            ("Café en Grano", "g"),
            ("Campari", "ml"),
            ("Canela", "g"),
            ("Carne de Hamburguesa", "g"),
            ("Cedrón", "g"),
            ("Chimichurri", "g"),
            ("Chocolate de Taza", "g"),
            ("Chorizo Artesanal (Argentino)", "g"),
            ("Chorizo Artesanal (Español)", "g"),
            ("Chorizo Industrial", "g"),
            ("Crema de Cacao Blanca", "ml"),
            ("Crema de Coco", "ml"),
            ("Crema de Leche", "ml"),
            ("Durazno", "g"),
            ("Empanada de Carne (Prefabricada)", "unidad"),
            ("Empanada de Jamón y Queso (Prefabricada)", "unidad"),
            ("Fernet", "ml"),
            ("Fresa", "g"),
            ("Frutos Rojos", "g"),
            ("Galleta tipo Oreo", "unidad"),
            ("Gaseosa", "ml"),
            ("Gin", "ml"),
            ("Ginger Ale", "ml"),
            ("Helado de Vainilla", "ml"),
            ("Hielo", "g"),
            ("Huevos", "unidad"),
            ("Jamaica (Flor/Infusión)", "g"),
            ("Jamón", "g"),
            ("Jarabe de Goma", "ml"),
            ("Jarabe de Vainilla", "ml"),
            ("Jugo de Limón", "ml"),
            ("Kahlúa (Licor de Café)", "ml"),
            ("Leche Condensada", "g"),
            ("Leche Deslactosada", "ml"),
            ("Leche Entera", "ml"),
            ("Leche en Polvo", "g"),
            ("Lechuga", "g"),
            ("Licor de Menta", "ml"),
            ("Limón", "unidad"),
            ("Maracuyá", "g"),
            ("Masa de Pizza", "unidad"),
            ("Miel", "g"),
            ("Mix de Frutas", "g"),
            ("Naranja", "unidad"),
            ("Pan Ciabatta", "unidad"),
            ("Pan de Hamburguesa", "unidad"),
            ("Panceta", "g"),
            ("Papa Fresca (Fritura)", "g"),
            ("Papas", "g"),
            ("Papaya", "g"),
            ("Pimiento", "g"),
            ("Pisco", "ml"),
            ("Piña (Fresca)", "g"),
            ("Piña (Lata)", "g"),
            ("Plátano", "g"),
            ("Queso Mozzarella", "g"),
            ("Red Bull", "ml"),
            ("Ron Blanco", "ml"),
            ("Ron Jamaiquino", "ml"),
            ("Salsa de Tomate", "g"),
            ("Syrup de Caramelo", "g"),
            ("Syrup de Chocolate", "ml"),
            ("Tequila", "ml"),
            ("Tomate", "g"),
            ("Triple Sec", "ml"),
            ("Té a Elegir (Base)", "unidad"),
            ("Vermut Rojo", "ml"),
            ("Vodka", "ml"),
            ("Whisky Bourbon", "ml"),
            ("Whisky", "ml"),
        };

        var ingredientes = ingredientesData
            .Select(i => new Ingrediente { Nombre = i.Nombre, UnidadMedida = i.Unidad })
            .ToList();
        context.Ingredientes.AddRange(ingredientes);
        await context.SaveChangesAsync();
        var porNombre = ingredientes.ToDictionary(i => i.Nombre);

        var productos = await context.Productos
            .Where(p => p.Marca == Marca.Sinners)
            .ToDictionaryAsync(p => p.Nombre);

        var recetaProductoData = new (string Producto, string Ingrediente, decimal Cantidad)[]
        {
            ("Espresso", "Café en Grano", 18m),
            ("Doppio", "Café en Grano", 36m),
            ("Americano", "Café en Grano", 18m),
            ("Americano", "Agua", 260m),
            ("Bombón", "Café en Grano", 18m),
            ("Bombón", "Leche Condensada", 30m),
            ("Capuccino", "Café en Grano", 18m),
            ("Capuccino de Caramelo", "Café en Grano", 18m),
            ("Capuccino de Caramelo", "Syrup de Caramelo", 24m),
            ("Capuccino de Menta", "Café en Grano", 18m),
            ("Capuccino de Menta", "Licor de Menta", 30m),
            ("Moccacino", "Café en Grano", 18m),
            ("Moccacino", "Syrup de Chocolate", 30m),
            ("Affogato", "Helado de Vainilla", 50m),
            ("Affogato", "Café en Grano", 18m),
            ("Cold Brew", "Café en Grano", 20m),
            ("Cold Brew", "Agua", 200m),
            ("Iced Capuccino", "Café en Grano", 18m),
            ("Iced Capuccino", "Leche Entera", 180m),
            ("Iced Capuccino", "Jarabe de Vainilla", 30m),
            ("Iced Capuccino", "Hielo", 70m),
            ("Iced Mocaccino", "Café en Grano", 18m),
            ("Iced Mocaccino", "Leche Entera", 180m),
            ("Iced Mocaccino", "Hielo", 70m),
            ("Iced Mocaccino", "Jarabe de Vainilla", 30m),
            ("Iced Mocaccino", "Syrup de Chocolate", 30m),
            ("Orange Coffee", "Naranja", 2m),
            ("Orange Coffee", "Jarabe de Goma", 30m),
            ("Orange Coffee", "Café en Grano", 18m),
            ("Orange Coffee", "Hielo", 70m),
            ("Orange Coffee", "Ginger Ale", 60m),
            ("Lemon Coffee", "Limón", 4m),
            ("Lemon Coffee", "Café en Grano", 18m),
            ("Lemon Coffee", "Jarabe de Goma", 60m),
            ("Lemon Coffee", "Hielo", 70m),
            ("Lemon Coffee", "Ginger Ale", 30m),
            ("Espresso Ginger", "Café en Grano", 18m),
            ("Espresso Ginger", "Ginger Ale", 120m),
            ("Espresso Ginger", "Hielo", 70m),
            ("Coffee Tonic", "Café en Grano", 18m),
            ("Coffee Tonic", "Agua Tónica", 120m),
            ("Coffee Tonic", "Hielo", 70m),
            ("Agua Personal", "Agua Personal con Gas", 630m),
            ("Gaseosa Personal", "Gaseosa", 630m),
            ("Red Bull", "Red Bull", 250m),
            ("Chocolate con Leche", "Chocolate de Taza", 40m),
            ("Chocolate con Leche", "Leche Entera", 240m),
            ("Chocolate con Leche", "Leche Condensada", 20m),
            ("Té de Cedrón", "Cedrón", 10m),
            ("Té de Cedrón", "Agua", 240m),
            ("Té de Cedrón", "Miel", 20m),
            ("Té Aromático", "Canela", 2m),
            ("Té Aromático", "Agua", 240m),
            ("Té Aromático", "Miel", 20m),
            ("Té Tropical", "Agua", 240m),
            ("Té Tropical", "Miel", 20m),
            ("Té Tropical", "Jamaica (Flor/Infusión)", 7m),
            ("Té Tropical", "Naranja", 1m),
            ("Té Piteado", "Té a Elegir (Base)", 1m),
            ("Té Piteado", "Pisco", 60m),
            ("Té de Cedrón (Jarra)", "Cedrón", 40m),
            ("Té de Cedrón (Jarra)", "Agua", 1000m),
            ("Té de Cedrón (Jarra)", "Miel", 80m),
            ("Té Aromático (Jarra)", "Canela", 8m),
            ("Té Aromático (Jarra)", "Agua", 1000m),
            ("Té Aromático (Jarra)", "Miel", 80m),
            ("Té Tropical (Jarra)", "Agua", 1000m),
            ("Té Tropical (Jarra)", "Miel", 80m),
            ("Té Tropical (Jarra)", "Jamaica (Flor/Infusión)", 30m),
            ("Té Tropical (Jarra)", "Naranja", 1m),
            ("Té Piteado (Jarra)", "Té a Elegir (Base)", 1m),
            ("Té Piteado (Jarra)", "Pisco", 60m),
            ("Pizza Americana", "Masa de Pizza", 1m),
            ("Pizza Americana", "Salsa de Tomate", 45m),
            ("Pizza Americana", "Queso Mozzarella", 60m),
            ("Pizza Americana", "Jamón", 40m),
            ("Pizza de Chorizo", "Masa de Pizza", 1m),
            ("Pizza de Chorizo", "Salsa de Tomate", 45m),
            ("Pizza de Chorizo", "Queso Mozzarella", 60m),
            ("Pizza de Chorizo", "Chorizo Industrial", 40m),
            ("Pizza de Cabanossi", "Masa de Pizza", 1m),
            ("Pizza de Cabanossi", "Salsa de Tomate", 45m),
            ("Pizza de Cabanossi", "Queso Mozzarella", 60m),
            ("Pizza de Cabanossi", "Cabanossi", 12.5m),
            ("Pizza Hawaiana", "Masa de Pizza", 1m),
            ("Pizza Hawaiana", "Salsa de Tomate", 45m),
            ("Pizza Hawaiana", "Queso Mozzarella", 60m),
            ("Pizza Hawaiana", "Jamón", 40m),
            ("Pizza Hawaiana", "Piña (Lata)", 60m),
            ("Pizza de Cabanossi con Piña", "Masa de Pizza", 1m),
            ("Pizza de Cabanossi con Piña", "Salsa de Tomate", 45m),
            ("Pizza de Cabanossi con Piña", "Queso Mozzarella", 60m),
            ("Pizza de Cabanossi con Piña", "Cabanossi", 12.5m),
            ("Pizza de Cabanossi con Piña", "Piña (Lata)", 60m),
            ("Pizza Parrillera", "Masa de Pizza", 1m),
            ("Pizza Parrillera", "Salsa de Tomate", 45m),
            ("Pizza Parrillera", "Queso Mozzarella", 60m),
            ("Pizza Parrillera", "Pimiento", 50m),
            ("Pizza Parrillera", "Chimichurri", 20m),
            ("Salchipapa de Panceta Ahumada", "Papa Fresca (Fritura)", 330m),
            ("Salchipapa de Panceta Ahumada", "Panceta", 60m),
            ("Salchipapa de Panceta Ahumada", "Aceite", 35m),
            ("Salchipapa de Chorizo", "Papa Fresca (Fritura)", 330m),
            ("Salchipapa de Chorizo", "Chorizo Artesanal (Argentino)", 180m),
            ("Salchipapa de Chorizo", "Aceite", 35m),
            ("Salchipapa de Entraña", "Papa Fresca (Fritura)", 330m),
            ("Salchipapa de Entraña", "Aceite", 35m),
            ("Empanada de Carne", "Empanada de Carne (Prefabricada)", 1m),
            ("Empanada de Jamón y Queso", "Empanada de Jamón y Queso (Prefabricada)", 1m),
            ("Choriargento", "Chorizo Artesanal (Argentino)", 180m),
            ("Choriargento", "Pan Ciabatta", 1m),
            ("Choriargento", "Lechuga", 20m),
            ("Choriargento", "Tomate", 50m),
            ("Choriargento", "Papas", 20m),
            ("Choriperucho", "Chorizo Artesanal (Español)", 160m),
            ("Choriperucho", "Pan Ciabatta", 1m),
            ("Choriperucho", "Lechuga", 20m),
            ("Choriperucho", "Tomate", 50m),
            ("Choriperucho", "Chimichurri", 10m),
            ("Choriperucho", "Papas", 20m),
            ("Hamburguesa", "Pan de Hamburguesa", 1m),
            ("Hamburguesa", "Carne de Hamburguesa", 120m),
            ("Hamburguesa", "Lechuga", 20m),
            ("Hamburguesa", "Tomate", 50m),
            ("Hamburguesa", "Papas", 20m),
            ("Sandwich Panceta", "Pan Ciabatta", 1m),
            ("Sandwich Panceta", "Panceta", 60m),
            ("Sandwich Panceta", "Jamón", 40m),
            ("Sandwich Panceta", "Piña (Lata)", 60m),
            ("Sandwich Panceta", "Lechuga", 20m),
            ("Sandwich Panceta", "Tomate", 50m),
            ("Sandwich Panceta", "Papas", 20m),
            ("Frappé Clásico", "Hielo", 150m),
            ("Frappé Clásico", "Helado de Vainilla", 60m),
            ("Frappé Clásico", "Café en Grano", 18m),
            ("Frappé Clásico", "Jarabe de Vainilla", 30m),
            ("Frappé Clásico", "Leche en Polvo", 15m),
            ("Frappé Clásico", "Barquillos", 1m),
            ("Frappé Caramelo", "Hielo", 150m),
            ("Frappé Caramelo", "Helado de Vainilla", 60m),
            ("Frappé Caramelo", "Café en Grano", 18m),
            ("Frappé Caramelo", "Jarabe de Vainilla", 30m),
            ("Frappé Caramelo", "Leche en Polvo", 15m),
            ("Frappé Caramelo", "Barquillos", 1m),
            ("Frappé Caramelo", "Syrup de Caramelo", 30m),
            ("Frappé Chocolate", "Hielo", 150m),
            ("Frappé Chocolate", "Helado de Vainilla", 60m),
            ("Frappé Chocolate", "Café en Grano", 18m),
            ("Frappé Chocolate", "Jarabe de Vainilla", 30m),
            ("Frappé Chocolate", "Leche en Polvo", 15m),
            ("Frappé Chocolate", "Barquillos", 1m),
            ("Frappé Chocolate", "Syrup de Chocolate", 30m),
            ("Frappé Oreo", "Hielo", 150m),
            ("Frappé Oreo", "Helado de Vainilla", 60m),
            ("Frappé Oreo", "Café en Grano", 18m),
            ("Frappé Oreo", "Jarabe de Vainilla", 30m),
            ("Frappé Oreo", "Leche en Polvo", 15m),
            ("Frappé Oreo", "Barquillos", 1m),
            ("Frappé Oreo", "Galleta tipo Oreo", 4m),
            ("Batido de Plátano", "Plátano", 150m),
            ("Batido de Plátano", "Jarabe de Goma", 30m),
            ("Batido de Fresa", "Fresa", 50m),
            ("Batido de Fresa", "Jarabe de Goma", 30m),
            ("Batido de Arándano", "Arándano", 50m),
            ("Batido de Arándano", "Jarabe de Goma", 30m),
            ("Batido Especial", "Mix de Frutas", 200m),
            ("Batido Especial", "Jarabe de Goma", 30m),
            ("Jugo de Papaya", "Papaya", 150m),
            ("Jugo de Papaya", "Agua", 300m),
            ("Jugo de Papaya", "Jarabe de Goma", 30m),
            ("Jugo de Piña", "Piña (Fresca)", 150m),
            ("Jugo de Piña", "Agua", 300m),
            ("Jugo de Piña", "Jarabe de Goma", 30m),
            ("Jugo de Maracuyá", "Maracuyá", 150m),
            ("Jugo de Maracuyá", "Agua", 300m),
            ("Jugo de Maracuyá", "Jarabe de Goma", 30m),
            ("Jugo Surtido", "Mix de Frutas", 200m),
            ("Jugo Surtido", "Agua", 300m),
            ("Jugo Surtido", "Jarabe de Goma", 30m),
            ("Limonada Clásica", "Agua", 270m),
            ("Limonada Clásica", "Azúcar", 20m),
            ("Limonada Clásica", "Limón", 1.5m),
            ("Limonada de Frutos Rojos", "Agua", 240m),
            ("Limonada de Frutos Rojos", "Frutos Rojos", 120m),
            ("Limonada de Frutos Rojos", "Azúcar", 20m),
            ("Limonada de Frutos Rojos", "Limón", 1.5m),
            ("Limonada de Durazno", "Agua", 240m),
            ("Limonada de Durazno", "Durazno", 120m),
            ("Limonada de Durazno", "Azúcar", 20m),
            ("Limonada de Durazno", "Limón", 1.5m),
            ("Kingston Negroni", "Ron Jamaiquino", 30m),
            ("Kingston Negroni", "Campari", 30m),
            ("Kingston Negroni", "Vermut Rojo", 30m),
            ("Kingston Negroni", "Hielo", 80m),
            ("Casino de Menta", "Vodka", 45m),
            ("Casino de Menta", "Licor de Menta", 30m),
            ("Casino de Menta", "Crema de Leche", 30m),
            ("Casino de Menta", "Crema de Cacao Blanca", 30m),
            ("Casino de Menta", "Hielo", 80m),
            ("Old Fashioned", "Whisky Bourbon", 60m),
            ("Old Fashioned", "Azúcar Blanca", 15m),
            ("Old Fashioned", "Angostura (Amargo)", 5m),
            ("Old Fashioned", "Agua", 30m),
            ("Old Fashioned", "Naranja", 0.25m),
            ("Margarita", "Tequila", 50m),
            ("Margarita", "Triple Sec", 25m),
            ("Margarita", "Limón", 1m),
            ("Margarita", "Hielo", 80m),
            ("Negroni", "Gin", 30m),
            ("Negroni", "Vermut Rojo", 30m),
            ("Negroni", "Campari", 30m),
            ("Negroni", "Hielo", 80m),
            ("Orgasmo", "Baileys", 30m),
            ("Orgasmo", "Amaretto", 30m),
            ("Orgasmo", "Kahlúa (Licor de Café)", 30m),
            ("Orgasmo", "Hielo", 80m),
            ("Whisky Sour", "Whisky", 60m),
            ("Whisky Sour", "Limón", 1m),
            ("Whisky Sour", "Jarabe de Goma", 20m),
            ("Whisky Sour", "Hielo", 80m),
            ("Piña Colada", "Ron Blanco", 50m),
            ("Piña Colada", "Crema de Coco", 30m),
            ("Piña Colada", "Piña (Lata)", 90m),
            ("Piña Colada", "Hielo", 80m),
            ("Charro Negro", "Tequila", 50m),
            ("Charro Negro", "Gaseosa", 30m),
            ("Charro Negro", "Limón", 1m),
            ("Charro Negro", "Hielo", 80m),
            ("Godfather", "Whisky", 45m),
            ("Godfather", "Amaretto", 25m),
            ("Godfather", "Hielo", 80m),
            ("Pisco Sour", "Pisco", 60m),
            ("Pisco Sour", "Limón", 2m),
            ("Pisco Sour", "Jarabe de Goma", 20m),
            ("Pisco Sour", "Huevos", 1m),
            ("Pisco Sour", "Angostura (Amargo)", 2m),
            ("Pisco Sour", "Hielo", 80m),
            ("Algarrobina", "Pisco", 60m),
            ("Algarrobina", "Algarrobina (Licor)", 30m),
            ("Algarrobina", "Leche Entera", 30m),
            ("Algarrobina", "Canela", 3m),
            ("Algarrobina", "Hielo", 80m),
            ("Capitán", "Pisco", 60m),
            ("Capitán", "Vermut Rojo", 30m),
            ("Capitán", "Hielo", 80m),
            ("Gin Tonic", "Gin", 50m),
            ("Gin Tonic", "Agua Tónica", 150m),
            ("Gin Tonic", "Limón", 0.5m),
            ("Gin Tonic", "Hielo", 80m),
            ("Fernandito", "Fernet", 50m),
            ("Fernandito", "Gaseosa", 60m),
            ("Fernandito", "Hielo", 80m),
            ("Mojito Clásico", "Ron Blanco", 50m),
            ("Mojito Clásico", "Jugo de Limón", 25m),
            ("Mojito Clásico", "Jarabe de Goma", 30m),
            ("Mojito Clásico", "Hielo", 80m),
            ("Chilcano Clásico", "Pisco", 60m),
            ("Chilcano Clásico", "Limón", 2m),
            ("Chilcano Clásico", "Ginger Ale", 60m),
            ("Chilcano Clásico", "Angostura (Amargo)", 2m),
            ("Chilcano Clásico", "Hielo", 80m),
            ("Cuba", "Ron Jamaiquino", 50m),
            ("Cuba", "Gaseosa", 60m),
            ("Cuba", "Limón", 0.5m),
            ("Cuba", "Hielo", 80m),
            ("Sinners Margarita", "Tequila", 50m),
            ("Sinners Margarita", "Kahlúa (Licor de Café)", 20m),
            ("Sinners Margarita", "Limón", 1m),
            ("Sinners Margarita", "Jarabe de Goma", 15m),
            ("Sinners Margarita", "Hielo", 80m),
            ("Café Irlandés", "Whisky", 50m),
            ("Café Irlandés", "Jarabe de Goma", 30m),
            ("Café Irlandés", "Café en Grano", 18m),
            ("Café Irlandés", "Leche Entera", 30m),
            ("Café Irlandés", "Hielo", 80m),
            ("Espresso Martini", "Vodka", 50m),
            ("Espresso Martini", "Kahlúa (Licor de Café)", 20m),
            ("Espresso Martini", "Jarabe de Vainilla", 10m),
            ("Espresso Martini", "Hielo", 80m),
            ("Toro Ruso", "Vodka", 50m),
            ("Toro Ruso", "Jarabe de Vainilla", 20m),
            ("Toro Ruso", "Café en Grano", 18m),
            ("Toro Ruso", "Ginger Ale", 60m),
            ("Toro Ruso", "Hielo", 80m),
            ("Old Coffee Fashion", "Ron Jamaiquino", 60m),
            ("Old Coffee Fashion", "Kahlúa (Licor de Café)", 10m),
            ("Old Coffee Fashion", "Angostura (Amargo)", 5m),
            ("Old Coffee Fashion", "Naranja", 0.25m),
            ("Old Coffee Fashion", "Hielo", 80m),
            ("Café Negroni", "Gin", 30m),
            ("Café Negroni", "Campari", 30m),
            ("Café Negroni", "Vermut Rojo", 30m),
            ("Café Negroni", "Hielo", 80m),
            ("Shakerato Baileys", "Baileys", 50m),
            ("Shakerato Baileys", "Café en Grano", 36m),
            ("Shakerato Baileys", "Hielo", 80m),
            ("Café Sour", "Pisco", 60m),
            ("Café Sour", "Café en Grano", 30m),
            ("Café Sour", "Limón", 1m),
            ("Café Sour", "Jarabe de Goma", 20m),
            ("Café Sour", "Huevos", 1m),
            ("Café Sour", "Angostura (Amargo)", 2m),
            ("Café Sour", "Hielo", 80m),
            ("Café Mexicano", "Café en Grano", 18m),
            ("Café Mexicano", "Tequila", 45m),
            ("Café Mexicano", "Kahlúa (Licor de Café)", 15m),
            ("Café Mexicano", "Leche Entera", 40m),
            ("Café Mexicano", "Hielo", 80m),
            ("Ginger Ale Coffee", "Vodka", 50m),
            ("Ginger Ale Coffee", "Jarabe de Vainilla", 20m),
            ("Ginger Ale Coffee", "Café en Grano", 18m),
            ("Ginger Ale Coffee", "Ginger Ale", 60m),
            ("Ginger Ale Coffee", "Hielo", 80m),
            ("Ruso Negro", "Vodka", 50m),
            ("Ruso Negro", "Kahlúa (Licor de Café)", 20m),
            ("Ruso Negro", "Hielo", 80m),
            ("Ruso Blanco", "Vodka", 45m),
            ("Ruso Blanco", "Jarabe de Goma", 30m),
            ("Ruso Blanco", "Café en Grano", 18m),
            ("Ruso Blanco", "Leche Entera", 40m),
            ("Ruso Blanco", "Hielo", 80m),
        };

        foreach (var (nombreProducto, nombreIngrediente, cantidad) in recetaProductoData)
        {
            context.RecetasProducto.Add(new RecetaProducto
            {
                Producto = productos[nombreProducto],
                Ingrediente = porNombre[nombreIngrediente],
                CantidadRequerida = cantidad
            });
        }
        await context.SaveChangesAsync();

        // Productos donde el cliente elige "Leche Entera" o "Leche Deslactosada" (grupo
        // modificador "Tipo de Leche"): la cantidad de leche depende del producto, así que
        // se registra como sobrescritura por producto en vez de receta fija. Ver
        // RecetaOpcionModificador.ProductoId.
        var recetaLecheData = new (string Producto, decimal Cantidad)[]
        {
            ("Bombón", 100m),
            ("Capuccino", 240m),
            ("Capuccino de Caramelo", 240m),
            ("Capuccino de Menta", 240m),
            ("Moccacino", 180m),
            ("Frappé Clásico", 100m),
            ("Frappé Caramelo", 100m),
            ("Frappé Chocolate", 100m),
            ("Frappé Oreo", 100m),
            ("Batido de Plátano", 300m),
            ("Batido de Fresa", 300m),
            ("Batido de Arándano", 300m),
            ("Batido Especial", 300m),
        };

        var grupoLeche = await context.GruposModificadores
            .Include(g => g.Opciones)
            .FirstAsync(g => g.Nombre == "Tipo de Leche");
        var opcionEntera = grupoLeche.Opciones.First(o => o.Nombre == "Leche Entera");
        var opcionDeslactosada = grupoLeche.Opciones.First(o => o.Nombre == "Leche Deslactosada");

        foreach (var (nombreProducto, cantidad) in recetaLecheData)
        {
            var producto = productos[nombreProducto];
            context.RecetasOpcionModificador.Add(new RecetaOpcionModificador
            {
                OpcionModificador = opcionEntera,
                Ingrediente = porNombre["Leche Entera"],
                Producto = producto,
                CantidadRequerida = cantidad
            });
            context.RecetasOpcionModificador.Add(new RecetaOpcionModificador
            {
                OpcionModificador = opcionDeslactosada,
                Ingrediente = porNombre["Leche Deslactosada"],
                Producto = producto,
                CantidadRequerida = cantidad
            });
        }
        await context.SaveChangesAsync();
    }
}
