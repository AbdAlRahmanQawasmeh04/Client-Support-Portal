import { ErrorHandler } from '@angular/core';
import { UiErrorHandler, uiLoggingInterceptor } from './core/ui-logging';
import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';

import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';

import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(), { provide: ErrorHandler, useClass: UiErrorHandler },
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([uiLoggingInterceptor, authInterceptor])
    ),
  ],
};
