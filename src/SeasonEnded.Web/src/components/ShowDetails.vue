<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  show: {
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
}>()

const followed = ref(false)

async function follow() {
  const response = await fetch(`/api/shows/${props.show.providerId}/follow`, { method: 'POST' })
  followed.value = response.ok
}
</script>

<template>
  <section class="show-details" aria-labelledby="details-title">
    <h2 id="details-title">{{ show.title }}</h2>
    <p>{{ show.premiereYear ?? 'Year unknown' }} · {{ show.status }}</p>
    <button type="button" :disabled="followed" @click="follow">
      {{ followed ? 'Following' : 'Follow show' }}
    </button>
    <h3>Seasons</h3>
    <ul>
      <li v-for="season in show.seasons" :key="season.number">
        <strong>Season {{ season.number }}</strong>
        <span v-if="season.completedAt">Completed {{ season.completedAt.slice(0, 10) }}</span>
        <span v-else>{{ season.premiereDate ?? 'Unknown start' }} – {{ season.endDate ?? 'Unknown end' }}</span>
      </li>
    </ul>
  </section>
</template>
