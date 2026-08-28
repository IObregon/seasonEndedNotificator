<script setup lang="ts">
import { ref, watch } from 'vue'

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

const emit = defineEmits<{ close: [] }>()
const followed = ref(false)

watch(() => props.show.providerId, () => {
  followed.value = false
}, { immediate: true })

async function follow() {
  const response = await fetch(`/api/shows/${props.show.providerId}/follow`, { method: 'POST' })
  followed.value = response.ok
}
</script>

<template>
  <section class="show-details" aria-labelledby="details-title">
    <button class="details-close" type="button" @click="emit('close')" aria-label="Close details">×</button>
    <h2 id="details-title">{{ show.title }}</h2>
    <p class="details-meta">{{ show.premiereYear ?? 'Year unknown' }} · {{ show.status }}</p>
    <button type="button" :disabled="followed" class="follow-btn" @click="follow">
      {{ followed ? '✓ Following' : 'Follow show' }}
    </button>
    <h3>Seasons</h3>
    <ul>
      <li v-for="season in show.seasons" :key="season.number">
        <strong>Season {{ season.number }}</strong>
        <span v-if="season.completedAt" class="season-completed">Completed {{ season.completedAt.slice(0, 10) }}</span>
        <span v-else class="season-pending">{{ season.premiereDate ?? 'Unknown start' }} – {{ season.endDate ?? 'Unknown end' }}</span>
      </li>
    </ul>
  </section>
</template>
