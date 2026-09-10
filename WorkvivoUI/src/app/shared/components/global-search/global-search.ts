import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { LocaleService } from '../../../core/services/locale.service';
import { MIN_SEARCH_LENGTH } from '../../../core/services/search.service';

/**
 * The search box in the header.
 *
 * Navigates rather than showing a dropdown of results. A type-ahead over five
 * content types is a request per keystroke against every table in the product; the
 * results page does the same work once, when somebody has finished typing.
 */
@Component({
  selector: 'app-global-search',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <form class="global-search" (submit)="$event.preventDefault(); go()">
      <input
        type="search"
        name="q"
        autocomplete="off"
        [attr.aria-label]="text().search.heading"
        [placeholder]="text().search.placeholder"
        [ngModel]="term()"
        (ngModelChange)="term.set($event)"
      />
    </form>
  `,
  styles: [
    `
      .global-search input {
        width: 12rem;
        padding: 0.35rem 0.65rem;
        border: 1px solid var(--wv-line);
        border-radius: 999px;
        background: var(--wv-surface-sunken);
        font: inherit;
        font-size: 0.85rem;

        &:focus {
          width: 16rem;
          background: var(--wv-surface);
          outline: 2px solid var(--wv-focus);
          outline-offset: 1px;
        }
      }

      @media (max-width: 52rem) {
        .global-search input,
        .global-search input:focus {
          width: 8rem;
        }
      }
    `,
  ],
})
export class GlobalSearch {
  private readonly router = inject(Router);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;
  readonly term = signal('');

  go(): void {
    const value = this.term().trim();

    if (value.length < MIN_SEARCH_LENGTH) {
      return;
    }

    void this.router.navigate(['/search'], { queryParams: { q: value } });
  }
}
