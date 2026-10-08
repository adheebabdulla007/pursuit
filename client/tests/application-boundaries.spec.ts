import { expect, test } from '@playwright/test'

test('resume validation and repeat application preserve one submitted application', async ({ page }) => {
  test.setTimeout(90_000)
  const marker = `apply-edge-${Date.now()}-${Math.floor(Math.random() * 100000)}`
  const jobTitle = `Backend Engineer ${marker}`

  await page.goto('/register')
  await page.getByLabel('First Name').fill('Evan')
  await page.getByLabel('Last Name').fill('Employer')
  await page.getByLabel('Email').fill(`employer-${marker}@example.com`)
  await page.getByLabel('Password').fill('Password123!')
  await page.getByLabel('I am a').selectOption('Employer')
  await page.getByLabel('Company Name').fill(`Boundary ${marker}`)
  await page.getByRole('button', { name: 'Register' }).click()
  await expect(page).toHaveURL(/\/employer\/jobs$/)

  await page.getByRole('main').getByRole('link', { name: 'Post a job' }).click()
  await page.getByLabel('Job title').fill(jobTitle)
  await page.getByLabel('Description').fill('A test role for application boundaries.')
  await page.getByLabel('Location').fill('Remote')
  await page.getByLabel('Minimum salary').fill('1000')
  await page.getByLabel('Maximum salary').fill('2000')
  await page.getByRole('button', { name: 'Publish job' }).click()
  await expect(page.getByRole('heading', { name: jobTitle })).toBeVisible()
  const applicationHref = await page.getByRole('link', { name: 'Applications' }).first().getAttribute('href')
  const jobId = applicationHref!.split('/')[3]
  const mobileNavigation = page.getByRole('button', { name: 'Open navigation' })
  if (await mobileNavigation.isVisible()) await mobileNavigation.click()
  await page.getByRole('button', { name: 'Log out' }).click()
  await expect(page).toHaveURL(/\/login$/)

  await page.goto('/register')
  await page.getByLabel('First Name').fill('Jill')
  await page.getByLabel('Last Name').fill('Seeker')
  await page.getByLabel('Email').fill(`seeker-${marker}@example.com`)
  await page.getByLabel('Password').fill('Password123!')
  await page.getByRole('button', { name: 'Register' }).click()
  await expect(page).toHaveURL(/\/jobs$/)
  await page.goto(`/jobs/${jobId}`)
  await expect(page.getByRole('heading', { level: 1, name: jobTitle })).toBeVisible()

  const resumeInput = page.getByLabel(/Resume/)
  const apply = page.getByRole('button', { name: 'Apply for this role' })
  await expect(apply).toBeDisabled()
  await resumeInput.setInputFiles({ name: 'resume.txt', mimeType: 'text/plain', buffer: Buffer.from('bad') })
  await expect(page.getByText('Only PDF and DOCX files are allowed.')).toBeVisible()
  await expect(apply).toBeDisabled()

  await resumeInput.setInputFiles({ name: 'oversize.pdf', mimeType: 'application/pdf', buffer: Buffer.alloc(5 * 1024 * 1024 + 1) })
  await expect(page.getByText('File size must not exceed 5MB.')).toBeVisible()
  await expect(apply).toBeDisabled()

  await resumeInput.setInputFiles({ name: 'resume.docx', mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', buffer: Buffer.from('Pursuit test document') })
  await expect(apply).toBeEnabled()
  await apply.click()
  await expect(page.getByText('Application submitted.')).toBeVisible()

  await page.reload()
  await resumeInput.setInputFiles({ name: 'resume.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4\nPursuit duplicate test') })
  await apply.click()
  await expect(page.getByText('You have already applied to this job.')).toBeVisible()
  await page.goto('/applications')
  await expect(page.getByText(jobTitle)).toHaveCount(1)
})
