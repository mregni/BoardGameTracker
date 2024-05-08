import { BarDatum } from '@nivo/bar';

export interface PlaysByDayChart extends BarDatum {
  dayOfWeek: number;
  playCount: number;
}
