/* eslint-disable @typescript-eslint/no-unsafe-member-access */
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { renderToString } from 'react-dom/server';
import { format } from 'date-fns';
import { AgChartsReact } from 'ag-charts-react';

import { useGame } from '../../hooks/useGame';

import { StringToRgb } from '@/utils/stringUtils';
import { useSettings } from '@/hooks/useSettings';
import { usePlayerById } from '@/hooks/usePlayerById';
import { BgtChartCard } from '@/components/BgtCard/BgtChartCard';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';

interface SeriesData {
  date: string;
  [person: string]: number | string; // This allows any number of person properties with number values
}

export const PlayerScoringChart = () => {
  const { id } = useParams();
  const { playerScoringChart, game } = useGame({ id });
  const { settings } = useSettings();
  const { playerById } = usePlayerById();
  const { t } = useTranslation();

  if (playerScoringChart.data === undefined || settings.data === undefined || !game.data?.hasScoring) return null;

  const transformedSeries: SeriesData[] = playerScoringChart.data.map(({ dateTime, series }) => {
    const data: SeriesData = { date: format(dateTime, settings.data.dateFormat) };
    series.forEach(({ id, value }) => {
      data[id] = value;
    });
    return data;
  });

  return (
    <div className="col-span-1">
      <BgtChartCard title={t('game.charts.scoring')}>
        <AgChartsReact
          options={{
            data: transformedSeries,
            theme: {
              overrides: {
                line: {
                  series: {
                    highlightStyle: {
                      series: {
                        dimOpacity: 0.2,
                        strokeWidth: 4,
                      },
                    },
                  },
                },
              },
            },
            series: Object.keys(transformedSeries[0] ?? [])
              .filter((x) => x !== 'date')
              .map((x) => ({
                tooltip: {
                  renderer: (params) =>
                    `<div class="ag-chart-tooltip-title !rounded-lg flex flex-row justify-start gap-2 items-center" style="background-color:${params.color}">
                    ${renderToString(<BgtAvatar title={params.title} image={playerById(params.yKey)?.image} noTooltip />)}
                    ${params.title}: ${params.datum[params.yKey]}
                  </div>`,
                },
                type: 'line',
                xKey: 'date',
                yKey: `${x}`,
                yName: playerById(x)?.name,
                stroke: StringToRgb(playerById(x)?.name),
                marker: {
                  fill: StringToRgb(playerById(x)?.name),
                  stroke: StringToRgb(playerById(x)?.name),
                },
              })),
            background: { visible: false },
            height: 300,
            axes: [
              {
                type: 'number',
                position: 'left',
                gridLine: {
                  style: [
                    {
                      stroke: 'lightgray',
                    },
                  ],
                },
              },
              {
                type: 'category',
                position: 'bottom',
              },
            ],
          }}
        />
      </BgtChartCard>
    </div>
  );
};
