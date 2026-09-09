import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { LocaleService } from '../../core/services/locale.service';
import { BrandMark } from '../../shared/components/brand-mark/brand-mark';
import { LanguageToggle } from '../../shared/components/language-toggle/language-toggle';

@Component({
  selector: 'app-public-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, BrandMark, LanguageToggle],
  templateUrl: './public-page.html',
  styleUrl: './public-page.scss',
})
export class PublicPage {
  readonly text = inject(LocaleService).text;
}
