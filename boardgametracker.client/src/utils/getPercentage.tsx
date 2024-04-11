import {RoundDecimal} from './roundDecimal';

export const GetPercentage = (value: number | null, total: number | null): number | null => {
  if (value === null || total === null || value === 0)
    return null;

  return RoundDecimal(value / total * 100);
}