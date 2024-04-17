import {PlayFlag} from '../models/Games/PlayFlag';

export const StringToHsl = (value: string | undefined): string => {
  if (value === undefined) {
    return 'hsl(0, 85%, 35%)';
  }

  return `hsl(${calculateHash(value)}, 85%, 35%)`;
};

export const StringToRgb = (value: string | undefined): string => {
  if (value === undefined) {
    return '#000000';
  }

  const h = calculateHash(value) / 360;
  const s = 0.85;
  const l = 0.35;

  const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
  const p = 2 * l - q;
  const r = hueToRgb(p, q, h + 1 / 3);
  const g = hueToRgb(p, q, h);
  const b = hueToRgb(p, q, h - 1 / 3);

  return `rgb(${Math.round(r * 255)}, ${Math.round(g * 255)}, ${Math.round(b * 255)})`;
}

const calculateHash = (input: string): number => {
  let hash = 0;
  for (let i = 0; i < input.length; i++) {
    hash = input.charCodeAt(i) + ((hash << 5) - hash);
  }

  return hash % 360;
}

const hueToRgb = (p: number, q: number, t: number): number => {
  if (t < 0) t += 1;
  if (t > 1) t -= 1;
  if (t < 1 / 6) return p + (q - p) * 6 * t;
  if (t < 1 / 2) return q;
  if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
  return p;
}

export const PlayFlagToString = (flag: PlayFlag): string => {
  switch (flag) {
    case PlayFlag.LongestGame:
      return 'common.flags.longest-game';
    case PlayFlag.ShortestGame:
      return 'common.flags.shortest-game';
    case PlayFlag.HighestScore:
      return 'common.flags.highest-score';
    case PlayFlag.LowestScore:
      return 'common.flags.lowest-score';
  }
}
