import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthenticatedUser } from './auth.model';

@Injectable()
export class AuthService {
    public currentUser$: BehaviorSubject<AuthenticatedUser>;

    constructor(private http: HttpClient) {
        const localStorageData = JSON.parse(localStorage.getItem('currentUser'));

        this.currentUser$ = new BehaviorSubject<AuthenticatedUser>(localStorageData);
    }

    login(username: string): Observable<void> {
        const params = new HttpParams()
            .set("username", username);

        return this.http.post('auth/token', null, { params: params, responseType: 'text' })
            .pipe(map(token => {
                if (token) {
                    const user = new AuthenticatedUser(username, token);

                    localStorage.setItem('currentUser', JSON.stringify(user));
                    this.currentUser$.next(user);
                }
            }));
    }

    logout() {
        // remove user from local storage to log user out
        localStorage.removeItem('currentUser');
        this.currentUser$.next(null);
    }
}