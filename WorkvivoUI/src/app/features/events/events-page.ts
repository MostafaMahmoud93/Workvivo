import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import { CalendarEvent, EventFormat, EventResponse } from '../../core/models/event';
import { EventService } from '../../core/services/event.service';
import { LocaleService } from '../../core/services/locale.service';

/**
 * The events people are invited to, soonest first.
 *
 * Grouped by day rather than shown as a flat list: a calendar's job is to answer
 * "what is happening on Thursday", and a list of twenty rows with repeated dates
 * answers it worse than a heading does.
 */
@Component({
  selector: 'app-events-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './events-page.html',
  styleUrl: './events-page.scss',
})
export class EventsPage {
  private readonly events = inject(EventService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;
  readonly responses = EventResponse;
  readonly formats = EventFormat;

  readonly items = signal<CalendarEvent[]>([]);
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly busy = signal<string | null>(null);

  readonly days = computed(() => {
    const groups = new Map<string, CalendarEvent[]>();

    for (const item of this.items()) {
      // Grouped by the viewer's local day, which is what a person means by
      // "Thursday" - the server's day would put a late-evening Gulf event on the
      // wrong side of midnight for anybody west of it.
      const key = new Date(item.startAt).toLocaleDateString(this.locale.locale(), {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
      });

      groups.set(key, [...(groups.get(key) ?? []), item]);
    }

    return [...groups.entries()].map(([label, events]) => ({ label, events }));
  });

  readonly isEmpty = computed(() => !this.loading() && this.items().length === 0);

  constructor() {
    this.load();
  }

  time(item: CalendarEvent): string {
    if (item.isAllDay) {
      return this.text().events.allDay;
    }

    const options: Intl.DateTimeFormatOptions = { hour: '2-digit', minute: '2-digit' };

    return (
      new Date(item.startAt).toLocaleTimeString(this.locale.locale(), options) +
      ' - ' +
      new Date(item.endAt).toLocaleTimeString(this.locale.locale(), options)
    );
  }

  where(item: CalendarEvent): string {
    if (item.format === EventFormat.Online) {
      return this.text().events.online;
    }

    return item.locationName ?? item.address ?? this.text().events.locationTbc;
  }

  respond(item: CalendarEvent, response: EventResponse): void {
    if (!item.canRsvp || this.busy()) {
      return;
    }

    this.busy.set(item.id);

    this.events.rsvp(item.id, response).subscribe({
      next: () => {
        this.busy.set(null);

        // Reloaded rather than patched: the joining link is released by the server
        // once somebody is attending, and only the server knows that.
        this.load();
      },
      error: () => this.busy.set(null),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.events.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }
}
