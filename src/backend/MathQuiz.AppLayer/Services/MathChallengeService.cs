using System;
using MathQuiz.AppLayer.Abstractions;
using MathQuiz.Domain;

namespace MathQuiz.AppLayer.Services
{
    public class MathChallengeService : IMathChallengeService
    {
        private readonly Random _random = new Random();
        private readonly Array _operations = Enum.GetValues(typeof(MathOperation));

        public MathChallenge CreateChallenge()
        {
            var operation = GetRandomOperation();
            var leftOperand = GetRandomOperand();
            var rightOperand = GetRandomOperand();
            var deviance = _random.Next(-2, 2);

            double answer;
            string operationString;
            switch (operation)
            {
                case MathOperation.Add:
                    operationString = "+";
                    answer = leftOperand + rightOperand;
                    break;
                case MathOperation.Subtract:
                    operationString = "-";
                    answer = leftOperand - rightOperand;
                    break;
                case MathOperation.Multiply:
                    operationString = "*";
                    answer = leftOperand * rightOperand;
                    break;
                case MathOperation.Divide:
                    operationString = "/";
                    answer = (double)leftOperand / rightOperand;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var possibleAnswer = answer + deviance;
            return new MathChallenge
            {
                Question = $"{leftOperand} {operationString} {rightOperand} = {possibleAnswer}",
                IsCorrect = deviance == 0
            };
        }

        private MathOperation GetRandomOperation()
        {
            return (MathOperation)_operations.GetValue(_random.Next(_operations.Length));
        }

        private int GetRandomOperand()
        {
            return _random.Next(1, 11);
        }

        private enum MathOperation
        {
            Add,
            Subtract,
            Multiply,
            Divide
        }
    }
}
