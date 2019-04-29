import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';

@Injectable()
export class HttpClientInterceptor implements HttpInterceptor {
    constructor(private authService: AuthService) { }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const currentUser = this.authService.currentUser$.value;
        const authToken = currentUser && currentUser.token;

        req = req.clone({
            url: `${environment.apiBaseUrl}/${req.url}`,
            setHeaders: {
                'Content-Type': 'application/json; charset=utf-8',
                'Authorization': `Bearer ${authToken}`,
            },
        });

        return next.handle(req);
    }
}