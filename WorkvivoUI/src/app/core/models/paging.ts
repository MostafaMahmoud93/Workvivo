/**
 * Mirrors Workvivo.Application.Common.Paging.PagedResult<T> - offset paging, with a
 * total.
 *
 * It lived in `employee.ts` while the directory was the only thing that paged. Now
 * that communities and the admin screens use it too, it belongs in a file of its own
 * rather than every feature importing from an unrelated model.
 */
export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}
