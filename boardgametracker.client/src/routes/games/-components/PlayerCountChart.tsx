import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';

import { PlayerCountChartData } from '@/models/Games/GameStatistics';
import { BgtPieChart } from '@/components/BgtCharts/BgtPieChart';
import { BgtChartCard } from '@/components/BgtCard/BgtChartCard';

interface Props {
  data: PlayerCountChartData[];
}

export const PlayerCountChart = ({ data }: Props) => {
  const { t } = useTranslation();

  const pieData = useMemo(() => {
    return data
      .map((rank) => ({
        id: rank.players,
        label: String(rank.players),
        value: rank.playCount,
      }))
      .reverse();
  }, [data]);

  return (
    <div className="col-span-1">
      <BgtChartCard title={t('game.charts.player-count')} className="h-96">
        <BgtPieChart data={pieData} labelPrinter={(value) => t('common.player', { count: +(value as number) })} />
      </BgtChartCard>
    </div>
  );
};
