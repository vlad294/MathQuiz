# MathQuiz
A real-time browser-based math game for up to 10 concurrent users. 
The game is structured as a continuous series of rounds, where all connected players compete to submit the correct answer first. 
The number of rounds is not limited, players can connect at any time and start competing.

## Getting Started

### Docker way

To start run the following command in root directory

```
docker-compose up -d
```

To stop serving run 

```
docker-compose down -v
```

## Running the tests

Run the following command in `./src/backend/` directory
```
dotnet test
```

## Scheme of work

### Broadcast event bus messages

* `UserConnected`
* `UserDisconnected`
* `ChallengeFinished`
* `ChallengeUpdated`
* `UserScoreUpdated`

Broadcast messages are used as SignalR backplane, to notify users on all application nodes.

### Round-robin event bus messages
* `ChallengeStarting`

 Round-robin message `ChallengeStarting` performs load balancing between all application nodes for generating and starting new math challenges.
 

 kek
 лул2

## Built With

* [AspNetCore](https://github.com/aspnet/AspNetCore)
* [SignalR](https://github.com/aspnet/AspNetCore/tree/master/src/SignalR)
* [MongoDb](https://www.mongodb.com/)
* [RabbitMq](https://www.rabbitmq.com/)
* [Angular](https://github.com/angular)
* [Docker](https://www.docker.com/)
* [Nginx](https://www.nginx.com)
