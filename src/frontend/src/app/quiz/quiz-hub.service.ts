import { Injectable } from '@angular/core';
import { HubConnectionBuilder, HubConnection, HttpTransportType, LogLevel } from '@aspnet/signalr';
import { environment } from '../../environments/environment';
import { AuthService } from '../shared/auth/auth.service';
import { QuizHubEvent } from './quiz-hub.model';

@Injectable()
export class QuizHubService {
    public connection: HubConnection;

    constructor(private authService: AuthService) {
    }

    async connect(): Promise<void> {
        const connection = new HubConnectionBuilder()
            .withUrl(environment.hubBaseUrl + "/quiz", {
                accessTokenFactory: () => this.authService.currentUser$.value.token,
                logger: LogLevel.Information,
                transport: HttpTransportType.WebSockets,
                skipNegotiation: true
            })
            .build();

        await connection.start()
            .then(() => console.log('Connection started'))
            .catch(err => console.log('Error while starting connection: ' + err))

        this.connection = connection;

        this.connection.on
    }

    on(eventName: QuizHubEvent, action: (...args: any[]) => void): QuizHubService {
        this.connection.on(eventName, action);

        return this;
    }
}