import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { environment } from './environments/environment';
import { assertProductionAuthConfigured } from './environments/environment-guard';

// Fail loud before boot if a production bundle still carries placeholder Logto config
// (MAJOR-2, Feature 033) — see environment-guard.ts.
assertProductionAuthConfigured(environment);

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
