import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Quiz } from './quiz.model';

@Injectable()
export class QuizService {
    constructor(private http: HttpClient) {
    }

    start(): Observable<Quiz> {
        return this.http.post<Quiz>('quiz/start', null);
    }

    exit(): Observable<void> {
        return this.http.post<void>('quiz/exit', null);
    }
}