export const RoundDecimal = (value: number | null, step = 0.5): number | null => {
  if (value === null) return null;

  const roundedValue = Math.round(value / step) * step;
  return Math.round(roundedValue * 1000) / 1000;
}