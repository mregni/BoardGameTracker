export const RoundDecimal = (value: number | undefined | null, step = 0.5): number | null => {
  if (value === undefined || value === null) return null;

  const roundedValue = Math.round(value / step) * step;
  return Math.round(roundedValue * 1000) / 1000;
};

export const GetPercentage = (value: number | undefined | null, total: number | null): number | null => {
  if (value === null || total === null || value === 0) return null;
  if (value === undefined) return null;

  return RoundDecimal((value / total) * 100);
};

export const ToLogLevel = (level: number): string => {
  switch (level) {
    case 1:
      return 'log-levels.debug';
    case 2:
      return 'log-levels.info';
    case 3:
      return 'log-levels.warn';
    case 4:
      return 'log-levels.error';
    default:
      return 'log-levels.warn';
  }
};
