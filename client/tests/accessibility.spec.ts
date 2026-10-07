import { AxeBuilder } from '@axe-core/playwright'
import { expect, test } from '@playwright/test'

test.describe.configure({ mode: 'serial', timeout: 60_000 })

function unique(prefix: string) { return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 100000)}@example.com` }
async function seriousViolations(page: import('@playwright/test').Page) {
  const results = await new AxeBuilder({ page }).analyze()
  return results.violations.filter((violation) => violation.impact === 'serious' || violation.impact === 'critical')
}
async function register(page: import('@playwright/test').Page, role: 'Employer' | 'JobSeeker') {
  await page.goto('/register'); await page.getByLabel('First Name').fill('Axe'); await page.getByLabel('Last Name').fill(role); await page.getByLabel('Email').fill(unique(role)); await page.getByLabel('Password').fill('Password123!')
  if (role === 'Employer') { await page.getByLabel('I am a').selectOption('Employer'); await page.getByLabel('Company Name').fill(`Accessible ${Date.now()}`) }
  await page.getByRole('button', { name: 'Register' }).click()
  await expect(page).toHaveURL(role === 'Employer' ? /\/employer\/jobs$/ : /\/jobs$/)
}

test('public job search has no serious or critical Axe violations', async ({ page }) => { await page.goto('/jobs'); await expect(page.getByRole('heading', { name: /Find work worth pursuing/ })).toBeVisible(); expect(await seriousViolations(page)).toEqual([]) })
test('employer desk has no serious or critical Axe violations', async ({ page }) => { await register(page, 'Employer'); await expect(page.getByRole('heading', { name: 'My jobs' })).toBeVisible(); expect(await seriousViolations(page)).toEqual([]) })
test('job seeker history has no serious or critical Axe violations', async ({ page }) => { await register(page, 'JobSeeker'); await page.goto('/applications'); await expect(page.getByRole('heading', { name: 'My applications' })).toBeVisible(); expect(await seriousViolations(page)).toEqual([]) })
test('administrator desk has no serious or critical Axe violations', async ({ page }) => { await page.goto('/login'); await page.getByLabel('Email').fill('admin.e2e@pursuit.test'); await page.getByLabel('Password').fill('PursuitE2EAdmin1!'); await page.getByRole('button', { name: 'Log In' }).click(); await expect(page.getByRole('heading', { name: 'Admin desk' })).toBeVisible(); expect(await seriousViolations(page)).toEqual([]) })
