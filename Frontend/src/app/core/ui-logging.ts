import { ErrorHandler, Injectable, inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { API_URL } from './auth.service';

@Injectable({ providedIn: 'root' })
export class UiLogger {
  record(kind: 'http-error' | 'unhandled-error', status?: number): void {
    // Never include request bodies, tokens, URLs, exception messages or user data.
    const entry = { time: new Date().toISOString(), kind, ...(typeof status === 'number' ? { status } : {}) };
    console.error('[Support Portal]', entry);
    try {
      const previous = JSON.parse(sessionStorage.getItem('support-portal-errors') ?? '[]');
      const safeEntries = Array.isArray(previous) ? previous.slice(-49).map(value => ({
        time: typeof value?.time === 'string' ? value.time : '',
        kind: value?.kind === 'http-error' ? 'http-error' : 'unhandled-error',
        ...(typeof value?.status === 'number' ? {status: value.status} : {}),
      })) : [];
      sessionStorage.setItem('support-portal-errors', JSON.stringify([...safeEntries, entry]));
    } catch { /* Logging must never interrupt the application. */ }
  }
}

@Injectable()
export class UiErrorHandler implements ErrorHandler {
  private readonly logger = inject(UiLogger);
  handleError(_error: unknown): void { this.logger.record('unhandled-error'); }
}

export const uiLoggingInterceptor: HttpInterceptorFn = (request, next) => {
  const logger = inject(UiLogger);
  if (!request.url.startsWith(API_URL + '/')) return next(request);
  return next(request).pipe(catchError(error => {
    logger.record('http-error', typeof error?.status === 'number' ? error.status : undefined);
    return throwError(() => error);
  }));
};
