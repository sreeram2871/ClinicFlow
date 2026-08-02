import { Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
})
export class AppShellComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.authService.currentUser;

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
