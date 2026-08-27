import { createSSRApp } from 'vue'
import { renderToString } from 'vue/server-renderer'
import { describe, expect, it } from 'vitest'
import App from './App.vue'

describe('App', () => {
  it('explains the purpose of the application', async () => {
    const html = await renderToString(createSSRApp(App))

    expect(html).toContain('<h1>Season Ended</h1>')
    expect(html).toContain('Know when a TV season is complete.')
  })
})
