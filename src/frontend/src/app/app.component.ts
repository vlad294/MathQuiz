import { Component } from '@angular/core';
import { AuthService } from './shared/auth/auth.service';
import { BehaviorSubject } from 'rxjs';
import { AuthenticatedUser } from './shared/auth/auth.model';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent {
  public isAuthenticated$: BehaviorSubject<AuthenticatedUser>;

  constructor(authService: AuthService) { 
    this.isAuthenticated$ = authService.currentUser$;
  }
}
