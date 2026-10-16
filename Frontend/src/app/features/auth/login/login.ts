import {
  Component,
  inject,
  signal,
} from '@angular/core';

import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';

import { finalize } from 'rxjs';

import {
  AuthService,
  LoginResponse,
} from '../../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly busy = signal(false);
  readonly showPassword = signal(false);
  readonly error = signal('');

  readonly signedInUser =
    signal<LoginResponse | null>(null);

  readonly form = this.fb.nonNullable.group({
    usernameOrEmail: ['', Validators.required],
    password: ['', Validators.required],
  });

  submit(): void {
    if (this.busy()) {
      return;
    }

    this.error.set('');
    this.signedInUser.set(null);

    const values = this.form.getRawValue();
    const usernameOrEmail = values.usernameOrEmail.trim();

    if (this.form.invalid || !usernameOrEmail) {
      this.form.markAllAsTouched();

      this.error.set(
        'Enter your username or email and password.'
      );

      return;
    }

    this.busy.set(true);

    this.auth
      .login(usernameOrEmail, values.password)
      .pipe(
        finalize(() => this.busy.set(false))
      )
      .subscribe({
        next: user => {
          this.signedInUser.set(user);
          this.form.controls.password.reset();

          if (user.role === 'SupportManager') {
            void this.router.navigateByUrl('/manager/tickets');
          } else if (user.role === 'Client') {
            void this.router.navigateByUrl('/my-tickets');
          } else if (user.role === 'SupportEmployee') {
            void this.router.navigateByUrl('/assigned-tickets');
          }
        },

        error: (response: HttpErrorResponse) => {
          const message =
            response.status === 401
              ? 'Incorrect username, email or password.'
              : response.status === 403
                ? 'Your account is inactive. Contact your support manager.'
                : response.status === 0
                  ? 'Cannot reach the server. Check that the backend is running.'
                  : 'Unable to sign in. Please try again.';

          this.error.set(message);
        },
      });
  }
}

