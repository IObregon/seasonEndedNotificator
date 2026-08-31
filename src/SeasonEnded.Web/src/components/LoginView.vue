<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api } from '../api'

const email = ref('')
const token = ref('')
const message = ref('')
const error = ref(false)
const loading = ref(false)
const tokenMode = ref(false)

const emit = defineEmits<{ authenticated: [] }>()

onMounted(() => {
  const params = new URLSearchParams(window.location.search)
  const urlToken = params.get('token')
  if (urlToken) {
    token.value = urlToken
    tokenMode.value = true
    consumeToken()
  }
})

async function requestLink() {
  if (!email.value.trim()) return
  loading.value = true
  error.value = false
  message.value = ''

  try {
    const data = await api.requestMagicLink(email.value)
    message.value = data.message
    tokenMode.value = true
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

async function consumeToken() {
  if (!token.value.trim()) return
  loading.value = true
  error.value = false
  message.value = ''

  try {
    await api.consumeToken(token.value)
    token.value = ''
    message.value = ''
    tokenMode.value = false
    const params = new URLSearchParams(window.location.search)
    params.delete('token')
    window.history.replaceState({}, '', `${window.location.pathname}?${params}`)
    emit('authenticated')
  } catch {
    error.value = true
    message.value = 'Token is invalid, expired, or already used.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section class="login" aria-labelledby="login-title">
    <h2 id="login-title">Sign in</h2>

    <form v-if="!tokenMode" @submit.prevent="requestLink">
      <label for="email">Email</label>
      <div class="login-row">
        <input id="email" v-model="email" type="email" placeholder="you@example.com" required />
        <button type="submit" :disabled="loading">Send link</button>
      </div>
    </form>

    <form v-else @submit.prevent="consumeToken">
      <label for="token">Sign-in code</label>
      <div class="login-row">
        <input id="token" v-model="token" placeholder="Paste your code" required />
        <button type="submit" :disabled="loading">Sign in</button>
      </div>
    </form>

    <p v-if="message" class="login-info">{{ message }}</p>
    <p v-if="error" class="login-error">{{ message || 'Something went wrong. Try again.' }}</p>
  </section>
</template>
