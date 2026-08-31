export const environment = {
  production: true,
  // Both `ng serve` (against a locally-run Api) and `docker compose up` map the
  // Api container to this same host port, so one value covers both cases.
  apiBaseUrl: 'http://localhost:5158',
};
