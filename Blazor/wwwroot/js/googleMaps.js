let map;
let marker;
let geocoder;
let dotNetRef;
let mapInitialized = false;
let pendingInitialization = null;

function getCurrentPosition() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation is not supported by your browser'));
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                // Ensure we have valid coordinates
                if (position.coords.latitude && position.coords.longitude) {
                    resolve({
                        coords: {
                            latitude: position.coords.latitude,
                            longitude: position.coords.longitude,
                            accuracy: position.coords.accuracy || 0,
                            altitude: position.coords.altitude || null,
                            altitudeAccuracy: position.coords.altitudeAccuracy || null,
                            heading: position.coords.heading || null,
                            speed: position.coords.speed || null
                        },
                        timestamp: position.timestamp
                    });
                } else {
                    reject(new Error('Invalid coordinates received'));
                }
            },
            (error) => reject(error),
            {
                enableHighAccuracy: true,
                timeout: 5000,
                maximumAge: 0
            }
        );
    });
}

function initializeGoogleMaps(mapElementId, dotNetReference, initialLat, initialLng) {
    if (!mapInitialized) {
        console.log('Waiting for Google Maps API to load...');
        // Store the initialization parameters for later use
        pendingInitialization = {
            mapElementId,
            dotNetReference,
            initialLat,
            initialLng
        };
        return;
    }

    try {
        console.log('Initializing Google Maps...');
        dotNetRef = dotNetReference;
        
        // Initialize the map with the current position
        map = new google.maps.Map(document.getElementById(mapElementId), {
            center: { lat: initialLat, lng: initialLng },
            zoom: 15,
            mapTypeControl: true,
            streetViewControl: true,
            fullscreenControl: true
        });
        
        // Initialize the geocoder
        geocoder = new google.maps.Geocoder();
        
        // Add initial marker
        updateMapMarker(initialLat, initialLng);
        
        // Add click listener to the map
        map.addListener("click", (event) => {
            const lat = event.latLng.lat();
            const lng = event.latLng.lng();
            
            // Update marker position
            updateMapMarker(lat, lng);
            
            // Notify Blazor
            dotNetRef.invokeMethodAsync("OnMapClick", lat, lng);
        });

        console.log('Google Maps initialized successfully');
    } catch (error) {
        console.error('Error initializing Google Maps:', error);
        throw error;
    }
}

function updateMapMarker(lat, lng) {
    if (!map || !mapInitialized) {
        console.log('Map not initialized yet');
        return;
    }

    const position = { lat: lat, lng: lng };
    
    if (marker) {
        marker.setPosition(position);
    } else {
        marker = new google.maps.Marker({
            position: position,
            map: map,
            draggable: true
        });
        
        // Add drag end listener
        marker.addListener("dragend", (event) => {
            const newLat = event.latLng.lat();
            const newLng = event.latLng.lng();
            dotNetRef.invokeMethodAsync("OnMapClick", newLat, newLng);
        });
    }
    
    // Center map on marker
    map.setCenter(position);
    map.setZoom(15);
}

// Initialize map when Google Maps API is loaded
window.initMap = function() {
    console.log('Google Maps API loaded');
    mapInitialized = true;
    
    // If there's a pending initialization, execute it now
    if (pendingInitialization) {
        console.log('Executing pending initialization...');
        initializeGoogleMaps(
            pendingInitialization.mapElementId,
            pendingInitialization.dotNetReference,
            pendingInitialization.initialLat,
            pendingInitialization.initialLng
        );
        pendingInitialization = null;
    }
};

// Export the functions
window.getCurrentPosition = getCurrentPosition;
window.initializeGoogleMaps = initializeGoogleMaps;
window.updateMapMarker = updateMapMarker; 