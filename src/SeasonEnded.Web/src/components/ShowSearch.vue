<script setup lang="ts">
import { ref } from 'vue'
import { searchShows, type SearchState, type ShowResult } from '../showSearch'

const query = ref('')
const results = ref<ShowResult[]>([])
const state = ref<SearchState | 'loading'>('idle')

async function search() {
  if (!query.value.trim()) {
    return
  }
  state.value = 'loading'

  const searchResult = await searchShows(query.value)
  results.value = searchResult.results
  state.value = searchResult.state
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

    <p v-if="state === 'empty'">No shows found.</p>
    <p v-if="state === 'rate-limited'">Too many searches. Try again shortly.</p>
    <p v-if="state === 'error'">Search is unavailable. Try again.</p>

    <ul v-if="results.length" class="results">
      <li v-for="show in results" :key="show.providerId">
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
