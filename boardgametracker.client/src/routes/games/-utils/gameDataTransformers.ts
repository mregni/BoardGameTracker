import { TFunction } from 'i18next';

import { PieChartDatum } from '@/models/Charts/PieChartDatum';

export const transformPlayerCountChartData = (
  data: Array<{ players: number | string; playCount: number }>,
  t: TFunction
): PieChartDatum[] => {
  return data.map((item) => ({
    label: t('common.player', { count: +(item.players as number) }),
    id: item.players,
    value: item.playCount,
  }));
};

export const transformSessionCountChartData = (
  data: Array<{ dayOfWeek: number; playCount: number }>,
  t: TFunction
): Array<{ day: string; sessions: number }> => {
  return data.map((item) => ({
    day: t(`common.days.${item.dayOfWeek}`),
    sessions: item.playCount,
  }));
};
