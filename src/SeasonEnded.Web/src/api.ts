export type ShowResult = {
  providerId: number
  title: string
  premiereYear: number | null
  status: string
  imageUrl: string | null
}

export type ShowDetailsData = {
  providerId: number
  title: string
  premiereYear: number | null
  status: string
  imageUrl: string | null
  seasons: Array<{
    number: number
    premiereDate: string | null
    endDate: string | null
    completedAt: string | null
  }>
}

export type FollowedShowData = {
  providerId: number
  title: string
  premiereYear: number | null
  status: string
  imageUrl: string | null
  followedAt: string
}

export type AuthUser = {
  email: string
  role: string
}

export type EmailPreferences = {
  emailEnabled: boolean
}

export type SearchState = 'idle' | 'empty' | 'error' | 'rate-limited'

async function request<T>(
  url: string,
  options?: RequestInit,
): Promise<T> {
  const response = await fetch(url, options)
  if (!response.ok) throw new ApiError(response.status, response.statusText)
  if (response.status === 204) return undefined as T
  return response.json()
}

export class ApiError extends Error {
  status: number
  statusText: string
  constructor(status: number, statusText: string) {
    super(`API error ${status}: ${statusText}`)
    this.status = status
    this.statusText = statusText
  }
}

function jsonBody(body: unknown): RequestInit {
  return {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  }
}

function jsonPut(body: unknown): RequestInit {
  return {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  }
}

export const api = {
  async getMe(): Promise<AuthUser | null> {
    const response = await fetch('/api/auth/me')
    if (!response.ok) return null
    return response.json()
  },

  async autoLogin(): Promise<boolean> {
    if (!import.meta.env.DEV) return false
    const response = await fetch('/api/dev/auto-login', { method: 'POST' })
    return response.ok
  },

  async requestMagicLink(email: string): Promise<{ message: string }> {
    return request('/api/auth/magic-link', jsonBody({ email }))
  },

  async consumeToken(token: string): Promise<void> {
    await request('/api/auth/magic-link/consume', jsonBody({ token }))
  },

  async searchShows(
    query: string,
    fetcher: typeof fetch = fetch,
  ): Promise<{ state: SearchState; results: ShowResult[] }> {
    const response = await fetcher(
      `/api/shows/search?query=${encodeURIComponent(query.trim())}`,
    )
    if (response.status === 429)
      return { state: 'rate-limited', results: [] }
    if (!response.ok) return { state: 'error', results: [] }

    const results: ShowResult[] = await response.json()
    return { state: results.length ? 'idle' : 'empty', results }
  },

  async getShowDetails(providerId: number): Promise<ShowDetailsData> {
    return request(`/api/shows/${providerId}`)
  },

  async followShow(providerId: number): Promise<{ followedAt: string; created: boolean }> {
    return request(`/api/shows/${providerId}/follow`, { method: 'POST' })
  },

  async unfollowShow(providerId: number): Promise<void> {
    await request(`/api/shows/${providerId}/follow`, { method: 'DELETE' })
  },

  async getFollows(): Promise<FollowedShowData[]> {
    return request('/api/follows')
  },

  async getEmailPreferences(): Promise<EmailPreferences> {
    return request('/api/notification-preferences')
  },

  async setEmailPreferences(emailEnabled: boolean): Promise<void> {
    await request('/api/notification-preferences', jsonPut({ emailEnabled }))
  },

  async getTelegramStatus(): Promise<{ connected: boolean }> {
    return request('/api/telegram/status')
  },

  async createTelegramLink(): Promise<{ deepLink: string }> {
    return request('/api/telegram/link', { method: 'POST' })
  },

  async disconnectTelegram(): Promise<void> {
    await request('/api/telegram/connection', { method: 'DELETE' })
  },

  async getPushSubscriptions(): Promise<Array<{ id: string; label: string | null; registeredAt: string; lastSuccessAt: string | null }>> {
    return request('/api/push/subscriptions')
  },

  async registerPushSubscription(
    endpoint: string,
    p256dh: string,
    auth: string,
    label?: string,
  ): Promise<{ id: string }> {
    return request('/api/push/subscriptions', jsonBody({ endpoint, p256dh, auth, label }))
  },

  async removePushSubscription(id: string): Promise<void> {
    await request(`/api/push/subscriptions/${id}`, { method: 'DELETE' })
  },
}
