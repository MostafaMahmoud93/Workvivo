import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, of, startWith, switchMap, tap } from 'rxjs';

import { EmployeeService } from '../../../core/services/employee.service';
import { LocaleService } from '../../../core/services/locale.service';
import { DirectoryFilters, EmployeeListItem, PagedResult } from '../../../core/models/employee';
import { Avatar } from '../../../shared/components/avatar/avatar';

const EMPTY_PAGE: PagedResult<EmployeeListItem> = {
  items: [],
  pageNumber: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
  hasPrevious: false,
  hasNext: false,
};

@Component({
  selector: 'app-employee-directory',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, Avatar],
  templateUrl: './employee-directory.html',
  styleUrl: './employee-directory.scss',
})
export class EmployeeDirectory {
  private readonly employees = inject(EmployeeService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;

  readonly search = signal('');
  readonly departmentId = signal('');
  readonly locationId = signal('');
  readonly pageNumber = signal(1);

  readonly loading = signal(true);
  readonly failed = signal(false);

  readonly lookups = toSignal(
    this.employees.getLookups().pipe(catchError(() => of(null))),
    { initialValue: null },
  );

  private readonly filters = computed<DirectoryFilters>(() => ({
    search: this.search(),
    departmentId: this.departmentId(),
    locationId: this.locationId(),
    pageNumber: this.pageNumber(),
    pageSize: 12,
  }));

  readonly page = toSignal(
    toObservable(this.filters).pipe(
      // Debounced because the search box writes to the signal on every keystroke.
      // distinctUntilChanged stops a repeated filter object re-issuing the same query.
      debounceTime(250),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
      tap(() => {
        this.loading.set(true);
        this.failed.set(false);
      }),
      // switchMap, so a slow response for an earlier keystroke cannot land after a
      // newer one and show results for a term the user has already changed.
      switchMap((filters) =>
        this.employees.getDirectory(filters).pipe(
          catchError(() => {
            this.failed.set(true);
            return of(EMPTY_PAGE);
          }),
        ),
      ),
      // Inside the pipeline rather than in a separate subscription: an extra
      // subscribe() in the constructor is an unmanaged stream that outlives the
      // component, and toObservable outside an injection context is an NG0203.
      tap(() => this.loading.set(false)),
      startWith(EMPTY_PAGE),
    ),
    { initialValue: EMPTY_PAGE },
  );

  readonly isEmpty = computed(
    () => !this.loading() && !this.failed() && this.page().totalCount === 0,
  );

  /** Any filter change returns to page one - page 7 of a new filter is meaningless. */
  onFilterChange(): void {
    this.pageNumber.set(1);
  }

  goToPage(page: number): void {
    this.pageNumber.set(page);
  }

  clearFilters(): void {
    this.search.set('');
    this.departmentId.set('');
    this.locationId.set('');
    this.pageNumber.set(1);
  }

  readonly skeletons = Array.from({ length: 6 });
}
