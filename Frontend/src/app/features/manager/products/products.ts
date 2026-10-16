import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { API_URL, AuthService } from '../../../core/auth.service';

interface Product { id: number; name: string; }

@Component({
  selector: 'app-manager-products',
  imports: [RouterLink],
  templateUrl: './products.html',
  styleUrls: ['../dashboard/dashboard.css', './products.css'],
})
export class ManagerProducts {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly products = signal<Product[] | null>(null);
  readonly search = signal('');
  readonly loading = signal(false);
  readonly error = signal('');
  readonly filtered = computed(() => {
    const term = this.search().trim().toLocaleLowerCase();
    return (this.products() ?? []).filter(product => product.name.toLocaleLowerCase().includes(term));
  });

  constructor() { this.load(); }

  load(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.http.get<Product[]>(API_URL + '/products').pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.loading.set(false)),
    ).subscribe({
      next: products => this.products.set(products),
      error: () => this.error.set('Could not refresh systems. Please try again. Previously loaded systems, if any, are shown below.'),
    });
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
