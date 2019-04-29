export interface Quiz {
    challenge: MathChallenge;
    users: User[];
}

export interface User {
    username: string;
    score: number;
}

export interface MathChallenge {
    question: string;
    isCompleted: boolean;
}

export interface HistoryEntry {
    timestamp: Date;
    message: string;
}