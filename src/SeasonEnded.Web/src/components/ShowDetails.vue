<script setup lang="ts">
defineProps<{
  show: {
    title: string
    premiereYear: number | null
    status: string
    seasons: Array<{
      number: number
      premiereDate: string | null
      endDate: string | null
    }>
  }
}>()
</script>

<template>
  <section class="show-details" aria-labelledby="details-title">
    <h2 id="details-title">{{ show.title }}</h2>
    <p>{{ show.premiereYear ?? 'Year unknown' }} · {{ show.status }}</p>
    <h3>Seasons</h3>
    <ul>
      <li v-for="season in show.seasons" :key="season.number">
        <strong>Season {{ season.number }}</strong>
        <span>{{ season.premiereDate ?? 'Unknown start' }} – {{ season.endDate ?? 'Unknown end' }}</span>
      </li>
    </ul>
  </section>
</template>
