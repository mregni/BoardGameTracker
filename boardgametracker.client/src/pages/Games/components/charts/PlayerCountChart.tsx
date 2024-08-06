/* eslint-disable @typescript-eslint/no-unsafe-member-access */
/* eslint-disable @typescript-eslint/dot-notation */
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';
import { AgChartsReact } from 'ag-charts-react';
import { ResponsivePie } from '@nivo/pie';

import { useGame } from '../../../../hooks/useGame';
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
    <>
      <div className="col-span-1">
        <BgtChartCard title={t('game.charts.player-count')} className="h-96">
          <ResponsivePie
            data={pieData}
            innerRadius={0.5}
            padAngle={3}
            cornerRadius={3}
            activeOuterRadiusOffset={8}
            borderWidth={1}
            borderColor={{
              from: 'color',
              modifiers: [['darker', 0.9]],
            }}
            arcLinkLabel={(e) => t('common.player', { count: +e.id })}
            arcLinkLabelsSkipAngle={10}
            arcLinkLabelsTextColor="#ffffff"
            arcLinkLabelsThickness={2}
            arcLinkLabelsColor={{ from: 'color' }}
            arcLabelsSkipAngle={10}
            arcLabelsTextColor={{
              from: 'color',
              modifiers: [['darker', 2]],
            }}
            theme={theme}
            margin={{ top: 40, right: 80, bottom: 40, left: 80 }}
            tooltip={() => <></>}
          />
        </BgtChartCard>
      </div>
      <div className="col-span-1">
        <BgtChartCard title={t('game.charts.player-count')}>
          <AgChartsReact
            options={{
              data: playerCountChart,
              series: [
                {
                  type: 'pie',
                  angleKey: 'playCount',
                  legendItemKey: 'players',
                  showInLegend: false,
                  tooltip: {
                    renderer: (params) => {
                      return `<div class="ag-chart-tooltip-title !rounded-lg" style="background-color: ${params.color}">
                  ${t('common.game', { count: +params.datum['playCount'] })} with ${t('common.player', { count: +params.datum['players'] })}
                </div>`;
                    },
                  },
                  calloutLabelKey: 'playCount',
                  calloutLabel: {
                    color: 'white',
                    formatter: (params) => t('common.player', { count: +params.datum['players'] }),
                  },
                },
              ],
              background: { visible: false },
              height: 300,
            }}
          />
        </BgtChartCard>
      </div>
    </>
  );
};
