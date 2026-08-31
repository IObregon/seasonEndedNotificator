<script setup lang="ts">
import { onMounted, ref } from 'vue'
import ShowSearch from './components/ShowSearch.vue'
import ShowDetails from './components/ShowDetails.vue'
import NotificationSettings from './components/NotificationSettings.vue'
import LoginView from './components/LoginView.vue'
import { api, type ShowDetailsData, type FollowedShowData } from './api'

const selectedShow = ref<ShowDetailsData | null>(null)
const detailsError = ref(false)
const followedShows = ref<FollowedShowData[]>([])
const followsError = ref(false)
const authChecking = ref(true)
const authenticated = ref(false)

onMounted(async () => {
  const me = await api.getMe()
  if (me) {
    authenticated.value = true
    authChecking.value = false
    await loadFollows()
    return
  }

  if (await api.autoLogin()) {
    authenticated.value = true
    authChecking.value = false
    await loadFollows()
    return
  }

  authChecking.value = false
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
  try {
    detailsError.value = false
    selectedShow.value = await api.getShowDetails(providerId)

    await nextTick()
    document.getElementById('show-details')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  } catch {
    detailsError.value = true
    selectedShow.value = null
  }
}

async function loadFollows() {
  followsError.value = false
  try {
    followedShows.value = await api.getFollows()
  } catch {
    followedShows.value = []
    followsError.value = true
  }
}

async function unfollow(providerId: number) {
  const show = followedShows.value.find(s => s.providerId === providerId)
  if (!show) return

  followedShows.value = followedShows.value.filter(s => s.providerId !== providerId)
  try {
    await api.unfollowShow(providerId)
  } catch {
    followedShows.value = [...followedShows.value, show]
  }
}

async function nextTick() {
  await new Promise(resolve => requestAnimationFrame(resolve))
}
</script>

<template>
  <main class="shell">
    <div v-if="authChecking" class="skeleton-loader" aria-label="Loading">
      <div class="skeleton skeleton-title"></div>
      <div class="skeleton skeleton-line"></div>
      <div class="skeleton skeleton-line short"></div>
    </div>
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
            <ShowDetails v-if="selectedShow" :show="selectedShow" @close="closeDetails" @followed="loadFollows" />
          </div>

          <section class="followed-shows">
            <div class="section-heading">
              <h2>Followed shows</h2>
              <button type="button" @click="loadFollows">Refresh</button>
            </div>
            <p v-if="followsError" class="follows-error">Could not load followed shows.</p>
            <p v-else-if="!followedShows.length">No followed shows yet.</p>
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
