<script setup lang="ts">
import { ref } from 'vue'
import { api, type SearchState, type ShowResult } from '../api'

const props = defineProps<{ selectedProviderId: number | null }>()
const emit = defineEmits<{ select: [providerId: number] }>()

const query = ref('')
const results = ref<ShowResult[]>([])
const state = ref<SearchState | 'loading'>('idle')

async function search() {
  if (!query.value.trim()) return
  state.value = 'loading'

  const searchResult = await api.searchShows(query.value)
  results.value = searchResult.results
  state.value = searchResult.state
}

function select(providerId: number) {
  emit('select', providerId)
}
</script>

<template>
  <section class="show-search" aria-labelledby="search-title">
    <h2 id="search-title">Find a show</h2>
    <form @submit.prevent="search">
      <label for="show-query">Show title</label>
      <div class="search-row">
        <input id="show-query" v-model="query" required />
        <button type="submit" :disabled="state === 'loading'">Search</button>
      </div>
    </form>

    <div v-if="state === 'loading'" class="search-skeleton" aria-hidden="true">
      <div class="skeleton skeleton-result" v-for="i in 3" :key="i"></div>
    </div>
    <p v-if="state === 'empty'" class="search-status">No shows found.</p>
    <p v-if="state === 'rate-limited'" class="search-status">Too many searches. Try again shortly.</p>
    <p v-if="state === 'error'" class="search-status">Search is unavailable. Try again.</p>

    <ul v-if="results.length" class="results">
      <li
        v-for="show in results"
        :key="show.providerId"
        :class="{ selected: show.providerId === props.selectedProviderId }"
        @click="select(show.providerId)"
      >
        <img v-if="show.imageUrl" :src="show.imageUrl" :alt="`${show.title} poster`" />
        <div v-else class="poster-placeholder" aria-hidden="true">No image</div>
        <div>
          <strong>{{ show.title }}</strong>
          <span>{{ show.premiereYear ?? 'Year unknown' }} · {{ show.status }}</span>
        </div>
      </li>
    </ul>
  </section>
</template>
