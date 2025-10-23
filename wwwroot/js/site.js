// SITE.JS Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// para detalhes sobre bundling/minification de assets estáticos.
//
// Refatorado: modularizado, com debounce, menor repetição de queries DOM,
// tratamento de erros mais explícito e comentários em pt-BR.

document.addEventListener('DOMContentLoaded', () => {
    // --- Configurações iniciais ---
    const MAP_ID = 'map';
    const MAP_EL = document.getElementById(MAP_ID);
    if (!MAP_EL || typeof L === 'undefined') return;

    const DEFAULT_CENTER = [-21.6036, -48.3640]; // Matão, SP
    const DEFAULT_ZOOM = 13;
    let map = null;
    let userPos = null;

    // Estado compartilhado
    let markers = [];
    let allFoods = [];

    // --- Helpers ---
    const normalize = s => (s || '').toString().trim().toLowerCase();

    function debounce(fn, wait = 200) {
        let t = null;
        return (...args) => {
            clearTimeout(t);
            t = setTimeout(() => fn(...args), wait);
        };
    }

    // --- Mapa e camadas ---
    function createLeafIcon() {
        const html = `
          <div style="
              width:32px;height:32px;border-radius:50%;
              background:#007C46;border:2px solid #ffffff;
              display:flex;align-items:center;justify-content:center;
              box-shadow:0 2px 8px rgba(0,0,0,.25);
          ">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="#ffffff" aria-hidden="true">
              <path d="M2 12c6-8 14-8 20-8-2 10-8 14-14 14-3 0-6-2-6-6zM8 14c2 0 4-1 6-3" stroke="#ffffff" stroke-width="2" fill="none" stroke-linecap="round"/>
            </svg>
          </div>`;
        return L.divIcon({
            className: 'custom-div-icon',
            html,
            iconSize: [32, 32],
            iconAnchor: [16, 32],
            popupAnchor: [0, -28]
        });
    }

    function initMap() {
        map = L.map(MAP_ID, { zoomControl: true, attributionControl: true })
            .setView(DEFAULT_CENTER, DEFAULT_ZOOM);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        // tentar geolocalizar o usuário (não bloqueia se falhar)
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                pos => {
                    userPos = [pos.coords.latitude, pos.coords.longitude];
                    map.setView(userPos, 14);
                    L.marker(userPos).addTo(map).bindPopup('Você está aqui');
                },
                () => {
                    // Silent fallback: mantém centro padrão, opcional alerta original removido por UX
                    map.setView(DEFAULT_CENTER, DEFAULT_ZOOM);
                },
                { enableHighAccuracy: true, timeout: 5000, maximumAge: 300000 }
            );
        }

        return map;
    }

    // --- Navbar próximo ao mapa (efeito visual) ---
    (function observeNavbarProximity() {
        const path = window.location.pathname.toLowerCase();
        const isHome = path === '/' || path === '/home' || path === '/home/index';
        const headerEl = document.querySelector('header');
        const navbar = document.querySelector('.navbar');
        if (!headerEl || !navbar || !isHome) return;

        if (!document.getElementById('near-map-navbar-styles')) {
            const style = document.createElement('style');
            style.id = 'near-map-navbar-styles';
            style.textContent = `
                header.near-map,
                header.near-map nav,
                .navbar.near-map,
                .navbar.near-map .container,
                .navbar.near-map .container-fluid {
                    background-color: rgba(206,224,216,0.85) !important;
                    backdrop-filter: blur(6px) saturate(1.05);
                    -webkit-backdrop-filter: blur(6px) saturate(1.05);
                    transition: background-color .22s ease, backdrop-filter .22s ease;
                    background-image: none !important;
                }
                header.near-map *[class*="bg-"],
                .navbar.near-map *[class*="bg-"] {
                    background-color: transparent !important;
                    background-image: none !important;
                }
                header.near-map .nav-link,
                header.near-map .navbar-brand,
                header.near-map .navbar-text,
                .navbar.near-map .nav-link,
                .navbar.near-map .navbar-brand {
                    color: rgba(11,90,47,0.95) !important;
                }
                header.near-map .navbar-brand img,
                .navbar.near-map .navbar-brand img {
                    filter: invert(1) !important;
                }
                header.near-map { box-shadow: 0 1px 0 rgba(0,0,0,0.06) !important; border-bottom: 1px solid rgba(11,90,47,0.06) !important; }
                header.near-map .navbar-toggler,
                .navbar.near-map .navbar-toggler { border-color: rgba(11,90,47,0.12) !important; }
                header.near-map .navbar-toggler-icon,
                .navbar.near-map .navbar-toggler-icon { filter: none !important; }
            `;
            document.head.appendChild(style);
        }

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                const add = entry.isIntersecting;
                headerEl.classList.toggle('near-map', add);
                navbar.classList.toggle('near-map', add);
            });
        }, { root: null, threshold: 0, rootMargin: '-25% 0px -35% 0px' });

        observer.observe(MAP_EL);
    })();

    // --- Popup centralizado ---
    function centerActivePopup() {
        const popup = document.querySelector('.leaflet-popup');
        if (!popup || !map) return;

        const mapRect = map.getContainer().getBoundingClientRect();
        const popupRect = popup.getBoundingClientRect();

        const mapCenterX = mapRect.left + mapRect.width / 2;
        const mapCenterY = mapRect.top + mapRect.height / 2;
        const popupCenterX = popupRect.left + popupRect.width / 2;
        const popupCenterY = popupRect.top + popupRect.height / 2;

        const dx = popupCenterX - mapCenterX;
        const dy = popupCenterY - mapCenterY;

        map.panBy([dx, dy], { animate: true, duration: 0.4 });
    }

    // quando um popup abre: recentra
    // usa setTimeout(0) para garantir que DOM do popup já esteja renderizado
    function attachPopupAutoCenter() {
        if (!map) return;
        map.on('popupopen', () => setTimeout(centerActivePopup, 0));
    }

    // --- HTML do popup de uma horta ---
    function popupHtml(g) {
        const dest = encodeURIComponent(`${g.lat},${g.lng}`);
        const origin = userPos ? `&origin=${encodeURIComponent(userPos[0] + ',' + userPos[1])}` : '';
        const routeUrl = `https://www.google.com/maps/dir/?api=1&destination=${dest}${origin}&travelmode=driving`;
        const foodsText = g.foods?.length ? `Alimentos: ${g.foods.join(', ')}` : '';

        return `
            <div class="p-0" style="
                background:#ffffff;
                color:#1f1f1f;
                min-width: 320px;
                max-width: 460px;
                border-radius:12px;
                box-shadow:0 4px 12px rgba(0,0,0,0.15);
                overflow:hidden;
                font-family: 'Segoe UI', sans-serif;
            ">
                <div style="background:#007C46;padding:12px 16px;">
                    <strong class="fs-5 text-white" style="font-size:1.1rem;">${g.name}</strong>
                    ${foodsText ? `<div class="small opacity-75 text-white mt-1">${foodsText}</div>` : ''}
                </div>
                <div style="padding:14px;">
                    <div class="d-flex gap-2 flex-wrap">
                        <a href="${routeUrl}" target="_blank" rel="noopener"
                           class="btn btn-sm btn-success fw-semibold rounded-pill shadow-sm px-3">
                            <i class="bi bi-geo-alt me-1"></i> Como chegar
                        </a>
                        <button type="button"
                                class="btn btn-sm btn-outline-success fw-semibold rounded-pill horta-info px-3"
                                data-id="${g.id}">
                            <i class="bi bi-info-circle me-1"></i> Mais informações
                        </button>
                    </div>
                    <div class="horta-details mt-3 d-none"></div>
                </div>
            </div>
        `;
    }

    // --- Renderização e filtro de marcadores ---
    function clearMarkers() {
        markers.forEach(m => { if (map.hasLayer(m)) map.removeLayer(m); });
        markers = [];
    }

    function renderMarkers(gardens, leafIcon) {
        clearMarkers();

        allFoods = [...new Set(gardens.flatMap(g => (g.foods || [])))];

        gardens.forEach(g => {
            const m = L.marker([g.lat, g.lng], { icon: leafIcon }).addTo(map);
            m.bindPopup(popupHtml(g));
            m._foods = g.foods || [];
            markers.push(m);
        });

        const resultsEl = document.getElementById('resultsCount');
        const resultsContainer = document.getElementById('searchResults');
        if (resultsEl && resultsContainer) {
            resultsEl.textContent = String(markers.length);
            resultsContainer.classList.toggle('d-none', false);
        }
    }

    function filterMarkers(term) {
        const t = normalize(term);
        let visible = 0;
        markers.forEach(m => {
            const match = !t || m._foods.some(f => normalize(f).includes(t));
            if (match) {
                if (!map.hasLayer(m)) m.addTo(map);
                visible++;
            } else {
                if (map.hasLayer(m)) map.removeLayer(m);
            }
        });

        const resultsEl = document.getElementById('resultsCount');
        const resultsContainer = document.getElementById('searchResults');
        if (resultsEl && resultsContainer) {
            resultsEl.textContent = String(visible);
            resultsContainer.classList.toggle('d-none', false);
        }

        const noResultsEl = document.getElementById('noResults');
        const suggestionsEl = document.getElementById('suggestions');
        const hasNo = visible === 0 && t;
        if (noResultsEl && suggestionsEl) {
            noResultsEl.classList.toggle('d-none', !hasNo);
            if (hasNo) suggestionsEl.textContent = allFoods.slice(0, 5).join(', ');
        }
    }

    // --- Input de busca com debounce ---
    function initSearchInput() {
        const input = document.getElementById('searchInput');
        if (!input) return;
        input.addEventListener('input', debounce(e => filterMarkers(e.target.value), 180));
    }

    // --- Mais informações no popup (expansão inline) ---
    function initPopupDetails() {
        // Delegation no elemento do mapa (captura cliques nos botões com classe .horta-info)
        MAP_EL.addEventListener('click', async (e) => {
            const btn = e.target.closest('.horta-info');
            if (!btn) return;

            const popupContent = btn.closest('.leaflet-popup-content');
            if (!popupContent) return;

            const container = popupContent.querySelector('.horta-details');
            if (!container) return;

            const isVisible = !container.classList.contains('d-none');
            if (isVisible) {
                container.classList.add('d-none');
                container.innerHTML = '';
                btn.innerHTML = `<i class="bi bi-info-circle me-1"></i> Mais informações`;
                setTimeout(centerActivePopup, 0);
                return;
            }

            const id = btn.getAttribute('data-id');
            if (!id) return;

            btn.disabled = true;
            const oldHtml = btn.innerHTML;
            btn.innerHTML = `<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Carregando...`;

            try {
                const res = await fetch(`/api/hortas/${id}`, { headers: { 'Accept': 'application/json' } });
                if (!res.ok) throw new Error('Falha ao carregar detalhes');
                const h = await res.json();

                const produtos = (h.produtos || '')
                    .split(',')
                    .map(p => p.trim())
                    .filter(Boolean)
                    .join(', ');

                container.innerHTML = `
                    <div class="p-3 rounded" style="background:#F8FAF9;color:#333;">
                        ${h.foto ? `<img src="${h.foto}" alt="${h.nome}" class="img-fluid rounded mb-2 shadow-sm horta-photo" />` : ''}
                        <div class="row g-2">
                            <div class="col-12">
                                <div class="small text-muted">Descrição</div>
                                <div>${h.descricao ?? 'N/D'}</div>
                            </div>
                            <div class="col-6">
                                <div class="small text-muted">Localização</div>
                                <div>${(typeof h.latitude === 'number' ? h.latitude.toFixed(5) : 'N/D')}, ${(typeof h.longitude === 'number' ? h.longitude.toFixed(5) : 'N/D')}</div>
                            </div>
                            <div class="col-6">
                                <div class="small text-muted">Telefone</div>
                                <div>${h.telefone ? `<a href="tel:${h.telefone}" class="link-success text-decoration-none">${h.telefone}</a>` : 'N/D'}</div>
                            </div>
                            <div class="col-12">
                                <div class="small text-muted">Produtos</div>
                                <div>${produtos || 'N/D'}</div>
                            </div>
                            <div class="col-12">
                                <div class="small text-muted">Dono</div>
                                <div>${h.dono ?? 'N/D'}</div>
                            </div>
                            <div class="col-12">
                                <div class="small text-muted">Criado em</div>
                                <div>${h.criadoEm ? new Date(h.criadoEm).toLocaleString() : 'N/D'}</div>
                            </div>
                        </div>
                    </div>
                `;
                container.classList.remove('d-none');
                btn.innerHTML = `<i class="bi bi-chevron-up me-1"></i> Menos informações`;

                setTimeout(centerActivePopup, 0);

                // garante recenter depois que imagens carregarem
                container.querySelectorAll('img.horta-photo').forEach(img => {
                    img.addEventListener('load', () => setTimeout(centerActivePopup, 0), { once: true });
                });
            } catch (err) {
                container.innerHTML = `<div class="alert alert-warning mb-0">Não foi possível carregar as informações da horta.</div>`;
                container.classList.remove('d-none');
                btn.innerHTML = oldHtml;
                setTimeout(centerActivePopup, 0);
            } finally {
                btn.disabled = false;
            }
        });
    }

    // --- Carregar hortas do backend (com fallback) ---
    async function loadGardens(leafIcon) {
        try {
            const res = await fetch('/api/hortas', { headers: { 'Accept': 'application/json' } });
            if (!res.ok) throw new Error('Fetch /api/hortas não ok');
            const data = await res.json();
            const gardens = (data || []).map(h => ({
                id: h.id,
                name: h.nome,
                lat: h.latitude,
                lng: h.longitude,
                foods: (h.produtos || '')
                    .split(',')
                    .map(x => x.trim())
                    .filter(x => x.length > 0)
            }));
            renderMarkers(gardens, leafIcon);
        } catch {
            const fallback = [
                { id: 1, name: 'Horta Comunitária Central', lat: -21.6027, lng: -48.3605, foods: ['alface', 'tomate', 'cenoura'] },
                { id: 2, name: 'Horta Escola Municipal', lat: -21.6081, lng: -48.3722, foods: ['alface', 'couve'] }
            ];
            renderMarkers(fallback, leafIcon);
        }
    }

    // --- Inicialização ---
    const leafIcon = createLeafIcon();
    initMap();
    attachPopupAutoCenter();
    initSearchInput();
    initPopupDetails();
    loadGardens(leafIcon);

});
