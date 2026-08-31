<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api } from '../api'

const emailEnabled = ref(true)
const telegramConnected = ref(false)
const telegramDeepLink = ref('')
const pushDevices = ref<Array<{ id: string; label: string | null; registeredAt: string; lastSuccessAt: string | null }>>([])
const error = ref(false)
const saving = ref(false)

async function load() {
  error.value = false
  try {
    const [prefs, tgStatus, subs] = await Promise.all([
      api.getEmailPreferences(),
      api.getTelegramStatus(),
      api.getPushSubscriptions(),
    ])
    emailEnabled.value = prefs.emailEnabled
    telegramConnected.value = tgStatus.connected
    pushDevices.value = subs
  } catch {
    error.value = true
  }
}

async function saveEmail() {
  saving.value = true
  try {
    await api.setEmailPreferences(emailEnabled.value)
  } catch {
    await load()
  }
  saving.value = false
}

async function connectTelegram() {
  try {
    const result = await api.createTelegramLink()
    telegramDeepLink.value = result.deepLink
  } catch {
    error.value = true
  }
}

async function disconnectTelegram() {
  try {
    await api.disconnectTelegram()
    telegramConnected.value = false
    telegramDeepLink.value = ''
  } catch {
    error.value = true
  }
}

async function removePushDevice(id: string) {
  const device = pushDevices.value.find(d => d.id === id)
  pushDevices.value = pushDevices.value.filter(d => d.id !== id)
  try {
    await api.removePushSubscription(id)
  } catch {
    if (device) pushDevices.value = [...pushDevices.value, device]
  }
}

onMounted(load)
</script>

<template>
  <section class="notification-settings" aria-labelledby="notifications-title">
    <h2 id="notifications-title">Notifications</h2>

    <label>
      <input v-model="emailEnabled" type="checkbox" :disabled="saving" @change="saveEmail" />
      Email season-ended digests
    </label>

    <div class="telegram-section">
      <h3>Telegram</h3>
      <template v-if="telegramConnected">
        <p class="status-connected">✓ Connected</p>
        <button type="button" class="disconnect-btn" @click="disconnectTelegram">Disconnect</button>
      </template>
      <template v-else>
        <button type="button" class="connect-btn" @click="connectTelegram">Connect Telegram</button>
        <p v-if="telegramDeepLink" class="telegram-link">
          Open this link on your phone: <a :href="telegramDeepLink" target="_blank">{{ telegramDeepLink }}</a>
        </p>
      </template>
    </div>

    <div class="push-section">
      <h3>Push devices</h3>
      <p v-if="!pushDevices.length" class="push-empty">No push devices registered.</p>
      <ul v-else class="push-list">
        <li v-for="device in pushDevices" :key="device.id">
          <span>{{ device.label ?? 'Device' }}</span>
          <button type="button" @click="removePushDevice(device.id)">Remove</button>
        </li>
      </ul>
    </div>

    <p v-if="error" class="settings-error">Could not load notification settings.</p>
  </section>
</template>
