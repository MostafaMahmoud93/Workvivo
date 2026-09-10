import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Initials in a coloured disc.
 *
 * A placeholder until file storage serves real pictures. The colour is derived from
 * the name rather than random, so the same person is the same colour on every screen
 * and across reloads - which is what makes an avatar scannable in a list.
 */
@Component({
  selector: 'app-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      class="avatar"
      [style.width.px]="size()"
      [style.height.px]="size()"
      [style.background]="background()"
      [style.font-size.px]="size() * 0.38"
      [attr.aria-hidden]="true"
      >{{ initials() }}</span
    >
  `,
  styles: [
    `
      .avatar {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        flex: none;
        border-radius: 50%;
        color: #fff;
        font-weight: 600;
        letter-spacing: 0.02em;
        user-select: none;
      }
    `,
  ],
})
export class Avatar {
  readonly name = input.required<string>();
  readonly size = input(40);

  readonly initials = computed(() => {
    const parts = this.name().trim().split(/\s+/).filter(Boolean);

    if (parts.length === 0) {
      return '?';
    }

    // First and last, so "Nadia Al Rashid" reads NR rather than NA.
    const first = parts[0]![0] ?? '';
    const last = parts.length > 1 ? (parts[parts.length - 1]![0] ?? '') : '';

    return (first + last).toUpperCase();
  });

  readonly background = computed(() => {
    // A stable hash of the name picks the hue. Saturation and lightness are fixed at
    // values that keep white text above the contrast threshold for every hue.
    let hash = 0;
    for (const character of this.name()) {
      hash = (hash * 31 + character.charCodeAt(0)) | 0;
    }

    return `hsl(${Math.abs(hash) % 360} 45% 38%)`;
  });
}
