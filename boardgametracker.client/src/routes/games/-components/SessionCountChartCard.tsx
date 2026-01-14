import { useTranslation } from 'react-i18next';

import { transformSessionCountChartData } from '../-utils/gameDataTransformers';

import { BgtBarChart } from '@/components/BgtCharts/BgtBarChart';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import BarChart from '@/assets/icons/bar-chart.svg?react';

interface PlayByDayItem {
  dayOfWeek: number;
  playCount: number;
}

interface Props {
  playByDayChart: PlayByDayItem[];
}

export const SessionCountChartCard = (props: Props) => {
  const { playByDayChart } = props;
  const { t } = useTranslation();

  const chartData = transformSessionCountChartData(playByDayChart, t);

  return (
    <BgtCard title={t('game.titles.session-count-per-day')} icon={BarChart}>
      <BgtBarChart index="day" keys={['sessions']} data={chartData} />
    </BgtCard>
  );
};
