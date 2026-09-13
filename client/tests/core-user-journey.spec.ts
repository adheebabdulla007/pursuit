import { test, expect } from '@playwright/test';

function unique(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 10000)}`;
}

test('employer posts a job and job seeker applies to it', async ({ page }) => {
  const employerEmail = `${unique('employer')}@example.com`;
  const companyName = unique('Acme Corp');
  const jobTitle = unique('Senior Backend Engineer');
  const jobSeekerEmail = `${unique('seeker')}@example.com`;
  const password = 'Password123!';

  // Employer registers
  await page.goto('/register');
  await page.getByLabel('First Name').fill('Emma');
  await page.getByLabel('Last Name').fill('Employer');
  await page.getByLabel('Email').fill(employerEmail);
  await page.getByLabel('Password').fill(password);
  await page.getByLabel('I am a').selectOption('Employer');
  await page.getByLabel('Company Name').fill(companyName);
  await page.getByRole('button', { name: 'Register' }).click();

  await expect(page).toHaveURL(/\/jobs$/);

  // Employer posts a job
  await page.getByRole('link', { name: 'Post a Job' }).click();
  await expect(page).toHaveURL(/\/jobs\/new$/);

  await page.getByLabel('Title').fill(jobTitle);
  await page.getByLabel('Description').fill('A great opportunity to build production systems.');
  await page.getByLabel('Location').fill('Remote');
  await page.getByLabel('Salary Min').fill('80000');
  await page.getByLabel('Salary Max').fill('120000');
  await page.getByRole('button', { name: 'Post Job' }).click();

  await expect(page).toHaveURL(/\/jobs\/[0-9a-fA-F-]+$/);
  await expect(page.getByRole('heading', { level: 1, name: jobTitle })).toBeVisible();

  // Employer logs out
  await page.getByRole('button', { name: 'Logout' }).click();
  await expect(page).toHaveURL(/\/login$/);

  // Job seeker registers
  await page.goto('/register');
  await page.getByLabel('First Name').fill('Jill');
  await page.getByLabel('Last Name').fill('Seeker');
  await page.getByLabel('Email').fill(jobSeekerEmail);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Register' }).click();

  await expect(page).toHaveURL(/\/jobs$/);

  // Search for the exact job just created
  await page.getByLabel('Keyword').fill(jobTitle);
  await page.getByRole('button', { name: 'Search' }).click();

  const jobLink = page.getByRole('link', { name: new RegExp(jobTitle) });
  await expect(jobLink).toBeVisible();
  await jobLink.click();

  await expect(page).toHaveURL(/\/jobs\/[0-9a-fA-F-]+$/);
  await expect(page.getByRole('heading', { level: 1, name: jobTitle })).toBeVisible();

  // Upload resume and apply
  await page.locator('#resume').setInputFiles({
    name: 'resume.pdf',
    mimeType: 'application/pdf',
    buffer: Buffer.from('%PDF-1.4 fake resume content for e2e test'),
  });

  await page.getByRole('button', { name: 'Apply' }).click();

  await expect(page.getByText('Application submitted.')).toBeVisible();
});