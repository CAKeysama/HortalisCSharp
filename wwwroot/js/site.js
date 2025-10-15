// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', () => {
    const mapEl = document.getElementById('map');
    if (!mapEl || typeof L === 'undefined') return;

    const defaultCenter = [-21.6036, -48.3640]; // Matão, SP
    const map = L.map('map', { zoomControl: true, attributionControl: true })
        .setView(defaultCenter, 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    let userPos = null;

    // Tenta centralizar na localização do usuário
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            pos => {
                userPos = [pos.coords.latitude, pos.coords.longitude];
                map.setView(userPos, 14);
                L.marker(userPos).addTo(map).bindPopup('Você está aqui');
            },
            err => {
                alert('Não foi possível acessar sua localização. Usando localização padrão.');
                map.setView(defaultCenter, 13);
            },
            { enableHighAccuracy: true, timeout: 5000, maximumAge: 300000 }
        );
    }

    // Ícone customizado: círculo verde com folha branca
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
    const leafIcon = createLeafIcon();

    // Referências de UI da busca
    const input = document.getElementById('searchInput');
    const resultsEl = document.getElementById('resultsCount');
    const resultsContainer = document.getElementById('searchResults');
    const noResultsEl = document.getElementById('noResults');
    const suggestionsEl = document.getElementById('suggestions');

    const normalize = s => (s || '').toString().trim().toLowerCase();

    let markers = [];
    let allFoods = [];

    // Centraliza o popup atual no centro do mapa
    function centerActivePopup() {
        const popup = document.querySelector('.leaflet-popup');
        if (!popup) return;

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

    // Recentrar sempre que um popup abre
    map.on('popupopen', () => setTimeout(centerActivePopup, 0));

    function popupHtml(g) {
        const dest = encodeURIComponent(`${g.lat},${g.lng}`);
        const origin = userPos ? `&origin=${encodeURIComponent(userPos[0] + ',' + userPos[1])}` : '';
        const routeUrl = `https://www.google.com/maps/dir/?api=1&destination=${dest}${origin}&travelmode=driving`;
        const foodsText = g.foods?.length ? `Alimentos: ${g.foods.join(', ')}` : '';

        // Novo card visual (retangular, limpo e com hierarquia)
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

    function renderMarkers(gardens) {
        markers.forEach(m => { if (map.hasLayer(m)) map.removeLayer(m); });
        markers = [];

        allFoods = [...new Set(gardens.flatMap(g => (g.foods || [])))];

        gardens.forEach(g => {
            const m = L.marker([g.lat, g.lng], { icon: leafIcon }).addTo(map);
            m.bindPopup(popupHtml(g));
            m._foods = g.foods || [];
            markers.push(m);
        });

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

        if (resultsEl && resultsContainer) {
            resultsEl.textContent = String(visible);
            resultsContainer.classList.toggle('d-none', false);
        }

        const hasNo = visible === 0 && t;
        if (noResultsEl && suggestionsEl) {
            noResultsEl.classList.toggle('d-none', !hasNo);
            if (hasNo) suggestionsEl.textContent = allFoods.slice(0, 5).join(', ');
        }
    }

    if (input) {
        input.addEventListener('input', e => filterMarkers(e.target.value));
    }

    // "Mais informações": expande dentro do próprio popup (sem modal) e recentra
    mapEl.addEventListener('click', async (e) => {
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
                            <div>${h.latitude.toFixed(5)}, ${h.longitude.toFixed(5)}</div>
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
                            <div>${new Date(h.criadoEm).toLocaleString()}</div>
                        </div>
                    </div>
                </div>
            `;
            container.classList.remove('d-none');
            btn.innerHTML = `<i class="bi bi-chevron-up me-1"></i> Menos informações`;

            setTimeout(centerActivePopup, 0);

            container.querySelectorAll('img.horta-photo').forEach(img => {
                img.addEventListener('load', () => setTimeout(centerActivePopup, 0), { once: true });
            });
        } catch {
            container.innerHTML = `<div class="alert alert-warning mb-0">Não foi possível carregar as informações da horta.</div>`;
            container.classList.remove('d-none');
            btn.innerHTML = oldHtml;
            setTimeout(centerActivePopup, 0);
        } finally {
            btn.disabled = false;
        }
    });

    // Carrega as hortas do backend
    fetch('/api/hortas', { headers: { 'Accept': 'application/json' } })
        .then(r => r.ok ? r.json() : Promise.reject(r.statusText))
        .then((data) => {
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
            renderMarkers(gardens);
        })
        .catch(() => {
            const gardens = [
                { id: 1, name: 'Horta Comunitária Central', lat: -21.6027, lng: -48.3605, foods: ['alface', 'tomate', 'cenoura'] },
                { id: 2, name: 'Horta Escola Municipal', lat: -21.6081, lng: -48.3722, foods: ['alface', 'couve'] }
            ];
            renderMarkers(gardens);
        });
});
