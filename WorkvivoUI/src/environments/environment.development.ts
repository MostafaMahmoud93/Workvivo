export const environment = {
  production: false,
  /**
   * Empty on purpose: proxy.conf.json forwards /api to the .NET API on
   * https://localhost:7037, so the browser only ever talks to ng serve.
   */
  apiBaseUrl: '',
};
