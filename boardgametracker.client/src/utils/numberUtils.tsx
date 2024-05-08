export const RoundDecimal = (value: number | null, step = 0.5): number | null => {
  if (value === null) return null;

  const roundedValue = Math.round(value / step) * step;
  return Math.round(roundedValue * 1000) / 1000;
};

export const GetPercentage = (value: number | null, total: number | null): number | null => {
  if (value === null || total === null || value === 0) return null;

  return RoundDecimal((value / total) * 100);
};
