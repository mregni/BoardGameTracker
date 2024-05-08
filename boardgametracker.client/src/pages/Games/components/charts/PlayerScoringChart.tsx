/* eslint-disable @typescript-eslint/no-unsafe-member-access */
import { AgChartsReact } from 'ag-charts-react';
import { format } from 'date-fns';
import { renderToString } from 'react-dom/server';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';

import { BgtAvatar } from '../../../../components/BgtAvatar/BgtAvatar';
import { BgtCard } from '../../../../components/BgtCard/BgtCard';
import { useGame } from '../../../../hooks/useGame';
import { usePlayers } from '../../../../hooks/usePlayers';
import { useSettings } from '../../../../hooks/useSettings';
import { StringToRgb } from '../../../../utils/stringUtils';

interface SeriesData {
  date: string;
  [person: string]: number | string; // This allows any number of person properties with number values
}

export const PlayerScoringChart = () => {
  const { id } = useParams();
  const { playerScoringChart, game } = useGame(id);
  const { settings } = useSettings();
  const { byId } = usePlayers();
  const { t } = useTranslation();

  if (playerScoringChart === undefined || settings === undefined || !game?.hasScoring) return null;

  const transformedSeries: SeriesData[] = playerScoringChart.map(({ dateTime, series }) => {
    const data: SeriesData = { date: format(dateTime, settings.dateFormat) };
    series.forEach(({ id, value }) => {
      data[id] = value;
    });
    return data;
  });

  console.log(transformedSeries);

  return (
    <BgtCard>
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
                  `<div class="ag-chart-tooltip-title flex flex-row justify-start gap-2 items-center" style="background-color:${params.color}">
                    ${renderToString(<BgtAvatar title={params.title} image={byId(params.yKey)?.image} noTooltip />)}
                    ${params.title}: ${params.datum[params.yKey]}
                  </div>`,
              },
              type: 'line',
              xKey: 'date',
              yKey: `${x}`,
              yName: byId(x)?.name,
              stroke: StringToRgb(byId(x)?.name),
              marker: {
                fill: StringToRgb(byId(x)?.name),
                stroke: StringToRgb(byId(x)?.name),
              },
            })),
          background: { visible: false },
          title: { text: t('game.charts.scoring') },
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
    </BgtCard>
  );
};
