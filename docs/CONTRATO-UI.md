# Contrato de UI — MantIA

> Reglas con las que se construyeron las 38 pantallas actuales.
> Toda pantalla nueva las respeta, para que el producto se siga viendo como uno solo.

Reglas obligatorias para toda pantalla nueva. No las reinterpretes.

## Contexto

- Blazor Server, .NET 10, **MudBlazor 9.8.0**.
- El proyecto es `src/MantIA.WEB`. Las páginas van en `Components/Pages/<Modulo>/`.
- `Routes.razor` ya aplica `MainLayout` por defecto y `App.razor` ya declara
  `<Routes @rendermode="InteractiveServer" />`. **Nunca agregues `@rendermode` a una página.**
- `_Imports.razor` ya tiene: `MudBlazor`, `MantIA.WEB.Demo`, `MantIA.WEB.Components.Shared`,
  `MantIA.WEB.Theme`, `MantIA.WEB.Components.Layout`. No repitas esos `@using`.
- No hay backend todavia. Todo sale de `MantIA.WEB/Demo/DatosDemo.cs`, que se **inyecta**
  (`@inject DatosDemo DatosDemo`, registrado como `AddScoped`): una copia por sesion.
  **No uses miembros `static` en los bloques `@code`**: no pueden leer un servicio inyectado.
- **Leé antes de escribir**: `Demo/Models.cs`, `Demo/DatosDemo.cs`, `Demo/Ui.cs`,
  `Components/Shared/*.razor` y `Components/Pages/Operacion/DashboardOperativo.razor`
  (esa página es la referencia de estilo).

## Restricciones de API de MudBlazor que tenés que respetar

- `MudChip` es genérico: siempre `<MudChip T="string">`. Preferí el wrapper `<StatusChip>`.
- `MudCheckBox` y `MudSwitch` son genéricos: `<MudCheckBox T="bool" @bind-Value="x">`.
- `MudSelect` siempre con `T="..."` explícito, y sus `MudSelectItem` también con `T`.
- En diálogos, el parámetro en cascada es `[CascadingParameter] private IMudDialogInstance Dialogo { get; set; } = null!;`
- Diálogos: `<MudDialog>` con `<TitleContent>`, `<DialogContent>`, `<DialogActions>`.
  Se abren con `await DialogService.ShowAsync<MiDialogo>("Título", parametros, opciones);`
- Gráficos: sólo `MudChart`, y en la 9.8.0 **es genérico**. Verificado contra el compilador:
  siempre `<MudChart T="double" ChartType="ChartType.Bar" ...>` y las series son
  `List<ChartSeries<double>>` con `new ChartSeries<double> { Name = "...", Data = arrayDeDouble }`.
  Escribir `<MudChart ChartType=...>` o `List<ChartSeries>` **no compila**
  (errores RZ10001 y CS0305). Tipos válidos: `ChartType.Bar`, `Line`, `Donut`, `Pie`.
  Se usan además `XAxisLabels` (`string[]`), `InputData` (`double[]`), `InputLabels` (`string[]`)
  y `ChartOptions` con `ChartPalette`. No uses ninguna librería de gráficos externa.
- No uses `MudDataGrid`. Usá `MudTable`.
- No uses `MudTimeline`, `MudTreeView`, `MudCarousel`, `MudDropZone` ni componentes exóticos.
- No agregues paquetes NuGet.

## Componentes propios disponibles (ya escritos, no los reescribas)

| Componente | Parámetros principales |
|---|---|
| `PageHeader` | `Titulo`, `Bajada`, `Icono`, `Migas` (`List<BreadcrumbItem>`), fragmentos `Acciones` y `EtiquetaExtra` |
| `SectionCard` | `Titulo`, `Bajada`, `SinPadding`, `Class`, fragmentos `Acciones` y `ChildContent` |
| `MetricCard` | `Etiqueta`, `Valor`, `Unidad`, `Detalle`, `Icono`, `Acento` (`Color`), `Href`, `ContenidoExtra` |
| `StatusChip` | `Texto`, `ChipColor`, `Icono`, `Tamanio`, `Variante`, `Class` |
| `EmptyState` | `Titulo`, `Descripcion`, `Icono`, fragmento `Accion` |
| `FilterBar` | sólo `ChildContent` (contenedor con estilo) |
| `DataPoint` | `Etiqueta`, `Valor` o `ChildContent` |
| `CoverageBar` | `Actual`, `Minimo`, `Unidad` |
| `ConfirmDialog` | `Mensaje`, `Detalle`, `TextoConfirmar`, `ColorAccion`, `Icono` |
| `MapaPlantas` | `Plantas`, `@bind-PlantaSeleccionada` |
| `RecomendacionCard` | `Recomendacion`, `MostrarAcciones`, `AlAceptar`, `AlRechazar` |

Helpers de formato y color en `Ui`: `Ui.ColorDe(...)` y `Ui.TextoDe(...)` sobrecargados para cada enum,
`Ui.IconoDe(...)`, `Ui.Moneda(decimal)`, `Ui.Numero(decimal)`, `Ui.Fecha(...)`, `Ui.FechaHora(...)`,
`Ui.Relativo(DateTime)`.

## Clases CSS propias disponibles

`mantia-tabla`, `mantia-celda-principal`, `mantia-superficie`, `mantia-filtros`,
`mantia-filtros__buscador`, `mantia-filtros__campo`, `mantia-lista-datos`, `mantia-dato`,
`mantia-dato__label`, `mantia-dato__valor`, `mantia-reco__bloque`, `mantia-reco__bloque-titulo`,
`mantia-timeline-item`, `mantia-scroll-x`.

## Patrón obligatorio de pantalla de listado

1. `<PageTitle>Nombre · MantIA</PageTitle>`
2. `<PageHeader>` con `Migas`, `Bajada` explicativa y botón de alta en `Acciones`.
3. `<FilterBar>` con un `MudTextField` de búsqueda (`Immediate="true"`, `Adornment.Start` con
   `Icons.Material.Filled.Search`, clase `mantia-filtros__buscador`), los `MudSelect` de filtro
   (clase `mantia-filtros__campo`, `Margin.Dense`, `Variant.Outlined`, siempre con una opción
   "Todos/Todas"), un `MudSpacer` y un `MudText Typo="Typo.body2"` con el conteo de resultados.
4. Tabla:
   `<MudTable Items="@Filtrados" Dense="true" Hover="true" Elevation="0" Class="mantia-tabla"
   Breakpoint="Breakpoint.Sm" Striped="false">` con `<HeaderContent>` de `MudTh` y `<RowTemplate>`
   de `MudTd` **siempre con `DataLabel`** (es lo que hace que funcione en pantalla chica).
   Si la fila navega a un detalle, usá `OnRowClick` con
   `@((TableRowClickEventArgs<TipoVm> e) => Abrir(e.Item))` y `RowStyle="cursor:pointer"`.
   Agregá `<PagerContent><MudTablePager /></PagerContent>` cuando la lista pueda superar 10 filas.
5. Si `Filtrados` está vacío, mostrá `<EmptyState>` en lugar de la tabla.

## Patrón obligatorio de formulario

- `MudTextField`, `MudSelect`, `MudNumericField`, `MudDatePicker` con `Variant="Variant.Outlined"`.
- Agrupá los campos en uno o más `SectionCard` dentro de un `MudGrid` de 12 columnas
  (`MudItem xs="12" md="6"` para campos cortos).
- Barra de acciones al pie: `Cancelar` (`Variant.Text`, vuelve atrás con `Nav.NavigateTo`) y
  `Guardar` (`Variant.Filled`, `Color.Primary`).
- Al guardar: **mutá de verdad la lista estática de `DatosDemo`** (`Add` o modificar la instancia),
  mostrá `Snackbar.Add("mensaje", Severity.Success)` y navegá al listado o al detalle.
  Esto es lo que hace que el panel principal se actualice: no lo omitas.
- Marcá los campos obligatorios con `Required="true"` y `RequiredError="..."`.
- Los formularios de alta y de edición son **la misma página** con dos `@page`, distinguidos por
  un parámetro `[Parameter] public Guid? Id { get; set; }`.

## Reglas de contenido y tono

- Todo en español rioplatense, profesional, sin tutear de más y sin signos de exclamación.
- Nada de texto de relleno tipo "Lorem" ni "Ejemplo 1". Los datos salen del dominio real:
  mantenimiento industrial, repuestos críticos, órdenes de trabajo, plantas de GBA/CABA.
- Los rótulos son explícitos y en palabras, no siglas sueltas. Los iconos siempre acompañados
  de texto cuando puedan ser ambiguos.
- La aplicación tiene que poder usarla tanto un ingeniero de 70 años como un operario de 18:
  tipografía legible, jerarquía clara, botones grandes, pocas decisiones por pantalla,
  nada escondido detrás de un hover.

## Prohibido

- Comentarios que expliquen código evidente.
- Cualquier mención a Claude, Anthropic, IA generativa, "generado por", `Co-Authored-By`,
  o cualquier marca de que el código fue escrito por un agente.
- Bloques `@code` con lógica de negocio real: esto es UI con datos de ejemplo.
- Crear repositorios, interfaces, servicios inyectados o capas de abstracción para los mocks.
- Tocar `Program.cs`, `Routes.razor`, el csproj, ni nada de `MantIA.BE`, `MantIA.BLL`,
  `MantIA.DAL` o `MantIA.API`.
