// HydroAI PWA Service Worker - basic fetch handler for installability
const CACHE_NAME = 'hydroai-v1';

self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', (event) => {
  // Pass through to network; no offline cache for now
  event.respondWith(fetch(event.request));
});
