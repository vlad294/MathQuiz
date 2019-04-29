import { Component, OnInit } from '@angular/core';
import { QuizService } from './quiz.service';
import { AuthService } from '../shared/auth/auth.service';
import { BehaviorSubject } from 'rxjs';
import { MathChallenge, User, HistoryEntry } from './quiz.model';
import { AuthenticatedUser } from '../shared/auth/auth.model';
import { QuizHubService } from './quiz-hub.service';

@Component({
  selector: 'app-quiz',
  templateUrl: './quiz.component.html',
  styleUrls: ['./quiz.component.less']
})
export class QuizComponent implements OnInit {
  readonly challenge$: BehaviorSubject<MathChallenge> = new BehaviorSubject<MathChallenge>(null);
  readonly users$: BehaviorSubject<User[]> = new BehaviorSubject<User[]>([]);
  readonly history$: BehaviorSubject<HistoryEntry[]> = new BehaviorSubject<HistoryEntry[]>([]);
  readonly authenticatedUser$: BehaviorSubject<AuthenticatedUser>;

  constructor(
    private quizService: QuizService,
    private authService: AuthService,
    private quizHubService: QuizHubService) {
    this.authenticatedUser$ = authService.currentUser$;
  }

  ngOnInit(): void {
    this.quizService.start().subscribe(quiz => {
      this.challenge$.next(quiz.challenge);
      this.users$.next(quiz.users);
      this.quizHubService.connect().then(() => {
        this.subscribe();
      });
    });
  }

  exit(): void {
    this.quizService.exit().subscribe(() => {
      this.authService.logout();
    });
  }

  private subscribe() {
    const connection = this.quizHubService.connection;
    connection.on("ChallengeUpdated", (question: string) => {
      this.challenge$.next({
        question: question,
        isCompleted: false
      } as MathChallenge);
    });

    connection.on("UserScoreUpdated", (username, score) => {
      let users = this.users$.value;
      const userIndex = users.findIndex(x => x.username == username)

      if (userIndex !== -1) {
        users[userIndex].score = score;
        this.users$.next(users);
      }
    });

    connection.on("UserConnected", (username: string) => {
      const users = this.users$.value;

      if (users.findIndex(x => x.username == username) === -1) {
        users.push({
          username: username,
          score: 0
        } as User);
        this.users$.next(users);
      }
    });

    connection.on("UserDisconnected", (username: string) => {
      let users = this.users$.value;
      const userIndex = users.findIndex(x => x.username == username)

      if (userIndex !== -1) {
        users = users.splice(userIndex, 1);
        this.users$.next(users);
      }
    });
  }
}
