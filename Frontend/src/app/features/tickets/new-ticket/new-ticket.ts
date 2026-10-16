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

import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { API_URL } from '../../../core/auth.service';

interface Product {
  id: number;
  name: string;
}

@Component({
  selector: 'app-new-ticket',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './new-ticket.html',
  styleUrl: './new-ticket.css',
})
export class NewTicket {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly products = signal<Product[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);

  readonly productsError = signal('');
  readonly error = signal('');
  readonly filesError = signal('');

  readonly files = signal<File[]>([]);
  readonly createdId = signal<number | null>(null);

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
    this.loadProducts();
  }

  loadProducts(): void {
    if (this.loading()) {
      return;
    }

    this.loading.set(true);
    this.productsError.set('');

    this.http
      .get<Product[]>(`${API_URL}/products`)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: products => {
          this.products.set(products);
        },

        error: () => {
          this.productsError.set(
            'Could not load products. Please try again.'
          );
        },
      });
  }

  selectFiles(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = Array.from(input.files ?? []);

    input.value = '';

    if (!selected.length) {
      return;
    }

    this.filesError.set('');

    if (selected.length > 5) {
      this.filesError.set(
        'Choose no more than 5 files. Your previous selection has not changed.'
      );
      return;
    }

    const invalidFile = selected.some(file =>
      file.size === 0 ||
      file.size > 5 * 1024 * 1024 ||
      !/\.(pdf|png|jpe?g)$/i.test(file.name)
    );

    if (invalidFile) {
      this.filesError.set(
        'Use PDF, PNG or JPG files, up to 5 MB each. Empty files are not allowed. Your previous selection has not changed.'
      );
      return;
    }

    this.files.set(selected);
  }

  clearFiles(): void {
    this.files.set([]);
    this.filesError.set('');
  }

  submit(): void {
    if (
      this.saving() ||
      this.loading() ||
      this.createdId() !== null
    ) {
      return;
    }

    this.error.set('');
    this.form.markAllAsTouched();

    const values = this.form.getRawValue();
    const description = values.problemDescription.trim();

    const productExists = this.products().some(
      product => product.id === values.productId
    );

    if (
      this.form.invalid ||
      !description ||
      !productExists
    ) {
      this.error.set(
        'Choose a product and describe the problem using 1 to 5,000 characters.'
      );
      return;
    }

    if (this.filesError()) {
      return;
    }

    const body = new FormData();

    body.append('ProductId', String(values.productId));
    body.append('ProblemDescription', description);

    for (const file of this.files()) {
      body.append('Attachments', file, file.name);
    }

    this.saving.set(true);

    this.http
      .post<{ id: number }>(`${API_URL}/tickets`, body)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false))
      )
      .subscribe({
        next: ticket => {
          this.createdId.set(ticket.id);

          void this.router.navigate([
            '/my-tickets',
            ticket.id,
          ]);
        },

        error: (response: HttpErrorResponse) => {
          this.error.set(
            typeof response.error?.message === 'string'
              ? response.error.message
              : response.status === 0
                ? 'Cannot reach the server. Check your connection before trying again.'
                : 'Could not create the ticket. Check your details and try again.'
          );
        },
      });
  }
}