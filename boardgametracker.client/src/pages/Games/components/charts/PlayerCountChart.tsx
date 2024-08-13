/* eslint-disable @typescript-eslint/no-unsafe-member-access */
/* eslint-disable @typescript-eslint/dot-notation */
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';
import { AgChartsReact } from 'ag-charts-react';
import { ResponsivePie } from '@nivo/pie';

import { useGame } from '../../../../hooks/useGame';
import { BgtPieChart } from '../../../../components/BgtCharts/BgtPieChart';
import { BgtChartCard } from '../../../../components/BgtCard/BgtChartCard';

const theme = {
  axis: {
    ticks: {
      text: {
        fontFamily: 'Chakra Petch',
      },
    },
  },
};

export const PlayerCountChart = () => {
  const { id } = useParams();
  const { playerCountChart } = useGame(id);
  const { t } = useTranslation();

  const pieData = useMemo(() => {
    if (playerCountChart !== undefined) {
      return playerCountChart
        .map((rank) => ({
          id: rank.players,
          label: rank.players,
          value: rank.playCount,
        }))
        .reverse();
    }
    return [];
  }, [playerCountChart]);

  if (playerCountChart === undefined) return null;

  return (
    <div className="col-span-1">
      <BgtChartCard title={t('game.charts.player-count')} className="h-96">
        <BgtPieChart data={pieData} labelPrinter={(value) => t('common.player', { count: +(value as number) })} />
      </BgtChartCard>
    </div>
  );
};
