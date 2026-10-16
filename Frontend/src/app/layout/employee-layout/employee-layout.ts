import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, filter, finalize, map, of, switchMap, timer } from 'rxjs';
import { API_URL, AuthService } from '../../core/auth.service';

interface NotificationItem {
  id: number;
  ticketId: number | null;
  message: string;
  isRead: boolean;
  createdAt: string;
}
interface NotificationPage {
  items: NotificationItem[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
}

@Component({
  selector: 'app-employee-layout',
  imports: [DatePipe, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './employee-layout.html',
  styleUrl: './employee-layout.css',
})
export class EmployeeLayout {
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private countVersion = 0;
  readonly avatarFailed = signal(false);
  readonly accountPhoto = signal<string | null>(null);
  readonly notificationNotice = signal('');
  readonly opened = signal(false);
  readonly unread = signal<number | null>(null);
  readonly result = signal<NotificationPage | null>(null);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');

  constructor() {
    const accountId = this.auth.user()?.userId;
    if (accountId) {
      this.http.get(API_URL + "/users/" + accountId + "/image", { responseType: "blob" })
        .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: blob => {
            if (blob.type !== "image/png" && blob.type !== "image/jpeg") return;
            this.accountPhoto.set(URL.createObjectURL(blob));
          },
          error: () => {},
        });
    }
    this.destroyRef.onDestroy(() => { const photo = this.accountPhoto(); if (photo) URL.revokeObjectURL(photo); });
    timer(0, 30000).pipe(
      filter(() => !this.busy()),
      switchMap(() => {
        const version = ++this.countVersion;
        return this.http.get<{ unreadCount: number }>(API_URL + '/notifications/unread-count').pipe(
          catchError(() => of(null)),
          map(value => ({ value, version }))
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(({ value, version }) => {
      if (version !== this.countVersion) return;
      const previous = this.unread();
      this.unread.set(value?.unreadCount ?? null);
      if (value && previous !== null && value.unreadCount > previous) {
        this.notificationNotice.set('New notifications are available.');
      }
      if (this.opened() && !this.loading() && !this.busy()) {
        this.load(this.result()?.pageNumber ?? 1, false);
      }
    });

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(() => this.opened.set(false));
  }

  toggle(): void {
    this.opened.update(value => !value);
    if (this.opened()) { this.notificationNotice.set(''); this.load(); }
  }

  load(page = 1, updateCount = true): void {
    if (this.loading() || this.busy()) return;
    this.loading.set(true);
    this.error.set('');
    this.http.get<NotificationPage>(`${API_URL}/notifications`, {
      params: { PageNumber: page, PageSize: 10 },
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.loading.set(false)),
    ).subscribe({
      next: result => { this.result.set(result); if (updateCount) this.refreshCount(); },
      error: () => this.error.set('Could not load notifications. Please refresh.'),
    });
  }

  openNotification(item: NotificationItem): void {
    if (this.busy() || this.loading()) return;
    if (item.isRead) { this.goToTicket(item.ticketId); return; }
    ++this.countVersion;
    this.busy.set(true);
    this.error.set('');
    this.http.patch(`${API_URL}/notifications/${item.id}/read`, null).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.busy.set(false)),
    ).subscribe({
      next: () => {
        this.result.update(page => page ? { ...page, items: page.items.map(row => row.id === item.id ? { ...row, isRead: true } : row) } : null);
        this.refreshCount();
        this.goToTicket(item.ticketId);
      },
      error: () => this.error.set('Could not mark this notification as read. Try again.'),
    });
  }

  markAllRead(): void {
    if (this.busy() || this.loading()) return;
    ++this.countVersion;
    this.busy.set(true);
    this.error.set('');
    this.http.patch(`${API_URL}/notifications/read-all`, null).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.busy.set(false)),
    ).subscribe({
      next: () => {
        this.result.update(page => page ? { ...page, items: page.items.map(item => ({ ...item, isRead: true })) } : null);
        this.refreshCount();
      },
      error: () => this.error.set('Could not update notifications. Try again.'),
    });
  }

  private refreshCount(): void {
    const version = ++this.countVersion;
    this.http.get<{ unreadCount: number }>(`${API_URL}/notifications/unread-count`).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: value => { if (version === this.countVersion) this.unread.set(value.unreadCount); },
      error: () => { if (version === this.countVersion) this.unread.set(null); },
    });
  }

  private goToTicket(id: number | null): void {
    if (!id) return;
    this.opened.set(false);
    void this.router.navigate(['/assigned-tickets', id]);
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}




