import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { toObservable } from '@angular/core/rxjs-interop';

import { SearchResult, SearchResults } from '../../core/models/search';
import { LocaleService } from '../../core/services/locale.service';
import { SearchService } from '../../core/services/search.service';

/**
 * Search results, grouped by what they are.
 *
 * Grouped rather than interleaved by relevance: without a real scoring model, one
 * merged list orders results by an accident of query order and reads as noise. Five
 * of each, clearly labelled, is more useful and does not pretend to a ranking that
 * does not exist.
 */
@Component({
  selector: 'app-search-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  templateUrl: './search-page.html',
  styleUrl: './search-page.scss',
})
export class SearchPage {
  private readonly search = inject(SearchService);
  private readonly locale = inject(LocaleService);

  /** Bound from ?q= by withComponentInputBinding. */
  readonly q = input<string>('');

  readonly text = this.locale.text;

  private readonly results = toSignal(
    toObservable(this.q).pipe(switchMap((term) => this.search.search(term ?? '', 8))),
    { initialValue: null },
  );

  readonly groups = computed(() => {
    const results = this.results();

    if (!results) {
      return [];
    }

    return [
      { label: this.text().search.people, items: results.employees },
      { label: this.text().search.posts, items: results.posts },
      { label: this.text().search.communities, items: results.communities },
      { label: this.text().search.documents, items: results.documents },
      { label: this.text().search.events, items: results.events },
    ].filter((group) => group.items.length > 0);
  });

  readonly isEmpty = computed(() => {
    const results = this.results();

    return results !== null && results.totalShown === 0 && (this.q() ?? '').trim().length >= 2;
  });

  when(result: SearchResult): string {
    return result.date
      ? new Date(result.date).toLocaleDateString(this.locale.locale(), {
          day: 'numeric',
          month: 'short',
          year: 'numeric',
        })
      : '';
  }
}
