import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';

import { AnalyticsDashboard, Metric } from '../../core/models/analytics';
import { AnalyticsService } from '../../core/services/analytics.service';
import { LocaleService } from '../../core/services/locale.service';

/**
 * The engagement dashboard.
 *
 * Deliberately five numbers and one chart rather than a wall of tiles. A dashboard
 * that shows everything is read as decoration; these are the measures somebody
 * running internal communications would actually act on.
 */
@Component({
  selector: 'app-analytics-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './analytics-page.html',
  styleUrl: './analytics-page.scss',
})
export class AnalyticsPage {
  private readonly analytics = inject(AnalyticsService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;
  readonly windows = [7, 30, 90];

  readonly days = signal(30);
  readonly dashboard = signal<AnalyticsDashboard | null>(null);
  readonly loading = signal(true);
  readonly failed = signal(false);

  /** The tallest bar in the chart, so the others can be drawn relative to it. */
  readonly peak = computed(() => {
    const points = this.dashboard()?.activity ?? [];

    return points.reduce(
      (max, point) => Math.max(max, point.posts + point.comments + point.reactions),
      0,
    );
  });

  constructor() {
    this.load();
  }

  setDays(days: number): void {
    this.days.set(days);
    this.load();
  }

  /**
   * The translated name of a metric.
   *
   * Looked up here rather than in the template: the key arrives from the server as a
   * plain string, and indexing the typed catalogue with one is exactly the case the
   * compiler is right to reject.
   */
  metricLabel(key: string): string {
    const labels = this.text().analytics.metrics as Record<string, string | undefined>;

    return labels[key] ?? key;
  }

  /** Percentage change against the preceding window, or null when there is nothing to compare. */
  change(metric: Metric): number | null {
    if (metric.previous === null || metric.previous === 0) {
      return null;
    }

    return Math.round(((metric.value - metric.previous) / metric.previous) * 100);
  }

  height(value: number): number {
    const peak = this.peak();

    return peak === 0 ? 0 : Math.max(2, Math.round((value / peak) * 100));
  }

  day(iso: string): string {
    return new Date(iso).toLocaleDateString(this.locale.locale(), {
      day: 'numeric',
      month: 'short',
    });
  }

  private load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.analytics.dashboard(this.days()).subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }
}
