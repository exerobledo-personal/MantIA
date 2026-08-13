namespace MantIA.WEB.Demo;

public static class DatosDemo
{
    private static readonly DateTime Hoy = DateTime.Today;

    public static EmpresaVm EmpresaActual => Empresas[0];

    public static readonly UsuarioVm UsuarioActual = new()
    {
        Nombre = "Exequiel",
        Apellido = "Robledo",
        Email = "exequiel.robledo@mantia.com.ar",
        Rol = Roles.SuperAdmin,
        Nivel = "Sr",
        Estado = EstadoGenerico.Activo,
        FechaAlta = Hoy.AddYears(-2),
        UltimoAcceso = DateTime.Now.AddMinutes(-3)
    };

    public static readonly List<PlantaVm> Plantas =
    [
        new() { Nombre = "Planta Norte", Direccion = "Ruta Panamericana Km 44,5", Localidad = "Pilar, Buenos Aires",
                Latitud = -34.4587m, Longitud = -58.9142m, FechaAlta = Hoy.AddMonths(-14) },
        new() { Nombre = "Planta Oeste", Direccion = "Av. Márquez 2340", Localidad = "San Martín, Buenos Aires",
                Latitud = -34.5706m, Longitud = -58.5361m, FechaAlta = Hoy.AddMonths(-11) },
        new() { Nombre = "Planta Sur", Direccion = "Av. Industrial 1500", Localidad = "Avellaneda, Buenos Aires",
                Latitud = -34.6637m, Longitud = -58.3816m, FechaAlta = Hoy.AddMonths(-6) }
    ];

    public static PlantaVm PlantaNorte => Plantas[0];
    public static PlantaVm PlantaOeste => Plantas[1];
    public static PlantaVm PlantaSur => Plantas[2];

    public static readonly List<CatalogoMaquinaVm> Catalogo =
    [
        new() { Marca = "Tetra Pak", Modelo = "A3/Speed", Categoria = "Llenado aséptico",
                FallasComunes = ["Desgaste de mordazas de sellado", "Fuga en válvula aséptica", "Desalineación de banda de cartón"],
                RepuestosSugeridos = ["Kit de mordazas de sellado", "Junta aséptica DN50", "Correa dentada HTD-8M"],
                IntervalosMantenimiento = "Preventivo cada 2.000 h · Cambio de juntas cada 6 meses",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-12), TenantsQueLoUsan = 7 },
        new() { Marca = "Atlas Copco", Modelo = "GA55", Categoria = "Compresor de tornillo",
                FallasComunes = ["Saturación del filtro de aceite", "Sobretemperatura por intercambiador sucio", "Fuga en válvula de mínima presión"],
                RepuestosSugeridos = ["Filtro de aceite FA-220", "Separador aire-aceite", "Kit de válvula de admisión"],
                IntervalosMantenimiento = "Filtro de aceite cada 4.000 h · Aceite cada 8.000 h",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-13), TenantsQueLoUsan = 23 },
        new() { Marca = "Alfa Laval", Modelo = "FrontLine 20", Categoria = "Intercambiador de placas",
                FallasComunes = ["Fuga entre placas por junta vencida", "Ensuciamiento por incrustación", "Pérdida de eficiencia térmica"],
                RepuestosSugeridos = ["Juego de juntas NBR", "Placa de acero inoxidable AISI 316", "Sensor PT100"],
                IntervalosMantenimiento = "Apertura e inspección anual · Limpieza CIP semanal",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-10), TenantsQueLoUsan = 11 },
        new() { Marca = "GEA", Modelo = "Ariete NS3006", Categoria = "Homogeneizador",
                FallasComunes = ["Desgaste de pistones cerámicos", "Falla en válvula homogeneizadora", "Vibración por rodamiento"],
                RepuestosSugeridos = ["Pistón cerámico", "Kit de válvula homogeneizadora", "Rodamiento SKF 22315"],
                IntervalosMantenimiento = "Pistones cada 3.000 h · Aceite de cárter cada 2.000 h",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-9), TenantsQueLoUsan = 5 },
        new() { Marca = "Krones", Modelo = "Contiroll", Categoria = "Etiquetadora rotativa",
                FallasComunes = ["Corte irregular por cuchilla desgastada", "Falla del servo de arrastre", "Adhesivo insuficiente"],
                RepuestosSugeridos = ["Cuchilla de corte", "Rodillo de goma de arrastre", "Correa de transmisión"],
                IntervalosMantenimiento = "Cuchilla cada 1.500 h · Revisión de servos trimestral",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-8), TenantsQueLoUsan = 14 },
        new() { Marca = "Krones", Modelo = "Modulfill", Categoria = "Llenadora de botellas",
                FallasComunes = ["Goteo en válvula de llenado", "Falla de sensor de nivel", "Desgaste de estrella de transporte"],
                RepuestosSugeridos = ["Válvula de llenado", "Sensor de nivel capacitivo", "Estrella de transporte"],
                IntervalosMantenimiento = "Válvulas cada 4.000 h · Sanitización diaria",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-7), TenantsQueLoUsan = 9 },
        new() { Marca = "Mayekawa", Modelo = "Mycom 6WB", Categoria = "Compresor de refrigeración",
                FallasComunes = ["Pérdida de amoníaco por sello mecánico", "Alta temperatura de descarga", "Falla del presostato"],
                RepuestosSugeridos = ["Sello mecánico", "Filtro de succión", "Presostato de alta"],
                IntervalosMantenimiento = "Sello mecánico cada 12.000 h · Análisis de aceite trimestral",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-5), TenantsQueLoUsan = 4 },
        new() { Marca = "Multivac", Modelo = "RX 4.0", Categoria = "Termoformadora",
                FallasComunes = ["Sellado deficiente por placa desgastada", "Falla de bomba de vacío", "Ruptura de film"],
                RepuestosSugeridos = ["Placa de sellado", "Kit de bomba de vacío", "Junta de cámara"],
                IntervalosMantenimiento = "Placa de sellado cada 2.500 h · Aceite de bomba cada 1.000 h",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-4), TenantsQueLoUsan = 6 },
        new() { Marca = "Ishida", Modelo = "CCW-RV", Categoria = "Pesadora multicabezal",
                FallasComunes = ["Deriva de celda de carga", "Atasco en cangilón", "Falla de vibrador de canal"],
                RepuestosSugeridos = ["Celda de carga", "Cangilón de descarga", "Bobina de vibrador"],
                IntervalosMantenimiento = "Calibración mensual · Limpieza profunda semanal",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-3), TenantsQueLoUsan = 8 },
        new() { Marca = "Bosch", Modelo = "Universal UL-S", Categoria = "Caldera de vapor",
                FallasComunes = ["Incrustación en tubos de humo", "Falla del quemador por electrodo sucio", "Fuga en válvula de seguridad"],
                RepuestosSugeridos = ["Electrodo de encendido", "Válvula de seguridad", "Junta de tapa de registro"],
                IntervalosMantenimiento = "Inspección anual obligatoria · Purga diaria · Análisis de agua semanal",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-13), TenantsQueLoUsan = 18 },
        new() { Marca = "Bühler", Modelo = "Antares Plus", Categoria = "Molino de rodillos",
                FallasComunes = ["Desgaste de estría de rodillo", "Vibración por desbalanceo", "Falla de rascador"],
                RepuestosSugeridos = ["Rodillo estriado", "Rodamiento cónico", "Rascador de bronce"],
                IntervalosMantenimiento = "Reestriado cada 8.000 h · Lubricación semanal",
                Estado = EstadoEnriquecimiento.EnProceso, TenantsQueLoUsan = 3 },
        new() { Marca = "Grundfos", Modelo = "NB 65-200", Categoria = "Bomba centrífuga",
                FallasComunes = ["Fuga por sello mecánico", "Cavitación por filtro obstruido", "Rodamiento ruidoso"],
                RepuestosSugeridos = ["Sello mecánico BQQE", "Rodamiento 6308", "Impulsor de acero inoxidable"],
                IntervalosMantenimiento = "Sello cada 6.000 h · Verificación de vibración mensual",
                Estado = EstadoEnriquecimiento.Completado, FechaUltimoEnriquecimiento = Hoy.AddMonths(-2), TenantsQueLoUsan = 31 },
        new() { Marca = "SEW-Eurodrive", Modelo = "R77 DRN90", Categoria = "Motorreductor",
                FallasComunes = ["Pérdida de aceite por retén", "Sobrecalentamiento del motor", "Desgaste de engranaje"],
                RepuestosSugeridos = ["Retén de salida", "Rodamiento de eje", "Ventilador de motor"],
                IntervalosMantenimiento = "Cambio de aceite cada 10.000 h",
                Estado = EstadoEnriquecimiento.Pendiente, TenantsQueLoUsan = 2 }
    ];

    public static readonly List<MaquinaVm> Maquinas = ConstruirMaquinas();

    private static List<MaquinaVm> ConstruirMaquinas()
    {
        MaquinaVm Nueva(string codigo, string nombre, string serie, PlantaVm planta, int catalogo,
            string linea, EstadoMaquina estado, Criticidad criticidad, int mesesAlta, int diasUltima, int horas)
        {
            var ficha = Catalogo[catalogo];
            return new MaquinaVm
            {
                Codigo = codigo,
                Nombre = nombre,
                NumeroSerie = serie,
                PlantaId = planta.Id,
                Planta = planta.Nombre,
                CatalogoMaquinaId = ficha.Id,
                Marca = ficha.Marca,
                Modelo = ficha.Modelo,
                Linea = linea,
                Estado = estado,
                CriticidadOperativa = criticidad,
                FechaAlta = Hoy.AddMonths(-mesesAlta),
                UltimaIntervencion = Hoy.AddDays(-diasUltima),
                HorasOperacion = horas,
                Enriquecimiento = ficha.Estado
            };
        }

        return
        [
            Nueva("MAQ-001", "Llenadora Aséptica L1", "TP-A3S-88421", PlantaNorte, 0, "Línea 1 · UHT", EstadoMaquina.Operativa, Criticidad.Critica, 13, 6, 18420),
            Nueva("MAQ-002", "Compresor Línea 3", "AC-GA55-20419", PlantaNorte, 1, "Servicios auxiliares", EstadoMaquina.Operativa, Criticidad.Alta, 13, 2, 24160),
            Nueva("MAQ-003", "Pasteurizador de Placas P2", "AL-FL20-3312", PlantaNorte, 2, "Línea 2 · Leche", EstadoMaquina.EnMantenimiento, Criticidad.Critica, 12, 1, 15980),
            Nueva("MAQ-004", "Homogeneizador H1", "GEA-NS3006-771", PlantaNorte, 3, "Línea 1 · UHT", EstadoMaquina.Operativa, Criticidad.Alta, 12, 21, 14210),
            Nueva("MAQ-005", "Etiquetadora Rotativa E4", "KR-CTR-55190", PlantaOeste, 4, "Línea 4 · Botellas", EstadoMaquina.Operativa, Criticidad.Media, 10, 14, 9870),
            Nueva("MAQ-006", "Llenadora de Botellas L4", "KR-MDF-44821", PlantaOeste, 5, "Línea 4 · Botellas", EstadoMaquina.Detenida, Criticidad.Critica, 10, 0, 11340),
            Nueva("MAQ-007", "Túnel de Frío T1", "MYC-6WB-1204", PlantaOeste, 6, "Cámara de frío", EstadoMaquina.Operativa, Criticidad.Alta, 9, 33, 20110),
            Nueva("MAQ-008", "Envasadora Flow Pack F2", "MV-RX40-6621", PlantaSur, 7, "Línea 6 · Snacks", EstadoMaquina.Operativa, Criticidad.Media, 5, 9, 4320),
            Nueva("MAQ-009", "Balanza Multicabezal B3", "ISH-CCWRV-9083", PlantaSur, 8, "Línea 6 · Snacks", EstadoMaquina.Operativa, Criticidad.Media, 5, 27, 4180),
            Nueva("MAQ-010", "Caldera de Vapor CV1", "BSH-ULS-2210", PlantaNorte, 9, "Servicios auxiliares", EstadoMaquina.Operativa, Criticidad.Critica, 13, 44, 31200),
            Nueva("MAQ-011", "Molino de Rodillos MR2", "BUH-ANT-7741", PlantaOeste, 10, "Preparación", EstadoMaquina.Operativa, Criticidad.Alta, 8, 18, 7650),
            Nueva("MAQ-012", "Bomba Centrífuga BC5", "GRF-NB65-3390", PlantaNorte, 11, "Servicios auxiliares", EstadoMaquina.Operativa, Criticidad.Media, 11, 12, 16700),
            Nueva("MAQ-013", "Cinta Transportadora CT7", "SEW-R77-1188", PlantaSur, 12, "Línea 6 · Snacks", EstadoMaquina.Operativa, Criticidad.Baja, 4, 55, 3120),
            Nueva("MAQ-014", "Bomba de Proceso BP2", "GRF-NB65-3391", PlantaSur, 11, "Línea 6 · Snacks", EstadoMaquina.Inactiva, Criticidad.Baja, 4, 90, 2890)
        ];
    }

    public static readonly List<RepuestoVm> Repuestos = ConstruirRepuestos();

    private static List<RepuestoVm> ConstruirRepuestos()
    {
        RepuestoVm Nuevo(string nombre, string parte, string proveedor, string unidad, decimal actual,
            decimal minimo, Criticidad criticidad, int plazo, decimal costo, params string[] maquinas)
        {
            var vinculadas = Maquinas.Where(m => maquinas.Contains(m.Codigo)).ToList();
            return new RepuestoVm
            {
                Nombre = nombre,
                NumeroParte = parte,
                ProveedorReferencia = proveedor,
                UnidadMedida = unidad,
                StockActual = actual,
                StockMinimo = minimo,
                Criticidad = criticidad,
                PlazoReposicionDias = plazo,
                CostoUnitario = costo,
                MaquinaIds = vinculadas.Select(m => m.Id).ToList(),
                Maquinas = vinculadas.Select(m => m.Codigo + " · " + m.Nombre).ToList(),
                FechaAlta = Hoy.AddMonths(-9)
            };
        }

        return
        [
            Nuevo("Filtro de aceite", "FA-220", "Atlas Copco Argentina", "Unidad", 1, 3, Criticidad.Alta, 21, 84500, "MAQ-002"),
            Nuevo("Kit de mordazas de sellado", "TP-MS-4410", "Tetra Pak Cono Sur", "Kit", 2, 2, Criticidad.Critica, 45, 1240000, "MAQ-001"),
            Nuevo("Junta aséptica DN50", "JA-DN50", "Sealtec SRL", "Unidad", 14, 8, Criticidad.Alta, 12, 32800, "MAQ-001", "MAQ-014"),
            Nuevo("Juego de juntas NBR placas", "AL-JNBR-20", "Alfa Laval Argentina", "Juego", 0, 2, Criticidad.Critica, 30, 690000, "MAQ-003"),
            Nuevo("Placa inoxidable AISI 316", "AL-PL316", "Alfa Laval Argentina", "Unidad", 6, 4, Criticidad.Media, 60, 148000, "MAQ-003"),
            Nuevo("Pistón cerámico homogeneizador", "GEA-PC-3006", "GEA Group", "Unidad", 3, 3, Criticidad.Critica, 55, 875000, "MAQ-004"),
            Nuevo("Rodamiento SKF 22315", "SKF-22315", "Distribuidora Rodasur", "Unidad", 8, 4, Criticidad.Media, 10, 96500, "MAQ-004", "MAQ-011"),
            Nuevo("Cuchilla de corte etiquetadora", "KR-CU-118", "Krones Sudamérica", "Unidad", 5, 6, Criticidad.Alta, 25, 118000, "MAQ-005"),
            Nuevo("Válvula de llenado", "KR-VL-3320", "Krones Sudamérica", "Unidad", 4, 8, Criticidad.Critica, 40, 264000, "MAQ-006"),
            Nuevo("Sensor de nivel capacitivo", "IFM-KI5083", "IFM Electronic", "Unidad", 11, 5, Criticidad.Media, 15, 71200, "MAQ-006", "MAQ-001"),
            Nuevo("Sello mecánico compresor NH3", "MYC-SM-6WB", "Mayekawa Argentina", "Unidad", 1, 2, Criticidad.Critica, 50, 1120000, "MAQ-007"),
            Nuevo("Placa de sellado termoformadora", "MV-PS-40", "Multivac Argentina", "Unidad", 3, 2, Criticidad.Alta, 35, 385000, "MAQ-008"),
            Nuevo("Celda de carga multicabezal", "ISH-CC-30", "Ishida Latinoamérica", "Unidad", 7, 4, Criticidad.Media, 28, 156000, "MAQ-009"),
            Nuevo("Electrodo de encendido caldera", "BSH-EE-77", "Bosch Industrial", "Unidad", 2, 4, Criticidad.Alta, 18, 62400, "MAQ-010"),
            Nuevo("Válvula de seguridad 10 bar", "VS-10B", "Instrumentos Delta", "Unidad", 3, 2, Criticidad.Critica, 22, 298000, "MAQ-010"),
            Nuevo("Sello mecánico BQQE", "GRF-BQQE-65", "Grundfos Argentina", "Unidad", 9, 4, Criticidad.Media, 14, 88700, "MAQ-012", "MAQ-014"),
            Nuevo("Correa dentada HTD-8M", "CD-HTD8M", "Transmisiones del Sur", "Unidad", 22, 10, Criticidad.Baja, 7, 24300, "MAQ-001", "MAQ-005", "MAQ-013"),
            Nuevo("Retén de salida motorreductor", "SEW-RT-77", "SEW-Eurodrive Argentina", "Unidad", 16, 6, Criticidad.Baja, 12, 18900, "MAQ-013"),
            Nuevo("Rodillo estriado molino", "BUH-RE-220", "Bühler Argentina", "Unidad", 2, 2, Criticidad.Alta, 70, 940000, "MAQ-011"),
            Nuevo("Aceite lubricante ISO VG 46", "LUB-VG46", "Shell Argentina", "Litro", 180, 100, Criticidad.Baja, 5, 4200, "MAQ-002", "MAQ-007", "MAQ-012")
        ];
    }

    public static readonly List<UsuarioVm> Usuarios =
    [
        new() { Nombre = "Laura", Apellido = "Giménez", Email = "laura.gimenez@alimentospampa.com.ar", Rol = Roles.AdminEmpresa,
                Nivel = "Sr", Plantas = ["Planta Norte", "Planta Oeste", "Planta Sur"], FechaAlta = Hoy.AddMonths(-14),
                UltimoAcceso = DateTime.Now.AddHours(-2), OrdenesAsignadas = 0 },
        new() { Nombre = "Martín", Apellido = "Sosa", Email = "martin.sosa@alimentospampa.com.ar", Rol = Roles.Gerente,
                Nivel = "Sr", Plantas = ["Planta Norte", "Planta Oeste", "Planta Sur"], FechaAlta = Hoy.AddMonths(-13),
                UltimoAcceso = DateTime.Now.AddHours(-5), OrdenesAsignadas = 0 },
        new() { Nombre = "Diego", Apellido = "Ferrero", Email = "diego.ferrero@alimentospampa.com.ar", Rol = Roles.Supervisor,
                Nivel = "Sr", Plantas = ["Planta Norte"], FechaAlta = Hoy.AddMonths(-13),
                UltimoAcceso = DateTime.Now.AddMinutes(-40), OrdenesAsignadas = 4 },
        new() { Nombre = "Carla", Apellido = "Benítez", Email = "carla.benitez@alimentospampa.com.ar", Rol = Roles.Supervisor,
                Nivel = "Sr", Plantas = ["Planta Oeste"], FechaAlta = Hoy.AddMonths(-10),
                UltimoAcceso = DateTime.Now.AddHours(-1), OrdenesAsignadas = 3 },
        new() { Nombre = "Rubén", Apellido = "Quiroga", Email = "ruben.quiroga@alimentospampa.com.ar", Rol = Roles.Supervisor,
                Nivel = "Jr", Plantas = ["Planta Sur"], FechaAlta = Hoy.AddMonths(-5),
                UltimoAcceso = DateTime.Now.AddHours(-9), OrdenesAsignadas = 2 },
        new() { Nombre = "Javier", Apellido = "Molina", Email = "javier.molina@alimentospampa.com.ar", Rol = Roles.Empleado,
                Nivel = "Sr", Plantas = ["Planta Norte"], FechaAlta = Hoy.AddMonths(-12),
                UltimoAcceso = DateTime.Now.AddMinutes(-18), OrdenesAsignadas = 5 },
        new() { Nombre = "Sofía", Apellido = "Ledesma", Email = "sofia.ledesma@alimentospampa.com.ar", Rol = Roles.Empleado,
                Nivel = "Jr", Plantas = ["Planta Norte"], FechaAlta = Hoy.AddMonths(-4),
                UltimoAcceso = DateTime.Now.AddHours(-7), OrdenesAsignadas = 3 },
        new() { Nombre = "Nicolás", Apellido = "Ayala", Email = "nicolas.ayala@alimentospampa.com.ar", Rol = Roles.Empleado,
                Nivel = "Jr", Plantas = ["Planta Oeste"], FechaAlta = Hoy.AddMonths(-7),
                UltimoAcceso = DateTime.Now.AddHours(-26), OrdenesAsignadas = 2 },
        new() { Nombre = "Verónica", Apellido = "Paz", Email = "veronica.paz@alimentospampa.com.ar", Rol = Roles.Empleado,
                Nivel = "Sr", Plantas = ["Planta Sur"], FechaAlta = Hoy.AddMonths(-3),
                UltimoAcceso = DateTime.Now.AddHours(-13), OrdenesAsignadas = 1 },
        new() { Nombre = "Hugo", Apellido = "Ibarra", Email = "hugo.ibarra@alimentospampa.com.ar", Rol = Roles.Empleado,
                Nivel = "Jr", Plantas = ["Planta Oeste"], Estado = EstadoGenerico.Inactivo, FechaAlta = Hoy.AddMonths(-9),
                UltimoAcceso = Hoy.AddMonths(-2), OrdenesAsignadas = 0 }
    ];

    public static readonly List<NivelPermisoVm> Niveles =
    [
        new() { Nombre = "Jr", Descripcion = "Acceso operativo restringido. No puede dar de baja registros ni cerrar órdenes de trabajo.", Usuarios = 4 },
        new() { Nombre = "Sr", Descripcion = "Acceso operativo completo dentro de su rol, incluyendo bajas y cierres.", Usuarios = 6 }
    ];

    public static readonly List<OrdenTrabajoVm> Ordenes = ConstruirOrdenes();

    private static List<OrdenTrabajoVm> ConstruirOrdenes()
    {
        var lista = new List<OrdenTrabajoVm>();
        var n = 1;

        OrdenTrabajoVm Nueva(string codigoMaquina, TipoMantenimiento tipo, string falla, string descripcion,
            Prioridad prioridad, EstadoOrden estado, string tecnico, int diasApertura, double? horas,
            string resolucion, params (string parte, decimal cant)[] consumos)
        {
            var maquina = Maquinas.First(m => m.Codigo == codigoMaquina);
            var apertura = DateTime.Now.AddDays(-diasApertura);
            var orden = new OrdenTrabajoVm
            {
                Numero = $"OT-{2026}-{n++:D4}",
                MaquinaId = maquina.Id,
                Maquina = $"{maquina.Codigo} · {maquina.Nombre}",
                Planta = maquina.Planta,
                UsuarioAsignado = tecnico,
                TipoMantenimiento = tipo,
                TipoFalla = falla,
                Descripcion = descripcion,
                Prioridad = prioridad,
                Estado = estado,
                FechaApertura = apertura,
                FechaCierre = horas.HasValue ? apertura.AddHours(horas.Value) : null,
                ResolucionAplicada = resolucion
            };

            foreach (var (parte, cant) in consumos)
            {
                var repuesto = Repuestos.FirstOrDefault(r => r.NumeroParte == parte);
                if (repuesto is null) continue;
                orden.Repuestos.Add(new ConsumoRepuestoVm
                {
                    RepuestoId = repuesto.Id,
                    Repuesto = repuesto.Nombre,
                    NumeroParte = repuesto.NumeroParte,
                    Cantidad = cant,
                    StockAnterior = repuesto.StockActual + cant
                });
            }

            lista.Add(orden);
            return orden;
        }

        Nueva("MAQ-006", TipoMantenimiento.Correctivo, "Goteo en válvula de llenado",
            "Tres válvulas de la estrella 4 pierden producto durante el ciclo. Línea detenida.",
            Prioridad.Urgente, EstadoOrden.Abierta, "Carla Benítez", 0, null, "");
        Nueva("MAQ-003", TipoMantenimiento.Correctivo, "Fuga entre placas",
            "Pérdida visible en el paquete de placas del sector de regeneración. Se aisló el equipo.",
            Prioridad.Alta, EstadoOrden.EnCurso, "Diego Ferrero", 1, null, "");
        Nueva("MAQ-002", TipoMantenimiento.Preventivo, "Cambio de filtro de aceite",
            "Preventivo programado por horas de operación. Requiere filtro FA-220.",
            Prioridad.Media, EstadoOrden.Abierta, "Javier Molina", 2, null, "");
        Nueva("MAQ-010", TipoMantenimiento.Preventivo, "Purga y análisis de agua",
            "Control semanal de calidad de agua de alimentación y purga de fondo.",
            Prioridad.Media, EstadoOrden.Abierta, "Javier Molina", 3, null, "");
        Nueva("MAQ-001", TipoMantenimiento.Correctivo, "Sellado deficiente",
            "Envases con sellado irregular en la posición 6. Se reemplazaron mordazas.",
            Prioridad.Alta, EstadoOrden.Cerrada, "Diego Ferrero", 6, 5.5,
            "Se reemplazó el kit de mordazas y se recalibró la presión de sellado.", ("TP-MS-4410", 1));
        Nueva("MAQ-009", TipoMantenimiento.Preventivo, "Calibración mensual",
            "Calibración de las 14 celdas de carga con pesa patrón certificada.",
            Prioridad.Baja, EstadoOrden.Cerrada, "Verónica Paz", 9, 3.0,
            "Calibración completada. Desviación máxima 0,4 g dentro de tolerancia.");
        Nueva("MAQ-005", TipoMantenimiento.Correctivo, "Corte irregular de etiqueta",
            "Etiquetas con borde dentado. Cuchilla al final de su vida útil.",
            Prioridad.Media, EstadoOrden.Cerrada, "Nicolás Ayala", 14, 2.5,
            "Se reemplazó la cuchilla de corte y se ajustó el ángulo de ataque.", ("KR-CU-118", 1));
        Nueva("MAQ-012", TipoMantenimiento.Correctivo, "Fuga por sello mecánico",
            "Goteo constante en el sello del lado de acople.",
            Prioridad.Media, EstadoOrden.Cerrada, "Javier Molina", 12, 4.0,
            "Se reemplazó el sello mecánico BQQE y se verificó la alineación del acople.", ("GRF-BQQE-65", 1));
        Nueva("MAQ-004", TipoMantenimiento.Preventivo, "Cambio de pistones cerámicos",
            "Preventivo por horas. Reemplazo de los tres pistones cerámicos.",
            Prioridad.Alta, EstadoOrden.Cerrada, "Diego Ferrero", 21, 7.5,
            "Se reemplazaron los tres pistones y se cambió el aceite de cárter.", ("GEA-PC-3006", 3));
        Nueva("MAQ-011", TipoMantenimiento.Correctivo, "Vibración anormal",
            "Vibración por encima de 7 mm/s en el rodamiento del lado motriz.",
            Prioridad.Alta, EstadoOrden.Cerrada, "Nicolás Ayala", 18, 6.0,
            "Se reemplazó el rodamiento cónico y se realizó balanceo dinámico.", ("SKF-22315", 2));
        Nueva("MAQ-007", TipoMantenimiento.Preventivo, "Análisis de aceite trimestral",
            "Toma de muestra y análisis de aceite del compresor de amoníaco.",
            Prioridad.Baja, EstadoOrden.Cerrada, "Carla Benítez", 33, 1.5,
            "Análisis dentro de parámetros. Próxima toma en 90 días.");
        Nueva("MAQ-008", TipoMantenimiento.Correctivo, "Sellado deficiente",
            "Bolsas con fugas en el control de hermeticidad. Placa de sellado con desgaste.",
            Prioridad.Media, EstadoOrden.Cerrada, "Verónica Paz", 9, 3.5,
            "Se reemplazó la placa de sellado y se recalibró la temperatura.", ("MV-PS-40", 1));
        Nueva("MAQ-013", TipoMantenimiento.Preventivo, "Lubricación semanal",
            "Lubricación de rodillos y verificación de tensión de banda.",
            Prioridad.Baja, EstadoOrden.Cerrada, "Verónica Paz", 55, 1.0,
            "Lubricación completada. Se ajustó la tensión de banda.");
        Nueva("MAQ-010", TipoMantenimiento.Correctivo, "Falla de encendido",
            "El quemador no enciende al primer intento. Electrodo con depósitos.",
            Prioridad.Alta, EstadoOrden.Cerrada, "Javier Molina", 44, 2.0,
            "Se limpió y reemplazó el electrodo de encendido.", ("BSH-EE-77", 1));
        Nueva("MAQ-001", TipoMantenimiento.Preventivo, "Cambio de juntas asépticas",
            "Preventivo semestral de juntas del circuito aséptico.",
            Prioridad.Media, EstadoOrden.Cerrada, "Sofía Ledesma", 62, 4.5,
            "Se reemplazaron seis juntas asépticas DN50 y se validó el circuito.", ("JA-DN50", 6));
        Nueva("MAQ-002", TipoMantenimiento.Correctivo, "Sobretemperatura",
            "Parada por alta temperatura de descarga. Intercambiador obstruido.",
            Prioridad.Alta, EstadoOrden.Cerrada, "Diego Ferrero", 71, 5.0,
            "Se limpió el intercambiador y se reemplazó el filtro de aceite.", ("FA-220", 1));

        return lista;
    }

    public static readonly List<RecomendacionVm> Recomendaciones = ConstruirRecomendaciones();

    private static List<RecomendacionVm> ConstruirRecomendaciones()
    {
        RecomendacionVm Nueva(string parte, string codigoMaquina, OrigenRecomendacion origen, string regla,
            decimal cantidad, string justificacion, List<string> evidencia, int confianza, Prioridad prioridad,
            int diasGeneracion, decimal impacto)
        {
            var repuesto = Repuestos.First(r => r.NumeroParte == parte);
            var maquina = Maquinas.First(m => m.Codigo == codigoMaquina);
            return new RecomendacionVm
            {
                RepuestoId = repuesto.Id,
                Repuesto = repuesto.Nombre,
                NumeroParte = repuesto.NumeroParte,
                Maquina = $"{maquina.Codigo} · {maquina.Nombre}",
                Planta = maquina.Planta,
                Origen = origen,
                ReglaAplicada = regla,
                CantidadSugerida = cantidad,
                StockActual = repuesto.StockActual,
                Justificacion = justificacion,
                Evidencia = evidencia,
                Confianza = confianza,
                Prioridad = prioridad,
                FechaGeneracion = DateTime.Now.AddDays(-diasGeneracion),
                ImpactoEstimado = impacto
            };
        }

        return
        [
            Nueva("AL-JNBR-20", "MAQ-003", OrigenRecomendacion.Regla,
                "Stock actual (0) por debajo del umbral mínimo (2)", 3,
                "El repuesto está en quiebre total sobre una máquina de criticidad crítica con una orden de trabajo abierta que lo requiere.",
                ["Stock actual: 0 de 2 mínimo", "OT-2026-0002 abierta sobre MAQ-003 desde hace 1 día",
                 "Plazo de reposición del proveedor: 30 días", "Máquina de criticidad Crítica en Línea 2"],
                100, Prioridad.Urgente, 1, 690000),

            Nueva("KR-VL-3320", "MAQ-006", OrigenRecomendacion.Regla,
                "Stock actual (4) por debajo del umbral mínimo (8)", 8,
                "Línea 4 detenida por goteo en tres válvulas. El stock disponible no cubre el reemplazo completo de la estrella.",
                ["Stock actual: 4 de 8 mínimo", "OT-2026-0001 urgente con línea detenida",
                 "Plazo de reposición: 40 días", "3 válvulas comprometidas en la intervención actual"],
                100, Prioridad.Urgente, 0, 1056000),

            Nueva("FA-220", "MAQ-002", OrigenRecomendacion.Modelo,
                "Frecuencia histórica de consumo sobre modelo Atlas Copco GA55", 4,
                "El modelo detecta un consumo de 1 unidad cada 4.000 horas de operación. Con 24.160 horas acumuladas y stock de 1 unidad, el próximo preventivo agota la existencia.",
                ["2 consumos registrados en los últimos 12 meses (OT-2026-0016, OT-2026-0003)",
                 "Intervalo del catálogo técnico: filtro cada 4.000 h",
                 "Horas desde el último cambio: 3.740", "Plazo de reposición: 21 días"],
                87, Prioridad.Alta, 2, 338000),

            Nueva("MYC-SM-6WB", "MAQ-007", OrigenRecomendacion.Modelo,
                "Proyección de vida útil sobre historial de intervenciones", 2,
                "El sello mecánico acumula 20.110 horas contra un intervalo recomendado de 12.000. El stock actual de 1 unidad queda por debajo del mínimo de 2 para un equipo de refrigeración con amoníaco.",
                ["Horas de operación: 20.110 sobre intervalo de 12.000 h",
                 "Stock actual: 1 de 2 mínimo", "Plazo de reposición: 50 días",
                 "Riesgo regulatorio: fuga de amoníaco en cámara de frío"],
                79, Prioridad.Alta, 3, 1120000),

            Nueva("BSH-EE-77", "MAQ-010", OrigenRecomendacion.Regla,
                "Stock actual (2) por debajo del umbral mínimo (4)", 4,
                "Caldera de criticidad crítica con historial de fallas de encendido. El stock no cubre dos intervenciones consecutivas.",
                ["Stock actual: 2 de 4 mínimo", "OT-2026-0014 cerrada por falla de encendido hace 44 días",
                 "Plazo de reposición: 18 días", "Equipo sujeto a inspección anual obligatoria"],
                100, Prioridad.Media, 4, 124800),

            Nueva("KR-CU-118", "MAQ-005", OrigenRecomendacion.Modelo,
                "Frecuencia histórica de consumo sobre modelo Krones Contiroll", 3,
                "El consumo histórico indica una cuchilla cada 1.500 horas. La máquina acumula 9.870 horas con stock de 5 unidades contra un mínimo de 6.",
                ["Stock actual: 5 de 6 mínimo", "1 consumo registrado hace 14 días (OT-2026-0007)",
                 "Intervalo del catálogo: cuchilla cada 1.500 h", "Plazo de reposición: 25 días"],
                72, Prioridad.Media, 5, 354000),

            Nueva("BUH-RE-220", "MAQ-011", OrigenRecomendacion.Modelo,
                "Proyección de desgaste con plazo de reposición extendido", 1,
                "El plazo de reposición de 70 días supera ampliamente la cobertura de stock proyectada. Conviene anticipar la compra aunque el umbral todavía no se haya cruzado.",
                ["Stock actual: 2 de 2 mínimo, sin margen", "Plazo de reposición: 70 días",
                 "Reestriado recomendado cada 8.000 h, acumula 7.650 h",
                 "Vibración detectada hace 18 días en OT-2026-0010"],
                68, Prioridad.Media, 6, 940000),

            Nueva("GEA-PC-3006", "MAQ-004", OrigenRecomendacion.Regla,
                "Stock actual (3) igual al umbral mínimo (3)", 3,
                "El último preventivo consumió las tres unidades del juego completo. Sin reposición, la próxima intervención queda sin cobertura.",
                ["Stock actual: 3 de 3 mínimo", "3 unidades consumidas hace 21 días en OT-2026-0009",
                 "Plazo de reposición: 55 días", "Intervalo del catálogo: pistones cada 3.000 h"],
                95, Prioridad.Alta, 2, 2625000)
        ];
    }

    public static readonly List<ReporteVm> Reportes =
    [
        new() { Nombre = "Stock crítico consolidado — Agosto", TipoReporte = "Estado de stock",
                Parametros = "Todas las plantas · Criticidad Alta y Crítica", Periodo = "01/08 al 31/08",
                Planta = "Todas", UsuarioCreador = "Martín Sosa", FechaGeneracion = Hoy.AddDays(-2), Filas = 42,
                Historial = [ new() { Usuario = "Martín Sosa", Accion = "Generación", Fecha = Hoy.AddDays(-2), Detalle = "Reporte generado con 42 filas" },
                              new() { Usuario = "Martín Sosa", Accion = "Exportación", Fecha = Hoy.AddDays(-2), Detalle = "Exportado a PDF" } ] },
        new() { Nombre = "Frecuencia de fallas por máquina — Q3", TipoReporte = "Historial de fallas",
                Parametros = "Planta Norte · Correctivo", Periodo = "01/07 al 31/08",
                Planta = "Planta Norte", UsuarioCreador = "Diego Ferrero", FechaGeneracion = Hoy.AddDays(-5), Filas = 18,
                Historial = [ new() { Usuario = "Diego Ferrero", Accion = "Generación", Fecha = Hoy.AddDays(-5), Detalle = "Reporte generado con 18 filas" },
                              new() { Usuario = "Diego Ferrero", Accion = "Modificación", Fecha = Hoy.AddDays(-4), Detalle = "Se amplió el rango al 31/08" } ] },
        new() { Nombre = "Consumo de repuestos por línea", TipoReporte = "Consumo de repuestos",
                Parametros = "Planta Oeste · Todas las líneas", Periodo = "01/06 al 31/08",
                Planta = "Planta Oeste", UsuarioCreador = "Carla Benítez", FechaGeneracion = Hoy.AddDays(-8), Filas = 27,
                Historial = [ new() { Usuario = "Carla Benítez", Accion = "Generación", Fecha = Hoy.AddDays(-8), Detalle = "Reporte generado con 27 filas" } ] },
        new() { Nombre = "Órdenes de trabajo cerradas — Julio", TipoReporte = "Órdenes de trabajo",
                Parametros = "Todas las plantas · Estado Cerrada", Periodo = "01/07 al 31/07",
                Planta = "Todas", UsuarioCreador = "Martín Sosa", FechaGeneracion = Hoy.AddDays(-13), Filas = 34,
                Historial = [ new() { Usuario = "Martín Sosa", Accion = "Generación", Fecha = Hoy.AddDays(-13), Detalle = "Reporte generado con 34 filas" },
                              new() { Usuario = "Martín Sosa", Accion = "Exportación", Fecha = Hoy.AddDays(-13), Detalle = "Exportado a Excel" } ] },
        new() { Nombre = "Tiempo medio de resolución por técnico", TipoReporte = "Órdenes de trabajo",
                Parametros = "Todas las plantas · Agrupado por técnico", Periodo = "01/05 al 31/07",
                Planta = "Todas", UsuarioCreador = "Martín Sosa", FechaGeneracion = Hoy.AddDays(-20), Filas = 9,
                Historial = [ new() { Usuario = "Martín Sosa", Accion = "Generación", Fecha = Hoy.AddDays(-20), Detalle = "Reporte generado con 9 filas" } ] },
        new() { Nombre = "Evolución de stock — Planta Sur", TipoReporte = "Estado de stock",
                Parametros = "Planta Sur · Todas las criticidades", Periodo = "01/04 al 30/06",
                Planta = "Planta Sur", UsuarioCreador = "Rubén Quiroga", Estado = EstadoGenerico.Inactivo,
                FechaGeneracion = Hoy.AddDays(-40), Filas = 15,
                Historial = [ new() { Usuario = "Rubén Quiroga", Accion = "Generación", Fecha = Hoy.AddDays(-40), Detalle = "Reporte generado con 15 filas" },
                              new() { Usuario = "Laura Giménez", Accion = "Eliminación lógica", Fecha = Hoy.AddDays(-30), Detalle = "Reporte marcado como eliminado por duplicado" } ] }
    ];

    public static readonly List<PlanVm> Planes =
    [
        new() { Nombre = "Básico", MaxMaquinas = 15, Precio = 180000,
                Descripcion = "Una planta, hasta 15 máquinas y recomendaciones por reglas de negocio.", EmpresasActivas = 2 },
        new() { Nombre = "Profesional", MaxMaquinas = 50, Precio = 420000,
                Descripcion = "Hasta tres plantas, 50 máquinas, motor de recomendaciones completo y reportes exportables.", EmpresasActivas = 3 },
        new() { Nombre = "Corporativo", MaxMaquinas = 150, Precio = 890000,
                Descripcion = "Plantas ilimitadas, 150 máquinas, matriz de permisos personalizable y soporte prioritario.", EmpresasActivas = 1 },
        new() { Nombre = "Piloto", MaxMaquinas = 5, Precio = 0,
                Descripcion = "Prueba sin costo por 60 días, hasta 5 máquinas y una sola planta.", EmpresasActivas = 1 }
    ];

    public static readonly List<EmpresaVm> Empresas =
    [
        new() { RazonSocial = "Alimentos Pampa S.A.", Dominio = "alimentospampa.com.ar", TenantId = "org_pampa_7f31",
                Plan = "Profesional", MaxMaquinasHabilitadas = 50, MaquinasRegistradas = 14, UsuariosActivos = 9,
                Rubro = "Alimenticia", FechaAlta = Hoy.AddMonths(-14), AdminInicial = "laura.gimenez@alimentospampa.com.ar",
                OrdenesUltimoMes = 34, RecomendacionesProcesadas = 128 },
        new() { RazonSocial = "Aceros del Norte S.A.", Dominio = "acerosdelnorte.com.ar", TenantId = "org_acenor_2b90",
                Plan = "Corporativo", MaxMaquinasHabilitadas = 150, MaquinasRegistradas = 87, UsuariosActivos = 24,
                Rubro = "Metalúrgica", FechaAlta = Hoy.AddMonths(-9), AdminInicial = "admin@acerosdelnorte.com.ar",
                OrdenesUltimoMes = 112, RecomendacionesProcesadas = 340 },
        new() { RazonSocial = "Química Delta S.R.L.", Dominio = "quimicadelta.com.ar", TenantId = "org_qdelta_5c14",
                Plan = "Profesional", MaxMaquinasHabilitadas = 50, MaquinasRegistradas = 31, UsuariosActivos = 11,
                Rubro = "Química", FechaAlta = Hoy.AddMonths(-7), AdminInicial = "mantenimiento@quimicadelta.com.ar",
                OrdenesUltimoMes = 48, RecomendacionesProcesadas = 176 },
        new() { RazonSocial = "Plásticos Rivadavia S.A.", Dominio = "plasticosrivadavia.com", TenantId = "org_privad_9e02",
                Plan = "Básico", MaxMaquinasHabilitadas = 15, MaquinasRegistradas = 12, UsuariosActivos = 5,
                Rubro = "Plástico", FechaAlta = Hoy.AddMonths(-5), AdminInicial = "jvega@plasticosrivadavia.com",
                OrdenesUltimoMes = 19, RecomendacionesProcesadas = 61 },
        new() { RazonSocial = "Frigorífico San Andrés S.A.", Dominio = "frigosanandres.com.ar", TenantId = "org_fsandr_3a77",
                Plan = "Profesional", MaxMaquinasHabilitadas = 50, MaquinasRegistradas = 22, UsuariosActivos = 8,
                Rubro = "Alimenticia", FechaAlta = Hoy.AddMonths(-3), AdminInicial = "sistemas@frigosanandres.com.ar",
                OrdenesUltimoMes = 27, RecomendacionesProcesadas = 54 },
        new() { RazonSocial = "Textil Lanús S.R.L.", Dominio = "textillanus.com.ar", TenantId = "org_tlanus_6d45",
                Plan = "Básico", MaxMaquinasHabilitadas = 15, MaquinasRegistradas = 9, UsuariosActivos = 4,
                Rubro = "Textil", Estado = EstadoGenerico.Inactivo, FechaAlta = Hoy.AddMonths(-11),
                AdminInicial = "admin@textillanus.com.ar", OrdenesUltimoMes = 0, RecomendacionesProcesadas = 88 },
        new() { RazonSocial = "Autopartes Córdoba S.A.", Dominio = "autopartescba.com.ar", TenantId = "org_apcba_1f68",
                Plan = "Piloto", MaxMaquinasHabilitadas = 5, MaquinasRegistradas = 4, UsuariosActivos = 3,
                Rubro = "Autopartes", FechaAlta = Hoy.AddDays(-18), AdminInicial = "ncastro@autopartescba.com.ar",
                OrdenesUltimoMes = 6, RecomendacionesProcesadas = 9 }
    ];

    public static readonly List<ServicioVm> Servicios =
    [
        new() { Nombre = "Aplicación web", Tecnologia = "Azure App Service · Blazor Server", Estado = EstadoServicio.Operativo,
                LatenciaMs = 84, Disponibilidad = 99.98, UltimoIncidente = "Sin incidentes en 90 días" },
        new() { Nombre = "Base de datos relacional", Tecnologia = "Azure Database for PostgreSQL · pgvector", Estado = EstadoServicio.Operativo,
                LatenciaMs = 12, Disponibilidad = 99.99, UltimoIncidente = "Sin incidentes en 90 días" },
        new() { Nombre = "Bitácoras", Tecnologia = "MongoDB Atlas", Estado = EstadoServicio.Operativo,
                LatenciaMs = 31, Disponibilidad = 99.95, UltimoIncidente = "Latencia elevada el 02/08" },
        new() { Nombre = "Pipeline de enriquecimiento", Tecnologia = "N8N sobre Azure Container Instance", Estado = EstadoServicio.Degradado,
                LatenciaMs = 2480, Disponibilidad = 97.20, UltimoIncidente = "Timeout en cola de enriquecimiento hace 4 h" },
        new() { Nombre = "Modelo de lenguaje", Tecnologia = "Gemini API · Google Cloud", Estado = EstadoServicio.Operativo,
                LatenciaMs = 1140, Disponibilidad = 99.60, UltimoIncidente = "Cuota excedida el 28/07" },
        new() { Nombre = "Identidad", Tecnologia = "Auth0", Estado = EstadoServicio.Operativo,
                LatenciaMs = 156, Disponibilidad = 99.99, UltimoIncidente = "Sin incidentes en 90 días" },
        new() { Nombre = "Borde y seguridad", Tecnologia = "Cloudflare · DNS, SSL y WAF", Estado = EstadoServicio.Operativo,
                LatenciaMs = 22, Disponibilidad = 100.00, UltimoIncidente = "Sin incidentes en 90 días" }
    ];

    public static readonly List<EventoBitacoraVm> Bitacora = ConstruirBitacora();

    private static List<EventoBitacoraVm> ConstruirBitacora()
    {
        EventoBitacoraVm E(int horas, string usuario, string empresa, string accion, string recurso,
            string detalle, NivelLog nivel, string origen) => new()
            {
                Fecha = DateTime.Now.AddHours(-horas),
                Usuario = usuario,
                Empresa = empresa,
                Accion = accion,
                Recurso = recurso,
                Detalle = detalle,
                Nivel = nivel,
                Origen = origen
            };

        const string pampa = "Alimentos Pampa S.A.";
        const string acenor = "Aceros del Norte S.A.";
        const string qdelta = "Química Delta S.R.L.";

        return
        [
            E(1, "Carla Benítez", pampa, "Alta", "OrdenTrabajo", "OT-2026-0001 creada sobre MAQ-006 con prioridad Urgente", NivelLog.Info, "MantIA.WEB"),
            E(2, "Sistema", pampa, "Generación", "AlertaStock", "Alerta generada para AL-JNBR-20 por stock 0 sobre mínimo 2", NivelLog.Warning, "AlertaService"),
            E(3, "Motor ML", pampa, "Generación", "Recomendacion", "8 recomendaciones generadas para el tenant org_pampa_7f31", NivelLog.Info, "MotorRecomendaciones"),
            E(4, "Sistema", "—", "Excepción", "PipelineEnriquecimiento", "Timeout al consultar Gemini API tras 30 s. Ficha SEW-Eurodrive R77 DRN90 queda pendiente", NivelLog.Error, "N8N"),
            E(5, "Diego Ferrero", pampa, "Modificación", "OrdenTrabajo", "OT-2026-0002 pasó de Abierta a En curso", NivelLog.Info, "MantIA.WEB"),
            E(7, "Laura Giménez", pampa, "Modificación", "PermisoPorRolYNivel", "Se deshabilitó la acción Alta sobre Repuestos para Empleado/Jr", NivelLog.Info, "MantIA.WEB"),
            E(9, "Javier Molina", pampa, "Consulta", "Repuesto", "Consulta del listado de repuestos críticos de Planta Norte", NivelLog.Debug, "MantIA.WEB"),
            E(12, "admin@acerosdelnorte.com.ar", acenor, "Alta", "Usuario", "Usuario operativo creado con rol Empleado y nivel Jr", NivelLog.Info, "Auth0MgmtClient"),
            E(14, "Sistema", acenor, "Generación", "AlertaStock", "12 alertas de stock generadas en el cierre diario", NivelLog.Warning, "AlertaService"),
            E(18, "Martín Sosa", pampa, "Exportación", "Reporte", "Reporte de stock crítico exportado a PDF", NivelLog.Info, "ReporteService"),
            E(22, "Sistema", "—", "Excepción", "GeminiApi", "Respuesta 429 por cuota excedida. Reintento programado en 15 min", NivelLog.Warning, "N8N"),
            E(26, "Verónica Paz", pampa, "Cierre", "OrdenTrabajo", "OT-2026-0006 cerrada con 0 repuestos consumidos", NivelLog.Info, "MantIA.WEB"),
            E(30, "mantenimiento@quimicadelta.com.ar", qdelta, "Alta", "Maquina", "Reactor R-204 registrado. Pipeline de enriquecimiento encolado", NivelLog.Info, "MantIA.WEB"),
            E(34, "Sistema", qdelta, "Enriquecimiento", "CatalogoMaquina", "Ficha técnica completada para Pfaudler RA-2000 en 42 s", NivelLog.Info, "N8N"),
            E(38, "Nicolás Ayala", pampa, "Intento fallido", "Repuesto", "Acceso denegado a la acción Baja por nivel Jr", NivelLog.Warning, "AuthorizationHandler"),
            E(44, "Superadmin MantIA", "—", "Alta", "Empresa", "Autopartes Córdoba S.A. dada de alta con plan Piloto", NivelLog.Info, "MantIA.WEB"),
            E(50, "Sistema", "—", "Configuración", "ConfigSistema", "Nivel de log de bitácora de excepciones ajustado a Warning", NivelLog.Info, "PanelSistema"),
            E(58, "Diego Ferrero", pampa, "Validación", "Recomendacion", "Recomendación de FA-220 aceptada con cantidad 4", NivelLog.Info, "MantIA.WEB"),
            E(66, "Sistema", pampa, "Baja", "Usuario", "Hugo Ibarra desactivado. 0 órdenes reasignadas", NivelLog.Info, "Auth0MgmtClient"),
            E(74, "Sistema", "—", "Excepción", "MongoDB", "Latencia de escritura de 4.200 ms sobre la colección auditoria", NivelLog.Error, "MongoAuditLogger"),
            E(82, "Laura Giménez", pampa, "Alta", "Planta", "Planta Sur registrada en Av. Industrial 1500, Avellaneda", NivelLog.Info, "MantIA.WEB"),
            E(90, "Carla Benítez", pampa, "Modificación", "Repuesto", "Umbral mínimo de KR-VL-3320 modificado de 6 a 8", NivelLog.Info, "MantIA.WEB"),
            E(104, "Sistema", acenor, "Generación", "Recomendacion", "34 recomendaciones generadas para el tenant org_acenor_2b90", NivelLog.Info, "MotorRecomendaciones"),
            E(120, "Superadmin MantIA", "—", "Modificación", "Empresa", "Textil Lanús S.R.L. desactivada por baja de contrato", NivelLog.Info, "MantIA.WEB")
        ];
    }

    public static readonly string[] Recursos =
        ["Máquinas", "Repuestos", "Alertas", "Órdenes de trabajo", "Recomendaciones", "Reportes", "Usuarios", "Plantas"];

    public static readonly string[] Acciones = ["Alta", "Baja", "Modificación", "Consulta"];

    public static readonly List<PermisoVm> Permisos = ConstruirPermisos();

    private static List<PermisoVm> ConstruirPermisos()
    {
        var roles = new[] { Roles.Empleado, Roles.Supervisor, Roles.Gerente, Roles.AdminEmpresa };
        var lista = new List<PermisoVm>();

        foreach (var rol in roles)
        foreach (var nivel in new[] { "Jr", "Sr" })
        foreach (var recurso in Recursos)
        foreach (var accion in Acciones)
            lista.Add(new PermisoVm
            {
                Rol = rol,
                Nivel = nivel,
                Recurso = recurso,
                Accion = accion,
                Habilitado = PermisoPorDefecto(rol, nivel, recurso, accion)
            });

        return lista;
    }

    private static bool PermisoPorDefecto(string rol, string nivel, string recurso, string accion)
    {
        var administrativo = recurso is "Usuarios" or "Plantas";

        if (rol == Roles.AdminEmpresa)
            return nivel == "Sr" || accion != "Baja";

        if (administrativo)
            return false;

        return rol switch
        {
            Roles.Empleado => recurso switch
            {
                "Recomendaciones" => accion == "Consulta",
                "Reportes" => accion == "Consulta",
                "Máquinas" or "Repuestos" or "Órdenes de trabajo" => accion is "Consulta" or "Alta" || (nivel == "Sr" && accion == "Modificación"),
                "Alertas" => accion == "Consulta",
                _ => accion == "Consulta"
            },
            Roles.Supervisor => recurso switch
            {
                "Recomendaciones" => accion is "Consulta" or "Modificación",
                _ => accion != "Baja" || nivel == "Sr"
            },
            Roles.Gerente => accion is "Consulta"
                             || (recurso == "Reportes" && accion != "Baja")
                             || (recurso == "Reportes" && nivel == "Sr"),
            _ => false
        };
    }

    public static List<AlertaStockVm> Alertas()
    {
        return Repuestos
            .Where(r => r.Estado == EstadoGenerico.Activo && r.BajoMinimo)
            .Select(r =>
            {
                var maquina = Maquinas.FirstOrDefault(m => r.MaquinaIds.Contains(m.Id));
                var consumoMensual = ConsumoMensual(r);
                return new AlertaStockVm
                {
                    RepuestoId = r.Id,
                    Repuesto = r.Nombre,
                    NumeroParte = r.NumeroParte,
                    Maquina = maquina is null ? "—" : $"{maquina.Codigo} · {maquina.Nombre}",
                    Planta = maquina?.Planta ?? "—",
                    Criticidad = r.Criticidad,
                    StockActual = r.StockActual,
                    StockMinimo = r.StockMinimo,
                    DiasCobertura = consumoMensual <= 0 ? 999 : (int)Math.Round((double)(r.StockActual / consumoMensual) * 30),
                    Estado = EstadoAlerta.Activa,
                    FechaGeneracion = DateTime.Now.AddHours(-Math.Abs(r.NumeroParte.GetHashCode() % 72) - 1)
                };
            })
            .OrderByDescending(a => a.Criticidad)
            .ThenBy(a => a.DiasCobertura)
            .ToList();
    }

    private static decimal ConsumoMensual(RepuestoVm repuesto)
    {
        var consumos = Ordenes
            .Where(o => o.Estado == EstadoOrden.Cerrada && o.FechaCierre >= DateTime.Now.AddMonths(-6))
            .SelectMany(o => o.Repuestos)
            .Where(c => c.RepuestoId == repuesto.Id)
            .Sum(c => c.Cantidad);

        return Math.Round(consumos / 6m, 2);
    }

    public static List<HistorialFallaVm> HistorialDe(Guid maquinaId)
    {
        return Ordenes
            .Where(o => o.MaquinaId == maquinaId && o.Estado == EstadoOrden.Cerrada)
            .OrderByDescending(o => o.FechaCierre)
            .Select(o => new HistorialFallaVm
            {
                Fecha = o.FechaCierre ?? o.FechaApertura,
                Orden = o.Numero,
                TipoFalla = o.TipoFalla,
                Tipo = o.TipoMantenimiento,
                RepuestoUtilizado = o.Repuestos.Count == 0
                    ? "Sin consumo"
                    : string.Join(", ", o.Repuestos.Select(r => $"{r.Repuesto} ×{r.Cantidad:0.##}")),
                HorasResolucion = o.HorasResolucion ?? 0,
                Tecnico = o.UsuarioAsignado
            })
            .ToList();
    }

    static DatosDemo() => SincronizarPlantas();

    public static void SincronizarPlantas()
    {
        var alertas = Alertas();

        foreach (var planta in Plantas)
        {
            planta.Maquinas = Maquinas.Count(m => m.Planta == planta.Nombre && m.Estado != EstadoMaquina.Inactiva);
            planta.AlertasActivas = alertas.Count(a => a.Planta == planta.Nombre);
            planta.OrdenesAbiertas = Ordenes.Count(o => o.Planta == planta.Nombre
                                                        && o.Estado is EstadoOrden.Abierta or EstadoOrden.EnCurso);
        }
    }

    public static string SiguienteNumeroOrden() => $"OT-2026-{Ordenes.Count + 1:D4}";

    public static string SiguienteCodigoMaquina() => $"MAQ-{Maquinas.Count + 1:D3}";
}
