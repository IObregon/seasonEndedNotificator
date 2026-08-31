import { createApp } from 'vue'
import './style.css'
import App from './App.vue'

if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => {})
  })
}

createApp(App).mount('#app')
