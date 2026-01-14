export const RoundDecimal = (value: number | null, increment: number = 1): number | null => {
  if (value === null) return null;
  return Math.round(value / increment) * increment;
};

export const GetPercentage = (value: number, total: number): number => {
  if (total === 0) return 0;
  return Math.round((value / total) * 100);
};

export const ToLogLevel = (level: number): string => {
  const levels = ['log-levels.warn', 'log-levels.debug', 'log-levels.info', 'log-levels.warn', 'log-levels.error'];
  return levels[level] || 'log-levels.warn';
};
