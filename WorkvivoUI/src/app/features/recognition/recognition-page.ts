import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { PersonSummary } from '../../core/models/employee';
import {
  Leaderboard,
  LeaderboardPeriod,
  Recognition,
  RecognitionType,
  RecognitionVisibility,
} from '../../core/models/recognition';
import { EmployeeService } from '../../core/services/employee.service';
import { LocaleService } from '../../core/services/locale.service';
import { RecognitionService } from '../../core/services/recognition.service';
import { Avatar } from '../../shared/components/avatar/avatar';

/**
 * The recognition wall, the leaderboard, and the form for adding to both.
 *
 * One page rather than three routes: recognising somebody is prompted by seeing
 * recognition, and separating them turns a social feature into an admin task.
 */
@Component({
  selector: 'app-recognition-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, Avatar],
  templateUrl: './recognition-page.html',
  styleUrl: './recognition-page.scss',
})
export class RecognitionPage {
  private readonly recognition = inject(RecognitionService);
  private readonly employees = inject(EmployeeService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;
  readonly visibilities = RecognitionVisibility;

  readonly periods: LeaderboardPeriod[] = [
    LeaderboardPeriod.Month,
    LeaderboardPeriod.Quarter,
    LeaderboardPeriod.Year,
    LeaderboardPeriod.AllTime,
  ];

  readonly types = signal<RecognitionType[]>([]);
  readonly wall = signal<Recognition[]>([]);
  readonly board = signal<Leaderboard | null>(null);
  readonly period = signal<LeaderboardPeriod>(LeaderboardPeriod.Month);
  readonly loading = signal(true);

  // Composer state.
  readonly recipientQuery = signal('');
  readonly recipient = signal<PersonSummary | null>(null);
  readonly typeId = signal<string | null>(null);
  readonly message = signal('');
  readonly visibility = signal<RecognitionVisibility>(RecognitionVisibility.Public);
  readonly sending = signal(false);
  readonly sent = signal(false);
  readonly error = signal<string | null>(null);

  /**
   * Suggestions for the recipient box.
   *
   * Debounced and distinct: a keystroke-per-request search against the directory is
   * the easiest way to make a name box feel slow and a server busy.
   */
  readonly suggestions = toSignal(
    toObservable(this.recipientQuery).pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap((term) => this.employees.suggest(term)),
    ),
    { initialValue: [] as PersonSummary[] },
  );

  readonly canSend = computed(
    () =>
      this.recipient() !== null &&
      this.typeId() !== null &&
      this.message().trim().length >= 10 &&
      !this.sending(),
  );

  constructor() {
    this.recognition.types().subscribe({
      next: (types) => {
        this.types.set(types);
        this.typeId.set(types[0]?.id ?? null);
      },
    });

    this.loadWall();
    this.loadBoard();
  }

  setPeriod(period: LeaderboardPeriod): void {
    this.period.set(period);
    this.loadBoard();
  }

  periodLabel(period: LeaderboardPeriod): string {
    return this.text().recognition.periods[period] ?? '';
  }

  choose(person: PersonSummary): void {
    this.recipient.set(person);
    this.recipientQuery.set('');
  }

  clearRecipient(): void {
    this.recipient.set(null);
  }

  send(): void {
    const person = this.recipient();
    const typeId = this.typeId();

    if (!person || !typeId || !this.canSend()) {
      return;
    }

    this.sending.set(true);
    this.error.set(null);

    this.recognition
      .give(person.id, typeId, this.message().trim(), this.visibility())
      .subscribe({
        next: () => {
          this.sending.set(false);
          this.sent.set(true);
          this.message.set('');
          this.recipient.set(null);

          // Both views change: the wall gains a row and the leaderboard's totals move.
          this.loadWall();
          this.loadBoard();
        },
        error: (response) => {
          this.sending.set(false);
          this.error.set(response?.error?.message ?? this.text().recognition.sendFailed);
        },
      });
  }

  when(iso: string): string {
    return new Date(iso).toLocaleDateString(this.locale.locale(), {
      day: 'numeric',
      month: 'short',
    });
  }

  private loadWall(): void {
    this.loading.set(true);

    this.recognition.wall(null).subscribe({
      next: (page) => {
        this.wall.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private loadBoard(): void {
    this.recognition.leaderboard(this.period()).subscribe({
      next: (board) => this.board.set(board),
      error: () => this.board.set(null),
    });
  }
}
