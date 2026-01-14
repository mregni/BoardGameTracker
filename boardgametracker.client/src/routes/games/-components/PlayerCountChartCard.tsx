import { useTranslation } from 'react-i18next';

import { transformPlayerCountChartData } from '../-utils/gameDataTransformers';

import { BgtPieChart } from '@/components/BgtCharts/BgtPieChart';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import Users from '@/assets/icons/users.svg?react';

interface PlayerCountItem {
  players: number | string;
  playCount: number;
}

interface Props {
  playerCountChart: PlayerCountItem[];
}

export const PlayerCountChartCard = (props: Props) => {
  const { playerCountChart } = props;
  const { t } = useTranslation();

  const chartData = transformPlayerCountChartData(playerCountChart, t);

  return (
    <BgtCard title={t('game.charts.player-count')} icon={Users}>
      <BgtPieChart showLegend data={chartData} tooltipPrefix="common.game" />
    </BgtCard>
  );
};
