import { describe, expect, it } from 'vitest'

type ShowResult = {
  providerId: number
  title: string
  premiereYear: number | null
  status: string
  imageUrl: string | null
}

type SearchState = 'idle' | 'empty' | 'error' | 'rate-limited'

async function searchShows(
  query: string,
  fetcher: typeof fetch = fetch,
): Promise<{ state: SearchState; results: ShowResult[] }> {
  const response = await fetcher(
    `/api/shows/search?query=${encodeURIComponent(query.trim())}`,
  )
  if (response.status === 429) return { state: 'rate-limited', results: [] }
  if (!response.ok) return { state: 'error', results: [] }

  const results: ShowResult[] = await response.json()
  return { state: results.length ? 'idle' : 'empty', results }
}

describe('show search states', () => {
  it('returns successful results', async () => {
    const show: ShowResult = { providerId: 82, title: 'Game of Thrones', premiereYear: 2011, status: 'Ended', imageUrl: null }

    const result = await searchShows('Game of Thrones', async () =>
      new Response(JSON.stringify([show]), { status: 200 }),
    )

    expect(result).toEqual({ state: 'idle', results: [show] })
  })

  it('returns empty state', async () => {
    const result = await searchShows('missing', async () =>
      new Response('[]', { status: 200 }),
    )

    expect(result).toEqual({ state: 'empty', results: [] })
  })

  it('returns rate-limited state', async () => {
    const result = await searchShows('show', async () => new Response(null, { status: 429 }))

    expect(result.state).toBe('rate-limited')
  })

  it('returns provider error state', async () => {
    const result = await searchShows('show', async () => new Response(null, { status: 502 }))

    expect(result.state).toBe('error')
  })
})
