import {
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';

import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';

import {
  takeUntilDestroyed,
} from '@angular/core/rxjs-interop';

import { finalize } from 'rxjs';

import {
  API_URL,
  AuthService,
} from '../../../core/auth.service';

interface Ticket {
  id: number;
  clientName: string;
  productName: string;
  problemDescription: string;
  status: string;
  createdAt: string;
}

interface TicketPage {
  items: Ticket[];
  totalCount: number;
  pageNumber: number;
  totalPages: number;
}

@Component({
  selector: 'app-assigned-tickets',
  imports: [DatePipe, RouterLink],

  templateUrl: './assigned-tickets.html',
  styleUrl: './assigned-tickets.css',
})
export class AssignedTickets {
  readonly auth = inject(AuthService);

  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly result = signal<TicketPage | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly page = signal(1);

  constructor() {
    this.load();
  }

  load(page = 1): void {
    if (this.loading()) {
      return;
    }

    this.page.set(page);
    this.loading.set(true);
    this.error.set('');
    this.result.set(null);

    this.http
      .get<TicketPage>(`${API_URL}/tickets/assigned`, {
        params: {
          PageNumber: page,
          PageSize: 20,
        },
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: data => {
          this.result.set(data);
        },

        error: () => {
          this.error.set(
            'Could not load assigned tickets. Please try again.'
          );
        },
      });
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
