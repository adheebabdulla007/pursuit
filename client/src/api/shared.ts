export async function extractErrorMessage(response: Response): Promise<string> {
  try {
    const body = await response.json()

    if (body.errors && typeof body.errors === 'object') {
      const fieldMessages = Object.values(body.errors).flat()
      if (fieldMessages.length > 0) {
        return fieldMessages.join(' ')
      }
    }

    return body.message ?? `Request failed: ${response.status}`
  } catch {
    return `Request failed: ${response.status}`
  }
}