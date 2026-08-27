import { describe, expect, it } from 'vitest'
import { searchShows } from './showSearch'

describe('show search states', () => {
  it('returns successful results', async () => {
    const show = { providerId: 82, title: 'Game of Thrones', premiereYear: 2011, status: 'Ended', imageUrl: null }

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
