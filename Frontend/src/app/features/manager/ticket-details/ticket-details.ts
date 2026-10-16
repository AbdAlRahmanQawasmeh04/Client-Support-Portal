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
  clientName: string;
  isResolvedByEmployee: boolean;
  id: number;
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
  selector: 'app-manager-ticket-details',
  imports: [
    DatePipe,
    RouterLink,

  ],
  templateUrl: './ticket-details.html',
  styleUrl: './ticket-details.css',
})
export class ManagerTicketDetails {
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

  readonly downloading = signal<number | null>(null);

  readonly pageError = signal('');
  readonly commentsError = signal('');
  readonly actionError = signal('');






  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        this.cancelRequests.next();

        this.ticket.set(null);
        this.comments.set(null);




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

}

