export enum BadgeType {
  sessions = 'sessions',
  differentGames = 'different_games',
  wins = 'wins',
  duration = 'duration',
  winPercentage = 'winPercentage',
  soloSpecialist = 'soloSpecialist',
  closeWin = 'closeWin',
  cLoseLoss = 'cLoseLoss',
  marathonRunner = 'marathonRunner',
  firstTry = 'firstTry',
  learningCurve = 'learningCurve',
  monthlyGoal = 'monthlyGoal',
  consistentSchedule = 'consistentSchedule',
  socialPlayer = 'socialPlayer',
  winningStreak = 'winningStreak',
}

export enum BadgeLevel {
  green = 'green',
  blue = 'blue',
  red = 'red',
  gold = 'gold',
}

export interface Badge {
  id: number;
  descriptionKey: string;
  titleKey: string;
  type: BadgeType;
  level: BadgeLevel | null;
  image: string;
}
