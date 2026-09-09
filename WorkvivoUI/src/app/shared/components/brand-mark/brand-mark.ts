import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-brand-mark',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="mark" [class.mark--lg]="size() === 'lg'">
      <span class="mark__glyph" aria-hidden="true">WV</span>
      <span class="mark__word">Workvivo</span>
    </span>
  `,
  styleUrl: './brand-mark.scss',
})
export class BrandMark {
  readonly size = input<'sm' | 'lg'>('sm');
}
