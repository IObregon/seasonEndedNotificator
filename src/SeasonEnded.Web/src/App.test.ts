import { createSSRApp } from 'vue'
import { renderToString } from 'vue/server-renderer'
import { describe, expect, it } from 'vitest'
import App from './App.vue'

describe('App', () => {
  it('shows loading state while checking auth', async () => {
    const html = await renderToString(createSSRApp(App))

    expect(html).toContain('skeleton')
  })
})
