import { Pipe, PipeTransform, SecurityContext, inject } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

/**
 * Sanitizes HTML through Angular's built-in sanitizer for safe [innerHTML]
 * binding. Never uses bypassSecurityTrustHtml.
 */
@Pipe({
  name: 'safeHtml',
  standalone: true,
})
export class SafeHtmlPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(value: string | null | undefined): string {
    return this.sanitizer.sanitize(SecurityContext.HTML, value ?? '') ?? '';
  }
}
