import { Component, DestroyRef, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { API_URL, AuthService } from '../../../core/auth.service';

@Component({
  selector: 'app-password-recovery', imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './password-recovery.html', styleUrl: './password-recovery.css',
})
export class PasswordRecovery {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  readonly reset = this.route.snapshot.data['reset'] === true;
  private token = new URLSearchParams(this.route.snapshot.fragment ?? '').get('token') ?? '';
  readonly hasToken = signal(this.token.length > 0 && this.token.length <= 4096);
  readonly busy = signal(false);
  readonly done = signal(false);
  readonly error = signal('');
  readonly showPassword = signal(false);
  readonly emailForm = this.fb.nonNullable.group({ email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]] });
  readonly passwordForm = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(128)]],
    confirmPassword: ['', Validators.required],
  });

  constructor() {
    if (this.reset) inject(Location).replaceState('/reset-password');
    this.destroyRef.onDestroy(() => { this.token = ''; });
  }

  submit(): void {
    if (this.busy() || this.done()) return;
    this.error.set('');
    const form = this.reset ? this.passwordForm : this.emailForm;
    form.markAllAsTouched();
    if (form.invalid) {
      this.error.set(this.reset ? 'Use a password containing 8 to 128 characters and confirm it.' : 'Enter a valid email address.');
      return;
    }
    if (this.reset && !this.hasToken()) return;
    const passwords = this.passwordForm.getRawValue();
    if (this.reset && passwords.password !== passwords.confirmPassword) {
      this.error.set('The passwords do not match.'); return;
    }
    this.busy.set(true);
    const body = this.reset ? { token: this.token, ...passwords } : { email: this.emailForm.controls.email.value.trim() };
    this.http.post(API_URL + (this.reset ? '/auth/reset-password' : '/auth/forgot-password'), body)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.done.set(true);
          if (this.reset) { this.token = ''; this.passwordForm.reset(); this.auth.logout(); }
        },
        error: (response: HttpErrorResponse) => {
          const fallback = response.status === 0 ? 'Cannot reach the server. Please try again.' :
            response.status === 429 ? 'Too many attempts. Try again in 15 minutes.' :
            response.status === 503 ? 'Email recovery is temporarily unavailable. Contact your support manager.' :
            this.reset && response.status === 400 ? 'This link is invalid, expired or already used. Request a new link.' :
            'Could not complete your request. Please try again.';
          this.error.set(fallback);
        },
      });
  }
}
