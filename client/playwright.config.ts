import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : 2,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:5174',
    trace: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },
    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },
    {
      name: 'mobile-chromium',
      use: { ...devices['Pixel 7'] },
    },
  ],

  webServer: [
    {
      command: 'dotnet run --launch-profile e2e --project ../src/Pursuit.API',
      env: {
        ConnectionStrings__DefaultConnection: process.env.ConnectionStrings__DefaultConnection ?? 'Server=localhost,1435;Database=PursuitDb;User Id=sa;Password=Pursuit@Str0ng2024;TrustServerCertificate=True;',
        JwtSettings__Secret: process.env.JwtSettings__Secret ?? 'pursuit-e2e-local-secret-key-must-be-at-least-32-characters',
        JwtSettings__Issuer: 'Pursuit',
        JwtSettings__Audience: 'PursuitUsers',
        JwtSettings__ExpiryInMinutes: '15',
        JwtSettings__RefreshTokenExpiryInDays: '7',
        RedisSettings__ConnectionString: process.env.RedisSettings__ConnectionString ?? 'localhost:6380',
        RabbitMqSettings__Host: 'localhost',
        RabbitMqSettings__Port: '5673',
        RabbitMqSettings__Username: 'pursuit_e2e',
        RabbitMqSettings__Password: 'pursuit_e2e_pass',
        AzureBlobSettings__ConnectionString: process.env.AzureBlobSettings__ConnectionString ?? 'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10001/devstoreaccount1;',
        AzureBlobSettings__ContainerName: 'resumes',
        AdminBootstrapSettings__Enabled: 'true',
        AdminBootstrapSettings__Email: 'admin.e2e@pursuit.test',
        AdminBootstrapSettings__Password: 'PursuitE2EAdmin1!',
      },
      url: 'http://localhost:5147/health',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
    {
      command: 'node node_modules/vite/bin/vite.js --mode e2e --port 5174',
      url: 'http://localhost:5174',
      reuseExistingServer: !process.env.CI,
    },
  ],
});
