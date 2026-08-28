<script setup lang="ts">
import { onMounted, ref } from 'vue'
import ShowSearch from './components/ShowSearch.vue'
import ShowDetails from './components/ShowDetails.vue'
import NotificationSettings from './components/NotificationSettings.vue'
import LoginView from './components/LoginView.vue'

type ShowDetailsData = {
  providerId: number
  title: string
  premiereYear: number | null
  status: string
  seasons: Array<{
    number: number
    premiereDate: string | null
    endDate: string | null
    completedAt: string | null
  }>
}

type FollowedShowData = {
  providerId: number
  title: string
  status: string
}

const selectedShow = ref<ShowDetailsData | null>(null)
const detailsError = ref(false)
const followedShows = ref<FollowedShowData[]>([])
const authChecking = ref(true)
const authenticated = ref(false)

onMounted(async () => {
  const response = await fetch('/api/auth/me')
  authChecking.value = false
  if (response.ok) {
    authenticated.value = true
    await loadFollows()
  }
})

function onAuthenticated() {
  authenticated.value = true
  loadFollows()
}

function closeDetails() {
  selectedShow.value = null
  detailsError.value = false
}

async function loadDetails(providerId: number) {
  const response = await fetch(`/api/shows/${providerId}`)
  if (!response.ok) {
    detailsError.value = true
    selectedShow.value = null
    return
  }

  detailsError.value = false
  selectedShow.value = await response.json()

  await nextTick()
  document.getElementById('show-details')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

async function loadFollows() {
  const response = await fetch('/api/follows')
  if (!response.ok) {
    followedShows.value = []
    return
  }

  followedShows.value = await response.json()
}

async function unfollow(providerId: number) {
  const response = await fetch(`/api/shows/${providerId}/follow`, { method: 'DELETE' })
  if (response.ok) {
    followedShows.value = followedShows.value.filter(show => show.providerId !== providerId)
  }
}

async function nextTick() {
  await new Promise(resolve => requestAnimationFrame(resolve))
}
</script>

<template>
  <main class="shell">
    <template v-if="authChecking">
      <p>Loading…</p>
    </template>
    <template v-else-if="!authenticated">
      <LoginView @authenticated="onAuthenticated" />
    </template>
    <template v-else>
      <header class="app-header">
        <p class="eyebrow">SEASON FINALE NOTIFICATIONS</p>
        <h1>Season Ended</h1>
        <p class="summary">Know when a TV season is complete.</p>
      </header>

      <div class="app-grid">
        <div class="panel-left">
          <ShowSearch :selected-provider-id="selectedShow?.providerId ?? null" @select="loadDetails" />
        </div>

        <div class="panel-right">
          <div id="show-details">
            <p v-if="detailsError" class="details-error">Show details are unavailable. Try again.</p>
            <p v-if="!selectedShow && !detailsError" class="details-placeholder">
              Search for a show and select it to see details.
            </p>
            <ShowDetails v-if="selectedShow" :show="selectedShow" @close="closeDetails" />
          </div>

          <section class="followed-shows">
            <div class="section-heading">
              <h2>Followed shows</h2>
              <button type="button" @click="loadFollows">Refresh</button>
            </div>
            <p v-if="!followedShows.length">No followed shows yet.</p>
            <ul v-else>
              <li v-for="show in followedShows" :key="show.providerId">
                <span>{{ show.title }} · {{ show.status }}</span>
                <button type="button" @click="unfollow(show.providerId)">Unfollow</button>
              </li>
            </ul>
          </section>

          <NotificationSettings />
        </div>
      </div>
    </template>
  </main>
</template>
