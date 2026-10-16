import {
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';

import { DatePipe } from '@angular/common';

import {
  HttpClient,
  HttpErrorResponse,
} from '@angular/common/http';

import {
  ActivatedRoute,
  RouterLink,
} from '@angular/router';

import {
  FormControl,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';

import {
  takeUntilDestroyed,
} from '@angular/core/rxjs-interop';

import {
  finalize,
  Subject,
  takeUntil,
} from 'rxjs';

import { API_URL } from '../../../core/auth.service';

interface Attachment {
  id: number;
  fileName: string;
}

interface TicketDetails {
  id: number;
  clientName: string;
  isResolvedByEmployee: boolean;
  productName: string;
  problemDescription: string;
  status: string;
  assignedEmployeeId: number | null;
  assignedEmployeeName: string | null;
  isConfirmedByClient: boolean;
  createdAt: string;
  attachments: Attachment[];
}

interface TicketComment {
  id: number;
  authorName: string;
  content: string;
  parentCommentId: number | null;
  createdAt: string;
}

interface CommentPage {
  items: TicketComment[];
  totalCount: number;
  pageNumber: number;
  totalPages: number;
}

@Component({
  selector: 'app-employee-ticket',
  imports: [
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
  ],
  templateUrl: './employee-ticket.html',
  styleUrl: './employee-ticket.css',
})
export class EmployeeTicket {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);

  private readonly cancelRequests = new Subject<void>();

  private id = 0;

  private get url(): string {
    return `${API_URL}/tickets/${this.id}`;
  }

  readonly ticket = signal<TicketDetails | null>(null);
  readonly comments = signal<CommentPage | null>(null);

  readonly loading = signal(false);
  readonly commentsBusy = signal(false);
  readonly saving = signal(false);
  readonly downloading = signal<number | null>(null);

  readonly pageError = signal('');
  readonly commentsError = signal('');
  readonly actionError = signal('');
  readonly success = signal('');

  readonly replyTo = signal<TicketComment | null>(null);

  readonly comment = new FormControl('', {
    nonNullable: true,
    validators: [
      Validators.required,
      Validators.maxLength(5000),
    ],
  });

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        this.cancelRequests.next();

        this.ticket.set(null);
        this.comments.set(null);
        this.replyTo.set(null);
        this.comment.reset();

        this.success.set('');
        this.actionError.set('');
        this.pageError.set('');

        this.id = Number(params.get('id'));

        this.load();
      });
  }

  load(): void {
    if (this.loading()) {
      return;
    }

    if (!Number.isInteger(this.id) || this.id <= 0) {
      this.pageError.set('Invalid ticket number.');
      return;
    }

    this.loading.set(true);
    this.pageError.set('');

    this.http
      .get<TicketDetails>(this.url)
      .pipe(
        takeUntil(this.cancelRequests),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: ticket => {
          this.ticket.set(ticket);
          this.loadComments();
        },

        error: (error: HttpErrorResponse) => {
          this.ticket.set(null);

          this.pageError.set(
            error.status === 404
              ? 'Ticket not found or unavailable to your account.'
              : 'Could not load the ticket. Please try again.'
          );
        },
      });
  }

  loadComments(page = 1): void {
    if (this.commentsBusy()) {
      return;
    }

    this.commentsBusy.set(true);
    this.commentsError.set('');

    this.http
      .get<CommentPage>(`${this.url}/comments`, {
        params: {
          PageNumber: page,
          PageSize: 20,
        },
      })
      .pipe(
        takeUntil(this.cancelRequests),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.commentsBusy.set(false))
      )
      .subscribe({
        next: result => {
          this.comments.set(result);
        },

        error: () => {
          this.commentsError.set(
            'Could not load comments. Please retry.'
          );
        },
      });
  }

  sendComment(): void {
    const content = this.comment.value.trim();

    if (
      !this.ticket() ||
      this.ticket()?.status === 'Closed' ||
      this.saving() ||
      this.commentsBusy()
    ) {
      return;
    }

    this.actionError.set('');
    this.success.set('');

    if (!content || this.comment.invalid) {
      this.actionError.set(
        'Enter a comment between 1 and 5,000 characters.'
      );
      return;
    }

    this.saving.set(true);

    this.http
      .post<TicketComment>(`${this.url}/comments`, {
        content,
        parentCommentId: this.replyTo()?.id ?? null,
      })
      .pipe(
        takeUntil(this.cancelRequests),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false))
      )
      .subscribe({
        next: () => {
          this.comment.reset();
          this.replyTo.set(null);
          this.success.set('Comment sent.');

          const lastPage = Math.max(
            1,
            Math.ceil(
              ((this.comments()?.totalCount ?? 0) + 1) / 20
            )
          );

          this.loadComments(lastPage);
        },

        error: (error: HttpErrorResponse) => {
          this.handleActionError(error);
        },
      });
  }

  updateTicket(
    action: 'Assigned' | 'Paused' | 'resolve' | 'close'
  ): void {
    const ticket = this.ticket();

    if (
      !ticket ||
      ticket.status === 'Closed' ||
      this.saving()
    ) {
      return;
    }

    if (action === ticket.status) {
      return;
    }

    if (
      action === 'resolve' &&
      ticket.isResolvedByEmployee
    ) {
      return;
    }

    if (
      action === 'close' &&
      !ticket.isConfirmedByClient
    ) {
      this.actionError.set(
        'Client confirmation is required before closing.'
      );
      return;
    }

    if (
      action === 'close' &&
      !window.confirm(
        'Close this ticket? This will stop further comments and edits.'
      )
    ) {
      return;
    }

    if (
      action === 'resolve' &&
      !window.confirm(
        'Notify the client that a solution is ready?'
      )
    ) {
      return;
    }

    this.saving.set(true);
    this.actionError.set('');
    this.success.set('');

    const request =
      action === 'Assigned' || action === 'Paused'
        ? this.http.patch<TicketDetails>(
            `${this.url}/status`,
            { status: action }
          )
        : this.http.post<TicketDetails>(
            `${this.url}/${action}`,
            null
          );

    request
      .pipe(
        takeUntil(this.cancelRequests),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false))
      )
      .subscribe({
        next: result => {
          this.ticket.set(result);

          this.success.set(
            action === 'resolve'
              ? 'Solution marked as ready. The client has been notified.'
              : action === 'close'
                ? 'Ticket closed.'
                : 'Ticket status updated.'
          );
        },

        error: (error: HttpErrorResponse) => {
          this.handleActionError(error);
        },
      });
  }

  download(file: Attachment): void {
    if (this.downloading() !== null) {
      return;
    }

    this.downloading.set(file.id);
    this.actionError.set('');

    this.http
      .get(
        `${this.url}/attachments/${file.id}`,
        { responseType: 'blob' }
      )
      .pipe(
        takeUntil(this.cancelRequests),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.downloading.set(null))
      )
      .subscribe({
        next: blob => {
          const objectUrl = URL.createObjectURL(blob);
          const link = document.createElement('a');

          link.href = objectUrl;
          link.download = file.fileName;

          document.body.appendChild(link);
          link.click();
          link.remove();

          setTimeout(
            () => URL.revokeObjectURL(objectUrl),
            1000
          );
        },

        error: () => {
          this.actionError.set(
            'Could not download the attachment. Please try again.'
          );
        },
      });
  }

  private handleActionError(
    error: HttpErrorResponse
  ): void {
    this.actionError.set(
      typeof error.error?.message === 'string'
        ? error.error.message
        : 'The action failed. Please try again.'
    );

    if (error.status === 409 || error.status === 404) {
      this.load();
    }
  }
}