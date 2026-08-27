<script setup lang="ts">
import { onMounted, ref } from 'vue'

const emailEnabled = ref(true)
const error = ref(false)
const saving = ref(false)

async function load() {
  const response = await fetch('/api/notification-preferences')
  if (!response.ok) {
    error.value = true
    return
  }

  const preferences = await response.json()
  emailEnabled.value = preferences.emailEnabled
  error.value = false
}

async function save() {
  saving.value = true
  const response = await fetch('/api/notification-preferences', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ emailEnabled: emailEnabled.value }),
  })
  error.value = !response.ok
  saving.value = false
  if (!response.ok) {
    await load()
  }
}

onMounted(load)
</script>

<template>
  <section class="notification-settings" aria-labelledby="notifications-title">
    <h2 id="notifications-title">Notifications</h2>
    <label>
      <input v-model="emailEnabled" type="checkbox" :disabled="saving" @change="save" />
      Email season-ended digests
    </label>
    <p v-if="error">Could not save notification settings.</p>
  </section>
</template>
