window.mapFunctions = {
    loadMap: function (type, suppliers) {

        if (window.myMap) window.myMap.remove();

        // Karachi center (for default demo)
        window.myMap = L.map('map').setView([24.8607, 67.0011], 12);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19
        }).addTo(window.myMap);

        suppliers.forEach(s => {
            L.marker([s.lat, s.lng])
                .addTo(window.myMap)
                .bindPopup(`<b>${s.name}</b><br>${s.description}`);
        });git 
    }
}
