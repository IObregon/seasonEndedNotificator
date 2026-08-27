<script setup lang="ts">
import { ref } from 'vue'
import ShowSearch from './components/ShowSearch.vue'
import ShowDetails from './components/ShowDetails.vue'

type ShowDetailsData = {
  providerId: number
  title: string
  premiereYear: number | null
  status: string
  seasons: Array<{
    number: number
    premiereDate: string | null
    endDate: string | null
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

async function loadDetails(providerId: number) {
  const response = await fetch(`/api/shows/${providerId}`)
  if (!response.ok) {
    detailsError.value = true
    selectedShow.value = null
    return
  }

  detailsError.value = false
  selectedShow.value = await response.json()
}

async function loadFollows() {
  const response = await fetch('/api/follows')
  if (!response.ok) {
    followedShows.value = []
    return
  }

  followedShows.value = await response.json()
}
</script>

<template>
  <main class="shell">
    <p class="eyebrow">SEASON FINALE NOTIFICATIONS</p>
    <h1>Season Ended</h1>
    <p class="summary">Know when a TV season is complete.</p>
    <ShowSearch @select="loadDetails" />
    <p v-if="detailsError">Show details are unavailable. Try again.</p>
    <ShowDetails v-if="selectedShow" :show="selectedShow" />
    <section class="followed-shows">
      <div class="section-heading">
        <h2>Followed shows</h2>
        <button type="button" @click="loadFollows">Refresh</button>
      </div>
      <p v-if="!followedShows.length">No followed shows yet.</p>
      <ul v-else>
        <li v-for="show in followedShows" :key="show.providerId">
          {{ show.title }} · {{ show.status }}
        </li>
      </ul>
    </section>
  </main>
</template>
