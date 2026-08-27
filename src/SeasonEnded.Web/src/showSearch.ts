export type ShowResult = {
  providerId: number
  title: string
  premiereYear: number | null
  status: string
  imageUrl: string | null
}

export type SearchState = 'idle' | 'empty' | 'error' | 'rate-limited'

export async function searchShows(
  query: string,
  fetcher: typeof fetch = fetch,
): Promise<{ state: SearchState; results: ShowResult[] }> {
  const response = await fetcher(`/api/shows/search?query=${encodeURIComponent(query.trim())}`)
  if (response.status === 429) return { state: 'rate-limited', results: [] }
  if (!response.ok) return { state: 'error', results: [] }

  const results: ShowResult[] = await response.json()
  return { state: results.length ? 'idle' : 'empty', results }
}
