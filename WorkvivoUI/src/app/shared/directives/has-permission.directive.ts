import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';

import { AuthService } from '../../core/services/auth.service';

/**
 * Renders its content only when the user holds one of the given permissions.
 *
 *   <button *appHasPermission="'Post.Create'">New post</button>
 *   <a *appHasPermission="['Post.Moderate', 'Post.Delete']">Moderate</a>
 *
 * Hides, it does not protect: everything it wraps is in the delivered bundle and the
 * endpoint behind it is what actually refuses. Its job is to stop offering people
 * buttons that would fail.
 */
@Directive({
  selector: '[appHasPermission]',
})
export class HasPermissionDirective {
  private readonly auth = inject(AuthService);
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);

  readonly appHasPermission = input.required<string | string[]>();

  private rendered = false;

  constructor() {
    // An effect rather than a one-off check: permissions change when the session is
    // refreshed or restored, and the view has to follow.
    effect(() => {
      const required = this.appHasPermission();
      const permissions = Array.isArray(required) ? required : [required];
      const allowed = this.auth.hasAny(...permissions);

      if (allowed && !this.rendered) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.rendered = true;
      } else if (!allowed && this.rendered) {
        this.viewContainer.clear();
        this.rendered = false;
      }
    });
  }
}
