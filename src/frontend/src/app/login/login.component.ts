import { Component } from '@angular/core';
import { FormGroup, FormControl } from '@angular/forms';
import { AuthService } from '../shared/auth/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.less']
})
export class LoginComponent {
  constructor(private readonly authService: AuthService) { }

  form: FormGroup = new FormGroup({
    username: new FormControl('')
  });

  submit() {
    if (this.form.valid) {
      const username = this.form.controls['username'].value;

      this.authService.login(username).subscribe();
    }
  }
}
