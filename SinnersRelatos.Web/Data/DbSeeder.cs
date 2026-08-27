using Microsoft.EntityFrameworkCore;
using SinnersRelatos.Web.Models;

namespace SinnersRelatos.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!await context.Categorias.AnyAsync())
            await SeedCatalogoAsync(context);

        if (!await context.Mesas.AnyAsync())
            await SeedMesasAsync(context);
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
        Vincular(grupoLeche,
            "sin.cafe.espresso", "sin.cafe.doppio", "sin.cafe.americano", "sin.cafe.bombon",
            "sin.cafe.capuccino", "sin.cafe.capuccino_caramelo", "sin.cafe.capuccino_menta",
            "sin.cafe.moccacino", "sin.cafe.affogato", "sin.cafe.coldbrew",
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
}
