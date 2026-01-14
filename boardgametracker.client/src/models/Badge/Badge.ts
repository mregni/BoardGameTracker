export enum BadgeType {
  sessions = 0,
  differentGames = 1,
  wins = 2,
  duration = 3,
  winPercentage = 4,
  soloSpecialist = 5,
  closeWin = 6,
  cLoseLoss = 7,
  marathonRunner = 8,
  firstTry = 9,
  learningCurve = 10,
  monthlyGoal = 11,
  consistentSchedule = 12,
  socialPlayer = 13,
  winningStreak = 14,
}

export enum BadgeLevel {
  green = 0,
  blue = 1,
  red = 2,
  gold = 3,
}

export interface Badge {
  id: number;
  descriptionKey: string;
  titleKey: string;
  type: BadgeType;
  level: BadgeLevel | null;
  image: string;
}
