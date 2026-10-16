import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import {
  API_URL,
  AuthService,
} from '../../../core/auth.service';

interface Ticket {
  id: number;
  productName: string;
  problemDescription: string;
  status: string;
  assignedEmployeeName: string | null;
  createdAt: string;
}

interface TicketPage {
  items: Ticket[];
  totalCount: number;
  pageNumber: number;
  totalPages: number;
}

@Component({
  selector: 'app-my-tickets',
  imports: [DatePipe, RouterLink],
  templateUrl: './my-tickets.html',
  styleUrl: './my-tickets.css',
})
export class MyTickets {
  readonly auth = inject(AuthService);

  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly result = signal<TicketPage | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly requestedPage = signal(1);

  constructor() {
    this.load();
  }

  load(page = 1): void {
    if (this.loading()) {
      return;
    }

    this.requestedPage.set(page);
    this.loading.set(true);
    this.error.set('');
    this.result.set(null);

    this.http
      .get<TicketPage>(`${API_URL}/tickets/my`, {
        params: {
          PageNumber: page,
          PageSize: 20,
        },
      })
      .pipe(
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: result => {
          this.result.set(result);
        },

        error: () => {
          this.error.set(
            'Could not load your tickets. Please try again.'
          );
        },
      });
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}