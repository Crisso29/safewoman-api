using System.Globalization;

namespace SafeWoman.ViewModels.Denuncia;

/// Genera el HTML del mapa interactivo estilo Uber/WhatsApp:
///  - Capa CALLE: CartoDB Voyager (tiles OSM, gratis sin key).
///  - Capa SATÉLITE: Esri World Imagery (fotos aéreas, gratis sin key) + labels overlay.
///  - Pin FIJO y GRANDE en el centro visual del mapa. El usuario arrastra el mapa;
///    al soltar (moveend) las coordenadas del centro se envían al ViewModel.
///  - Zoom controls grandes (44px, accesibles con el dedo).
///  - Botón GPS flotante estilo Google Maps.
///  - Restricción de pan al departamento de Ayacucho (maxBounds).
///  - Notifica al code-behind vía safewoman://ubicacion?lat=X&amp;lng=Y
///    y safewoman://gps para pedir posición GPS.
public static class MapaHtmlBuilder
{
    public const string ColorAnonima = "#2E7D32"; // verde
    public const string ColorFormal  = "#8B1A3A"; // rojo institucional

    /// Genera el HTML del mapa centrado en (lat, lng) con el color de marca dado.
    /// Si <paramref name="modoSatelite"/> es true, el mapa arranca directamente en la
    /// capa satelital — así el usuario no pierde su preferencia cuando el mapa se
    /// regenera (después de GPS, elección de sugerencia, etc.).
    public static string Build(double lat, double lng, string color, bool modoSatelite = false)
    {
        var latS = lat.ToString(CultureInfo.InvariantCulture);
        var lngS = lng.ToString(CultureInfo.InvariantCulture);

        return Template
            .Replace("__LAT__",       latS)
            .Replace("__LNG__",       lngS)
            .Replace("__COLOR__",     color)
            .Replace("__START_SAT__", modoSatelite ? "true" : "false");
    }

    // Zoom inicial 16 (~ nivel manzana): da suficiente contexto para orientarse
    // pero conserva detalle de calles. El usuario puede acercar con + hasta 19.
    private const string Template = """
<!DOCTYPE html><html>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1,user-scalable=no'>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'
      integrity='sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=' crossorigin=''/>
<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'
        integrity='sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo=' crossorigin=''></script>
<style>
*{box-sizing:border-box;margin:0;padding:0}
html,body,#map{width:100%;height:100vh}
body{font-family:-apple-system,system-ui,Segoe UI,Roboto,Arial,sans-serif}
.leaflet-container{background:#e8ecf1}

/* --- Zoom controls agrandados y accesibles con el dedo --- */
.leaflet-control-zoom{
  border:none !important;
  box-shadow:0 2px 8px rgba(0,0,0,.25) !important;
  border-radius:10px !important;
  overflow:hidden;
  margin:14px !important;
}
.leaflet-control-zoom a{
  width:44px !important;
  height:44px !important;
  line-height:44px !important;
  font-size:24px !important;
  font-weight:600 !important;
  color:#333 !important;
}
.leaflet-control-zoom a:hover{background:#f5f5f5 !important}
.leaflet-control-zoom-in{border-bottom:1px solid #e0e0e0 !important}

/* --- Pin GRANDE y visible en el centro absoluto --- */
#centerpin{
  position:absolute;left:50%;top:50%;
  transform:translate(-50%,-100%);
  z-index:1000;pointer-events:none;
  transition:transform 220ms cubic-bezier(.34,1.56,.64,1);
  filter:drop-shadow(0 4px 8px rgba(0,0,0,.45));
}
#centerpin.lift{
  transform:translate(-50%,-100%) translateY(-14px);
}
#pin-shadow{
  position:absolute;left:50%;top:50%;
  transform:translate(-50%,0);
  width:22px;height:8px;
  background:radial-gradient(ellipse,rgba(0,0,0,.4) 0%,rgba(0,0,0,0) 70%);
  border-radius:50%;
  z-index:999;pointer-events:none;
  transition:transform 220ms cubic-bezier(.34,1.56,.64,1),opacity 220ms;
}
#centerpin.lift ~ #pin-shadow{
  transform:translate(-50%,0) scale(.65);
  opacity:.45;
}

/* --- Tip explicativo, desaparece al mover el mapa --- */
.tip{
  position:absolute;top:16px;left:50%;transform:translateX(-50%);
  z-index:1000;background:rgba(20,20,20,.85);color:#fff;
  padding:10px 18px;border-radius:22px;font-size:12px;font-weight:500;
  pointer-events:none;
  box-shadow:0 3px 10px rgba(0,0,0,.35);
  transition:opacity 240ms;
  max-width:85%;
  text-align:center;
  white-space:nowrap;
}
.tip.hide{opacity:0}

/* --- Botón GPS flotante estilo Google Maps --- */
#gps-btn{
  position:absolute;bottom:74px;right:14px;
  width:46px;height:46px;border-radius:23px;
  background:#fff;border:none;
  box-shadow:0 2px 8px rgba(0,0,0,.25);
  z-index:1000;cursor:pointer;
  display:flex;align-items:center;justify-content:center;
  padding:0;
}
#gps-btn:active{background:#f0f0f0;transform:scale(.94)}
#gps-btn svg{width:22px;height:22px}

/* --- Botón Satélite ↔ Calle (estilo Google Maps) --- */
#layer-btn{
  position:absolute;top:14px;right:14px;
  width:46px;height:46px;border-radius:10px;
  background:#fff;border:none;
  box-shadow:0 2px 8px rgba(0,0,0,.25);
  z-index:1000;cursor:pointer;
  display:flex;flex-direction:column;align-items:center;justify-content:center;
  padding:0;font-size:9px;font-weight:600;color:#333;
  gap:1px;
}
#layer-btn:active{background:#f0f0f0;transform:scale(.94)}
#layer-btn svg{width:20px;height:20px}
#layer-btn .lbl{font-size:8.5px;letter-spacing:.3px}

/* --- Pill de coordenadas en la parte inferior --- */
.coords{
  position:absolute;bottom:16px;left:50%;transform:translateX(-50%);
  z-index:1000;background:rgba(255,255,255,.96);color:#222;
  padding:9px 18px;border-radius:14px;font-size:12px;font-weight:600;
  pointer-events:none;
  box-shadow:0 3px 10px rgba(0,0,0,.28);
  border:1px solid rgba(0,0,0,.06);
  max-width:82%;
  text-align:center;
  white-space:nowrap;
  overflow:hidden;
  text-overflow:ellipsis;
}
.coords .lbl{color:#888;font-weight:500;margin-right:6px}

@keyframes pulse{
  0%,100%{transform:translate(-50%,0) scale(1);opacity:.35}
  50%    {transform:translate(-50%,0) scale(1.25);opacity:.6}
}
</style>
</head>
<body>
<div id='map'></div>

<!-- Pin más grande (46x56) — se ve claramente incluso en zoom bajo -->
<svg id='centerpin' width='46' height='56' viewBox='0 0 46 56' xmlns='http://www.w3.org/2000/svg'>
  <path d='M23 0C10.3 0 0 10.3 0 23c0 17.3 23 33 23 33s23-15.7 23-33C46 10.3 35.7 0 23 0z'
        fill='__COLOR__' stroke='#fff' stroke-width='2.5'/>
  <circle cx='23' cy='23' r='8' fill='#fff'/>
</svg>
<div id='pin-shadow'></div>

<div class='tip' id='tip'>👆 Arrastra el mapa para colocar el pin</div>

<button id='gps-btn' aria-label='Mi ubicación GPS'>
  <svg viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'>
    <circle cx='12' cy='12' r='3' fill='__COLOR__'/>
    <circle cx='12' cy='12' r='9' stroke='__COLOR__' stroke-width='2' fill='none'/>
    <line x1='12' y1='1' x2='12' y2='4' stroke='__COLOR__' stroke-width='2' stroke-linecap='round'/>
    <line x1='12' y1='20' x2='12' y2='23' stroke='__COLOR__' stroke-width='2' stroke-linecap='round'/>
    <line x1='1' y1='12' x2='4' y2='12' stroke='__COLOR__' stroke-width='2' stroke-linecap='round'/>
    <line x1='20' y1='12' x2='23' y2='12' stroke='__COLOR__' stroke-width='2' stroke-linecap='round'/>
  </svg>
</button>

<!-- Botón para alternar entre vista calle y vista satélite estilo Google Earth -->
<button id='layer-btn' aria-label='Cambiar entre calle y satélite'>
  <svg id='layer-icon' viewBox='0 0 24 24' fill='none' xmlns='http://www.w3.org/2000/svg'>
    <path d='M12 2L2 8l10 6 10-6-10-6z' stroke='__COLOR__' stroke-width='1.8' stroke-linejoin='round' fill='none'/>
    <path d='M2 16l10 6 10-6' stroke='__COLOR__' stroke-width='1.8' stroke-linejoin='round' fill='none'/>
  </svg>
  <span class='lbl' id='layer-lbl'>Satélite</span>
</button>

<div class='coords' id='coords'><span class='lbl'>📍</span><span id='coords-txt'>—</span></div>

<script>
(function(){
'use strict';
var LAT=__LAT__, LNG=__LNG__;

// Bounding box del departamento de Ayacucho — restringe el pan del mapa.
var ayacuchoBounds = L.latLngBounds([[-15.6, -75.2], [-12.3, -73.0]]);

var map = L.map('map', {
    zoomControl: true,
    attributionControl: false,
    tap: true,
    touchZoom: true,
    bounceAtZoomLimits: false,
    zoomSnap: 0.5,        // zoom fraccional — más suave en WebView Android
    zoomDelta: 0.5,       // botones + / − avanzan de 0.5 en 0.5
    maxBounds: ayacuchoBounds,
    maxBoundsViscosity: 1.0,
    minZoom: 9
}).setView([LAT, LNG], 16);   // zoom 16 da contexto de barrio con calles legibles

// Capa CALLE — CartoDB Voyager (tiles OSM con estilo pulido, gratis sin key).
var capaCalle = L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}.png', {
    maxZoom: 19,
    subdomains: ['a','b','c','d'],
    bounds: ayacuchoBounds
});

// Capa SATÉLITE — Esri World Imagery (fotos satelitales de Maxar/DigitalGlobe/USDA/NASA,
// misma fuente que Google Earth). Gratis sin key ni registro para uso no comercial.
var capaSatelite = L.tileLayer(
    'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
    maxZoom: 19,
    bounds: ayacuchoBounds
});

// Etiquetas encima del satélite (nombres de calles/pueblos) — para no perderse.
var etiquetasEncima = L.tileLayer(
    'https://{s}.basemaps.cartocdn.com/rastertiles/voyager_only_labels/{z}/{x}/{y}.png', {
    maxZoom: 19,
    subdomains: ['a','b','c','d'],
    bounds: ayacuchoBounds
});

var enSatelite = __START_SAT__;
if (enSatelite){
    capaSatelite.addTo(map);
    etiquetasEncima.addTo(map);
} else {
    capaCalle.addTo(map);
}
map.zoomControl.setPosition('topleft');

// Fallback si el mapa se sale del bbox (rebote).
map.on('drag', function(){ map.panInsideBounds(ayacuchoBounds, { animate: false }); });

var pinEl    = document.getElementById('centerpin');
var coordsEl = document.getElementById('coords-txt');
var tipEl    = document.getElementById('tip');
var gpsBtn   = document.getElementById('gps-btn');
var debounceId = null;

function updateCoordsLabel(la, ln){
    coordsEl.textContent = la.toFixed(6) + ', ' + ln.toFixed(6);
}
updateCoordsLabel(LAT, LNG);

function notificar(la, ln){
    window.location.href = 'safewoman://ubicacion?lat=' + la.toFixed(6) + '&lng=' + ln.toFixed(6);
}

map.on('movestart', function(){
    pinEl.classList.add('lift');
    tipEl.classList.add('hide');   // esconde el tip cuando el usuario ya interactúa
});

map.on('moveend', function(){
    pinEl.classList.remove('lift');
    var c = map.getCenter();
    updateCoordsLabel(c.lat, c.lng);
    if (debounceId) clearTimeout(debounceId);
    debounceId = setTimeout(function(){ notificar(c.lat, c.lng); }, 350);
});

map.on('move', function(){
    var c = map.getCenter();
    updateCoordsLabel(c.lat, c.lng);
});

// Al tocar/click sobre un punto del mapa, el pin fijo va a ese lugar
// animando el pan. Al terminar (moveend) se notifica al ViewModel automáticamente.
map.on('click', function(e){
    tipEl.classList.add('hide');
    map.panTo(e.latlng, { animate: true, duration: 0.3 });
});

// Click en el botón GPS → notifica al code-behind, que obtiene el GPS y regenera el mapa.
gpsBtn.addEventListener('click', function(){
    window.location.href = 'safewoman://gps';
});

// Toggle Calle ↔ Satélite (con etiquetas de nombres encima del satélite).
var layerBtn = document.getElementById('layer-btn');
var layerLbl = document.getElementById('layer-lbl');
// Etiqueta inicial coincide con el modo cargado
layerLbl.textContent = enSatelite ? 'Calle' : 'Satélite';
layerBtn.addEventListener('click', function(){
    if (enSatelite){
        // Agregar la nueva capa ANTES de remover la anterior para evitar flash gris.
        capaCalle.addTo(map);
        map.removeLayer(capaSatelite);
        map.removeLayer(etiquetasEncima);
        layerLbl.textContent = 'Satélite';
    } else {
        capaSatelite.addTo(map);
        etiquetasEncima.addTo(map);
        map.removeLayer(capaCalle);
        layerLbl.textContent = 'Calle';
    }
    enSatelite = !enSatelite;
    // Notificamos al code-behind el modo actual para conservarlo si el mapa se regenera.
    window.location.href = 'safewoman://modo?sat=' + (enSatelite ? '1' : '0');
});

setTimeout(function(){ map.invalidateSize(); }, 250);

// Ocultar el tip después de 5 segundos aunque el usuario no toque.
setTimeout(function(){ tipEl.classList.add('hide'); }, 5000);
})();
</script>
</body></html>
""";
}
