// File Path: wwwroot/js/map.js

window.mapFunctions = {
    initPickMap: function (elementId, dotnetHelper, initialAddress) {
        console.log("Map Init Start: " + elementId);

        var container = document.getElementById(elementId);
        if (!container) {
            console.error("Map container not found!");
            return;
        }

        // Agar map pehle se hai to usay clear karo (Grey box fix)
        if (container._leaflet_id) {
            container._leaflet_id = null;
            container.innerHTML = "";
        }

        // Default Karachi Location
        var lat = 24.8607;
        var lng = 67.0011;
        var zoom = 12;

        var map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap'
        }).addTo(map);

        var marker = L.marker([lat, lng], { draggable: true }).addTo(map);

        // 👇 ZAROORI: Grey box fix karne ke liye
        setTimeout(function () {
            map.invalidateSize();
        }, 500);

        // Click Event
        map.on('click', function (e) {
            marker.setLatLng(e.latlng);
            getAddress(e.latlng.lat, e.latlng.lng);
        });

        // Drag Event
        marker.on('dragend', function (e) {
            var pos = marker.getLatLng();
            getAddress(pos.lat, pos.lng);
        });

        function getAddress(lat, lng) {
            fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`)
                .then(res => res.json())
                .then(data => {
                    if (dotnetHelper) {
                        dotnetHelper.invokeMethodAsync('UpdateLocationFromMap', data.display_name);
                    }
                });
        }

        // Global Search Function attach
        window.searchAddressOnMap = function (addressQuery) {
            fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${addressQuery}`)
                .then(res => res.json())
                .then(data => {
                    if (data && data.length > 0) {
                        var newLat = data[0].lat;
                        var newLon = data[0].lon;
                        var newLatLng = new L.LatLng(newLat, newLon);

                        marker.setLatLng(newLatLng);
                        map.setView(newLatLng, 15);

                        if (dotnetHelper) {
                            dotnetHelper.invokeMethodAsync('UpdateLocationFromMap', data[0].display_name);
                        }
                    } else {
                        alert("Address not found!");
                    }
                });
        };
    }
};