import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { DocumentCategory, DocumentSummary } from '../../core/models/document';
import { DocumentService } from '../../core/services/document.service';
import { LocaleService } from '../../core/services/locale.service';

/**
 * The document centre.
 *
 * Downloads go through the API rather than a direct link, because the bearer token
 * lives in memory and an anchor would not carry it - and because the audience rules
 * are re-checked at the moment the bytes are served.
 */
@Component({
  selector: 'app-documents-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  templateUrl: './documents-page.html',
  styleUrl: './documents-page.scss',
})
export class DocumentsPage {
  private readonly documents = inject(DocumentService);
  private readonly locale = inject(LocaleService);

  readonly text = this.locale.text;

  readonly categories = signal<DocumentCategory[]>([]);
  readonly items = signal<DocumentSummary[]>([]);
  readonly categoryId = signal<string | null>(null);
  readonly search = signal('');
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly downloading = signal<string | null>(null);

  readonly isEmpty = computed(() => !this.loading() && this.items().length === 0);

  constructor() {
    this.documents.categories().subscribe({
      next: (categories) => this.categories.set(categories),
      error: () => this.categories.set([]),
    });

    this.load();
  }

  choose(categoryId: string | null): void {
    this.categoryId.set(categoryId);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.documents.list(this.categoryId(), this.search()).subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  size(bytes: number | null): string {
    if (!bytes) {
      return '';
    }

    const units = ['B', 'KB', 'MB', 'GB'];
    let value = bytes;
    let unit = 0;

    while (value >= 1024 && unit < units.length - 1) {
      value /= 1024;
      unit++;
    }

    return `${value.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
  }

  download(item: DocumentSummary): void {
    if (!item.isDownloadable || this.downloading()) {
      return;
    }

    this.downloading.set(item.id);

    this.documents.download(item).subscribe({
      next: (blob) => {
        this.downloading.set(null);
        this.save(blob, item.fileName ?? item.title);

        // The count moved on the server; reflecting it here avoids a reload for a
        // number nobody is going to check twice.
        this.items.update((items) =>
          items.map((row) =>
            row.id === item.id ? { ...row, downloadCount: row.downloadCount + 1 } : row,
          ),
        );
      },
      error: () => this.downloading.set(null),
    });
  }

  /**
   * Hands a fetched blob to the browser as a download.
   *
   * The object URL is revoked immediately afterwards - each one pins the whole blob
   * in memory until the tab closes, and a document centre is exactly where somebody
   * downloads thirty files in a sitting.
   */
  private save(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');

    anchor.href = url;
    anchor.download = fileName;
    anchor.click();

    URL.revokeObjectURL(url);
  }
}
