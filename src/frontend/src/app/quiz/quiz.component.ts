import { Component, OnInit } from '@angular/core';
import { QuizService } from './quiz.service';
import { AuthService } from '../shared/auth/auth.service';
import { BehaviorSubject } from 'rxjs';
import { MathChallenge, User, HistoryEntry } from './quiz.model';
import { AuthenticatedUser } from '../shared/auth/auth.model';
import { QuizHubService } from './quiz-hub.service';
import { QuizHubEvent } from './quiz-hub.model';

@Component({
  selector: 'app-quiz',
  templateUrl: './quiz.component.html',
  styleUrls: ['./quiz.component.less']
})
export class QuizComponent implements OnInit {
  readonly challenge$: BehaviorSubject<MathChallenge> = new BehaviorSubject<MathChallenge>(null);
  readonly history$: BehaviorSubject<HistoryEntry[]> = new BehaviorSubject<HistoryEntry[]>([]);
  readonly users$: BehaviorSubject<User[]> = new BehaviorSubject<User[]>([]);
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
        this.quizHubService
          .on(QuizHubEvent.UserConnected, this.onUserConnected.bind(this))
          .on(QuizHubEvent.UserDisconnected, this.onUserDisconnected.bind(this))
          .on(QuizHubEvent.ChallengeUpdated, this.onChallengeUpdated.bind(this))
          .on(QuizHubEvent.ChallengeFinished, this.onChallengeFinished.bind(this))
          .on(QuizHubEvent.UserScoreUpdated, this.onUserScoreUpdated.bind(this));
      });
    });
  }

  exit(): void {
    this.quizService.exit().subscribe(() => {
      this.authService.signout();
    });
  }

  canSendAnswer(): boolean {
    return this.challenge$.value
      && !this.challenge$.value.isCompleted;
  }

  sendAnswer(answer: boolean): void {
    const connection = this.quizHubService.connection;

    connection.send('SendAnswer', answer);
  }

  private onChallengeUpdated(question: string) {
    const history = this.history$.value;
    const challenge = this.challenge$.value;

    if (challenge && challenge.question === question) {
      return;
    }

    this.challenge$.next({
      question: question,
      isCompleted: false
    } as MathChallenge);

    history.unshift({
      timestamp: new Date(),
      message: `New challenge '${question}' started`
    } as HistoryEntry);
    this.history$.next(history);
  }

  private onChallengeFinished() {
    const challenge = this.challenge$.value;

    challenge.isCompleted = true;
    this.challenge$.next(challenge);
  }

  private onUserScoreUpdated(username: string, score: number) {
    let users = this.users$.value;
    const history = this.history$.value;
    const userIndex = users.findIndex(x => x.username == username)

    if (userIndex !== -1) {
      users[userIndex].score = score;
      this.users$.next(users);

      history.unshift({
        timestamp: new Date(),
        message: `${username} score updated to ${score}`
      } as HistoryEntry);
      this.history$.next(history);
    }
  }

  private onUserConnected(username: string) {
    const users = this.users$.value;
    const history = this.history$.value;

    if (users.findIndex(x => x.username == username) === -1) {
      users.push({
        username: username,
        score: 0
      } as User);
      this.users$.next(users);

      history.unshift({
        timestamp: new Date(),
        message: `User ${username} connected`
      } as HistoryEntry);
      this.history$.next(history);
    }
  }

  private onUserDisconnected(username: string) {
    let users = this.users$.value;
    const history = this.history$.value;
    const userIndex = users.findIndex(x => x.username == username)

    if (userIndex !== -1) {
      users = users.splice(userIndex, 1);
      this.users$.next(users);

      history.unshift({
        timestamp: new Date(),
        message: `User ${username} disconnected`
      } as HistoryEntry);
      this.history$.next(history);
    }
  }
}
