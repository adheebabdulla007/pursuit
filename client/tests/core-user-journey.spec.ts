import { expect, test } from '@playwright/test'

function unique(prefix: string) { return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}` }
const password = 'Password123!'

async function register(page: import('@playwright/test').Page, role: 'Employer' | 'JobSeeker', email: string, company?: string) {
  await page.goto('/register')
  await page.getByLabel('First Name').fill(role === 'Employer' ? 'Emma' : 'Jill')
  await page.getByLabel('Last Name').fill(role === 'Employer' ? 'Employer' : 'Seeker')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  if (role === 'Employer') { await page.getByLabel('I am a').selectOption('Employer'); await page.getByLabel('Company Name').fill(company!) }
  await page.getByRole('button', { name: 'Register' }).click()
  await expect(page).toHaveURL(role === 'Employer' ? /\/employer\/jobs$/ : /\/jobs$/)
}

async function login(page: import('@playwright/test').Page, email: string, expectedPath: RegExp, loginPassword = password) {
  await page.goto('/login'); await page.getByLabel('Email').fill(email); await page.getByLabel('Password').fill(loginPassword); await page.getByRole('button', { name: 'Log In' }).click()
  await expect(page).toHaveURL(expectedPath)
}

async function navigateViaShell(page: import('@playwright/test').Page, kind: 'link' | 'button', name: string) {
  const mobileMenu = page.getByRole('button', { name: 'Open navigation' })
  if (await mobileMenu.isVisible()) {
    await mobileMenu.click()
  }

  const control = kind === 'link'
    ? page.getByRole('link', { name, exact: true }).filter({ visible: true })
    : page.getByRole('button', { name, exact: true }).filter({ visible: true })
  await control.first().click()
}

async function logout(page: import('@playwright/test').Page) {
  await navigateViaShell(page, 'button', 'Log out')
  await expect(page).toHaveURL(/\/login$/)
}

test('complete hiring workflow crosses every role and preserves status', async ({ page }) => {
  test.setTimeout(90_000)
  const marker = unique('flow')
  const employerEmail = `employer-${marker}@example.com`
  const seekerEmail = `seeker-${marker}@example.com`
  const company = `Northstar ${marker}`
  const jobTitle = `Platform Engineer ${marker}`

  await register(page, 'Employer', employerEmail, company)
  await expect(page).toHaveURL(/\/employer\/jobs$/)
  await navigateViaShell(page, 'link', 'Post a job')
  await page.getByLabel('Job title').fill(jobTitle)
  await page.getByLabel('Description').fill('Own reliable services, production diagnostics, and clear technical decisions.')
  await page.getByLabel('Location').fill('Riyadh')
  await page.getByLabel('Minimum salary').fill('80000')
  await page.getByLabel('Maximum salary').fill('120000')
  await page.getByRole('button', { name: 'Publish job' }).click()
  await expect(page).toHaveURL(/\/employer\/jobs$/)
  await expect(page.getByRole('heading', { name: jobTitle })).toBeVisible()
  await logout(page)

  await register(page, 'JobSeeker', seekerEmail)
  await expect(page).toHaveURL(/\/jobs$/)
  await page.getByLabel('Keyword').fill(jobTitle)
  await page.getByRole('button', { name: 'Search jobs' }).click()
  await page.getByRole('link', { name: new RegExp(jobTitle) }).click()
  await expect(page.getByRole('heading', { level: 1, name: jobTitle })).toBeVisible()
  await page.getByLabel(/Resume/).setInputFiles({ name: 'resume.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4\nPursuit E2E resume') })
  await page.getByRole('button', { name: 'Apply for this role' }).click()
  await expect(page.getByText('Application submitted.')).toBeVisible()
  await page.getByRole('link', { name: 'View My Applications' }).click()
  await expect(page).toHaveURL(/\/applications$/)
  await expect(page.getByRole('heading', { name: 'My applications' })).toBeVisible()
  await expect(page.getByText(company, { exact: true })).toBeVisible()
  await expect(page.getByText('Applied', { exact: true })).toBeVisible()
  await logout(page)

  await login(page, employerEmail, /\/employer\/jobs$/)
  const jobRow = page.getByRole('heading', { name: jobTitle }).locator('xpath=ancestor::div[contains(@class,"rounded-lg")][1]')
  await jobRow.getByRole('link', { name: 'Applications' }).click()
  await page.getByRole('link', { name: /Jill Seeker/ }).click()
  const resumeResponse = page.waitForResponse((response) => response.url().includes('/resume') && response.request().method() === 'GET')
  await page.getByRole('button', { name: 'Open resume' }).click()
  expect((await resumeResponse).status()).toBe(200)
  await page.getByLabel('Application status').selectOption('Reviewed')
  await page.getByRole('button', { name: 'Update status' }).click()
  await page.getByRole('button', { name: 'Confirm status' }).click()
  await expect(page.locator('[data-status="reviewed"]').first()).toBeVisible()
  await logout(page)

  await login(page, seekerEmail, /\/jobs$/)
  await navigateViaShell(page, 'link', 'My applications')
  await expect(page.getByText('Reviewed', { exact: true })).toBeVisible()
  await logout(page)

  await login(page, 'admin.e2e@pursuit.test', /\/admin$/, 'PursuitE2EAdmin1!')
  await expect(page).toHaveURL(/\/admin$/)
  for (let index = 0; index < 20 && await page.getByText(seekerEmail, { exact: true }).count() === 0; index += 1) {
    const next = page.getByRole('button', { name: 'Next' })
    if (await next.isDisabled()) break
    const response = page.waitForResponse((candidate) => candidate.url().includes('/api/admin/users?') && candidate.request().method() === 'GET')
    await next.click(); await response
  }
  const accountEmail = page.getByText(seekerEmail, { exact: true }).filter({ visible: true }).first()
  await expect(accountEmail).toBeVisible()
  const account = (page.viewportSize()?.width ?? 1280) < 768
    ? accountEmail.locator('xpath=ancestor::div[contains(@class,"rounded-lg")][1]')
    : accountEmail.locator('xpath=ancestor::tr[1]')
  await account.getByRole('button', { name: 'Deactivate' }).click()
  await page.getByRole('button', { name: 'Confirm deactivation' }).click()
  await expect(account.getByRole('button', { name: 'Activate' })).toBeVisible()
})

test('mobile navigation, filters, focus, and detail layout remain usable', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== 'mobile-chromium')
  await page.goto('/jobs')
  await page.getByRole('button', { name: 'Open navigation' }).click()
  await expect(page.getByRole('dialog')).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(page.getByRole('button', { name: 'Open navigation' })).toBeFocused()
  await page.getByRole('button', { name: 'Filters' }).click()
  await expect(page.getByRole('dialog', { name: 'Filter jobs' })).toBeVisible()
  await page.getByRole('button', { name: 'Done' }).click()
  const overflow = await page.evaluate('document.documentElement.scrollWidth - document.documentElement.clientWidth')
  expect(overflow).toBeLessThanOrEqual(1)
  const firstJob = page.locator('a[href^="/jobs/"]').filter({ visible: true }).first()
  if (await firstJob.count()) { await firstJob.click(); await expect(page).toHaveURL(/\/jobs\/[0-9a-f-]+/); await expect(page.getByRole('heading', { level: 1 })).toBeVisible(); expect(await page.evaluate('document.documentElement.scrollWidth - document.documentElement.clientWidth')).toBeLessThanOrEqual(1) }
})
