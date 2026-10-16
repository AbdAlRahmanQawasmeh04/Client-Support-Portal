import {
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';

import {
  HttpClient,
  HttpErrorResponse,
} from '@angular/common/http';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';

import {
  ActivatedRoute,
  Router,
  RouterLink,
} from '@angular/router';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, forkJoin } from 'rxjs';

import { API_URL } from '../../../core/auth.service';

interface Product {
  id: number;
  name: string;
}

interface EditableTicket {
  id: number;
  productId: number;
  problemDescription: string;
  status: string;
  isConfirmedByClient: boolean;
}

@Component({
  selector: 'app-edit-ticket',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './edit-ticket.html',
  styleUrl: './edit-ticket.css',
})
export class EditTicket {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly id = Number(
    inject(ActivatedRoute).snapshot.paramMap.get('id')
  );

  readonly ticket = signal<EditableTicket | null>(null);
  readonly products = signal<Product[]>([]);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly saved = signal(false);

  readonly loadError = signal('');
  readonly error = signal('');
  readonly needsReload = signal(false);

  readonly form = this.fb.nonNullable.group({
    productId: [
      0,
      [Validators.required, Validators.min(1)],
    ],
    problemDescription: [
      '',
      [Validators.required, Validators.maxLength(5000)],
    ],
  });

  constructor() {
    this.load();
  }

  load(): void {
    if (this.loading() || this.saving()) {
      return;
    }

    if (!Number.isInteger(this.id) || this.id <= 0) {
      this.loadError.set('Invalid ticket number.');
      return;
    }

    this.loading.set(true);
    this.loadError.set('');
    this.error.set('');

    forkJoin({
      ticket: this.http.get<EditableTicket>(
        `${API_URL}/tickets/${this.id}`
      ),
      products: this.http.get<Product[]>(
        `${API_URL}/products`
      ),
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: ({ ticket, products }) => {
          this.ticket.set(ticket);
          this.products.set(products);

          this.form.reset({
            productId: ticket.productId,
            problemDescription: ticket.problemDescription,
          });

          this.needsReload.set(false);
        },

        error: (response: HttpErrorResponse) => {
          this.ticket.set(null);

          this.loadError.set(
            response.status === 404
              ? 'Ticket not found or unavailable to your account.'
              : 'Could not load the ticket and products. Please try again.'
          );
        },
      });
  }

  submit(): void {
    if (
      !this.ticket() ||
      this.ticket()?.status === 'Closed' ||
      this.loading() ||
      this.saving() ||
      this.saved() ||
      this.needsReload()
    ) {
      return;
    }

    this.error.set('');
    this.form.markAllAsTouched();

    const values = this.form.getRawValue();
    const problemDescription =
      values.problemDescription.trim();

    const productExists = this.products().some(
      product => product.id === values.productId
    );

    if (
      this.form.invalid ||
      !problemDescription ||
      !productExists
    ) {
      this.error.set(
        'Choose a product and enter a description between 1 and 5,000 characters.'
      );
      return;
    }

    this.saving.set(true);

    this.http
      .put<EditableTicket>(
        `${API_URL}/tickets/${this.id}`,
        {
          productId: values.productId,
          problemDescription,
        }
      )
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false))
      )
      .subscribe({
        next: () => {
          this.saved.set(true);

          void this.router.navigate([
            '/my-tickets',
            this.id,
          ]);
        },

        error: (response: HttpErrorResponse) => {
          this.error.set(
            typeof response.error?.message === 'string'
              ? response.error.message
              : 'Could not save your changes. Please try again.'
          );

          if (
            response.status === 409 ||
            response.status === 404
          ) {
            this.needsReload.set(true);
          }
        },
      });
  }
}