// Mapa de plantas industriales sobre Leaflet + OpenStreetMap.
//
// Leaflet se sirve desde wwwroot/lib/leaflet, no desde un CDN: la aplicacion no
// depende de una red externa para tener el codigo del mapa. Lo unico que viaja
// por internet son los tiles (las imagenes del mapa).

const RECURSOS = {
    js: '/lib/leaflet/leaflet.js',
    css: '/lib/leaflet/leaflet.css'
};

// Basemaps de CARTO sobre datos de OpenStreetMap. Son mucho mas sobrios que el
// mapa estandar de OSM: sin colores de comercios ni etiquetas de POI, que en un
// panel industrial solo hacen ruido.
const TILES = {
    claro: {
        url: 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png',
        atribucion: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>'
    },
    oscuro: {
        url: 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',
        atribucion: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>'
    }
};

const mapas = new Map();
let cargaLeaflet = null;

function cargarLeaflet() {
    if (window.L) return Promise.resolve(window.L);
    if (cargaLeaflet) return cargaLeaflet;

    cargaLeaflet = new Promise((resolver, rechazar) => {
        if (!document.querySelector(`link[href="${RECURSOS.css}"]`)) {
            const hoja = document.createElement('link');
            hoja.rel = 'stylesheet';
            hoja.href = RECURSOS.css;
            document.head.appendChild(hoja);
        }
        const script = document.createElement('script');
        script.src = RECURSOS.js;
        script.onload = () => resolver(window.L);
        script.onerror = () => rechazar(new Error('No se pudo cargar Leaflet'));
        document.head.appendChild(script);
    });

    return cargaLeaflet;
}

// El tema lo decide MudBlazor en tiempo de ejecucion. En lugar de que el
// componente tenga que enterarse, se lee la luminancia del color de fondo que
// publica el propio theme provider como variable CSS.
function esOscuro() {
    const valor = getComputedStyle(document.documentElement)
        .getPropertyValue('--mud-palette-background')
        .trim();
    if (!valor) return false;

    const rgb = valor.startsWith('#')
        ? [1, 3, 5].map(i => parseInt(valor.substr(i, 2), 16))
        : (valor.match(/\d+(\.\d+)?/g) || []).slice(0, 3).map(Number);

    if (rgb.length < 3 || rgb.some(isNaN)) return false;
    const luminancia = (0.2126 * rgb[0] + 0.7152 * rgb[1] + 0.0722 * rgb[2]) / 255;
    return luminancia < 0.5;
}

function icono(L, planta) {
    const html = `
        <div class="mantia-pin mantia-pin--${planta.severidad}${planta.seleccionada ? ' mantia-pin--activo' : ''}">
            <span class="mantia-pin__rotulo">${escapar(planta.nombre)}</span>
            <span class="mantia-pin__marca">
                <svg viewBox="0 0 24 24" width="19" height="19" aria-hidden="true">
                    <path fill="currentColor" d="M2 20V10.5l5.5 3.2V10.5l5.5 3.2V10.5l5.5 3.2V4h3v16H2Z"/>
                </svg>
            </span>
            <span class="mantia-pin__punta"></span>
        </div>`;

    return L.divIcon({
        html,
        className: 'mantia-pin__contenedor',
        iconSize: [40, 52],
        iconAnchor: [20, 52],
        popupAnchor: [0, -50]
    });
}

function escapar(texto) {
    const div = document.createElement('div');
    div.textContent = texto ?? '';
    return div.innerHTML;
}

function globo(planta) {
    return `
        <div class="mantia-globo">
            <p class="mantia-globo__titulo">${escapar(planta.nombre)}</p>
            <p class="mantia-globo__lugar">${escapar(planta.localidad)}</p>
            <dl class="mantia-globo__datos">
                <div><dt>Máquinas</dt><dd>${planta.maquinas}</dd></div>
                <div><dt>Alertas</dt><dd class="mantia-globo__${planta.severidad}">${planta.alertas}</dd></div>
                <div><dt>Órdenes</dt><dd>${planta.ordenes}</dd></div>
            </dl>
        </div>`;
}

export async function crear(elemento, plantas, referencia) {
    const L = await cargarLeaflet();
    destruir(elemento);

    const mapa = L.map(elemento, {
        zoomControl: true,
        scrollWheelZoom: false,   // que la rueda no secuestre el scroll de la pagina
        attributionControl: true
    });

    const tema = esOscuro() ? 'oscuro' : 'claro';
    const capa = L.tileLayer(TILES[tema].url, {
        attribution: TILES[tema].atribucion,
        maxZoom: 19,
        detectRetina: true
    }).addTo(mapa);

    const estado = { L, mapa, capa, tema, marcadores: [], referencia };
    mapas.set(elemento, estado);

    dibujar(estado, plantas);
    return true;
}

function dibujar(estado, plantas) {
    const { L, mapa } = estado;

    estado.marcadores.forEach(m => mapa.removeLayer(m));
    estado.marcadores = [];

    if (!plantas || plantas.length === 0) {
        mapa.setView([-34.6037, -58.3816], 10);   // Buenos Aires
        return;
    }

    plantas.forEach(planta => {
        const marcador = L.marker([planta.latitud, planta.longitud], {
            icon: icono(L, planta),
            title: planta.nombre,
            riseOnHover: true,
            keyboard: true,
            alt: `Planta ${planta.nombre}`
        }).addTo(mapa);

        marcador.bindPopup(globo(planta), { closeButton: true, className: 'mantia-globo__caja' });
        marcador.on('click', () => {
            if (estado.referencia) estado.referencia.invokeMethodAsync('SeleccionarDesdeMapa', planta.id);
        });

        estado.marcadores.push(marcador);
    });

    const limites = L.latLngBounds(plantas.map(p => [p.latitud, p.longitud]));
    if (plantas.length === 1) {
        mapa.setView(limites.getCenter(), 13);
    } else {
        mapa.fitBounds(limites, { padding: [60, 60], maxZoom: 12 });
    }
}

export async function actualizar(elemento, plantas) {
    const estado = mapas.get(elemento);
    if (!estado) return false;

    // Si cambio el tema de la aplicacion, se cambia el basemap.
    const tema = esOscuro() ? 'oscuro' : 'claro';
    if (tema !== estado.tema) {
        estado.mapa.removeLayer(estado.capa);
        estado.capa = estado.L.tileLayer(TILES[tema].url, {
            attribution: TILES[tema].atribucion,
            maxZoom: 19,
            detectRetina: true
        }).addTo(estado.mapa);
        estado.tema = tema;
    }

    dibujar(estado, plantas);
    return true;
}

export function invalidar(elemento) {
    const estado = mapas.get(elemento);
    if (estado) estado.mapa.invalidateSize();
}

export function destruir(elemento) {
    const estado = mapas.get(elemento);
    if (!estado) return;
    estado.mapa.remove();
    mapas.delete(elemento);
}
