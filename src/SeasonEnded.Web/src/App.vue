<script setup lang="ts">
import { ref } from 'vue'
import ShowSearch from './components/ShowSearch.vue'
import ShowDetails from './components/ShowDetails.vue'

type ShowDetailsData = {
  title: string
  premiereYear: number | null
  status: string
  seasons: Array<{
    number: number
    premiereDate: string | null
    endDate: string | null
  }>
}

const selectedShow = ref<ShowDetailsData | null>(null)
const detailsError = ref(false)

async function loadDetails(providerId: number) {
  const response = await fetch(`/api/shows/${providerId}`)
  detailsError.value = !response.ok
  selectedShow.value = response.ok ? await response.json() : null
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
  </main>
</template>
