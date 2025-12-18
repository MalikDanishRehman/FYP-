window.mapFunctions = {
    loadMap: function (type, suppliers) {

        if (window.myMap) window.myMap.remove();

        // Karachi center
        window.myMap = L.map('map').setView([24.8607, 67.0011], 12);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap' // Attribution dena achi practice hai
        }).addTo(window.myMap);

        suppliers.forEach(s => {
            // 🔒 SECURITY FIX: Create Element instead of String
            var container = document.createElement('div');

            var title = document.createElement('b');
            title.innerText = s.name || "Unknown"; // Script tags yahan text ban jayenge

            var lineBreak = document.createElement('br');

            var desc = document.createElement('span');
            desc.innerText = s.description || "No description"; // Secure

            // Elements ko combine karna
            container.appendChild(title);
            container.appendChild(lineBreak);
            container.appendChild(desc);

            L.marker([s.lat, s.lng])
                .addTo(window.myMap)
                .bindPopup(container); // Ab hum secure DOM element pass kar rahe hain
        });
    }
}