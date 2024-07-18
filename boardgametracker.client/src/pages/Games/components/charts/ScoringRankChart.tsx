/* eslint-disable @typescript-eslint/no-unsafe-member-access */
/* eslint-disable @typescript-eslint/dot-notation */
/* eslint-disable @typescript-eslint/no-unsafe-argument */
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';
import { ResponsiveBar } from '@nivo/bar';

import { RoundDecimal } from '../../../../utils/numberUtils';
import { usePlayers } from '../../../../hooks/usePlayers';
import { useGame } from '../../../../hooks/useGame';
import { BgtChartCard } from '../../../../components/BgtCard/BgtChartCard';

const fillColors: Record<string, string> = {
  'top-score': '#008000',
  'highest-losing': '#2B5600',
  average: '#562B00',
  'lowest-winning': '#802B00',
  lowest: '#800000',
};

const theme = {
  grid: {
    line: {
      stroke: '#333333',
    },
  },
  axis: {
    ticks: {
      text: {
        fill: '#ffffff',
        fontFamily: 'Chakra Petch',
      },
    },
  },
};

export const ScoringRankChart = () => {
  const { id } = useParams();
  const { scoreRankChart, game } = useGame(id);
  const { byId } = usePlayers();
  const { t } = useTranslation();

  const barData = useMemo(() => {
    if (scoreRankChart !== undefined) {
      return scoreRankChart
        .map((rank) => ({
          key: rank.key,
          score: rank.score,
          playerId: rank.playerId,
        }))
        .reverse();
    }
    return [];
  }, [scoreRankChart]);

  if (scoreRankChart === undefined || !game?.hasScoring) return null;

  return (
    <>
      <div className="col-span-1">
        <BgtChartCard title={t('game.charts.top-scoring.title')} className="h-96">
          <ResponsiveBar
            data={barData}
            theme={theme}
            keys={['score']}
            indexBy="key"
            margin={{ top: 0, right: 20, bottom: 50, left: 100 }}
            padding={0.3}
            layout="horizontal"
            colors={(id) => fillColors[id.data.key]}
            borderRadius={5}
            enableGridY={false}
            enableGridX
            enableLabel={false}
            labelSkipWidth={100}
            axisLeft={{
              format: (value) => t(`game.charts.top-scoring.${value}`),
            }}
            layers={[
              'grid',
              'axes',
              'bars',
              'markers',
              'legends',
              'annotations',
              ({ bars, labelSkipWidth }) => {
                return (
                  <g>
                    {bars.map(({ width, height, y, data }) => {
                      // only show this custom outer label on bars that are too small
                      return width < labelSkipWidth ? (
                        <text
                          fill="#ffffff"
                          transform={`translate(${width + 10}, ${y + height / 2})`}
                          dominantBaseline="central"
                          className="uppercase"
                        >
                          {byId(data.data.playerId)
                            ? `${byId(data.data.playerId)?.name} ${data.data.score}`
                            : RoundDecimal(data.data.score)}
                        </text>
                      ) : (
                        <text
                          fill="#ffffff"
                          transform={`translate(${width - 10}, ${y + height / 2})`}
                          textAnchor="end"
                          dominantBaseline="central"
                          className="uppercase"
                        >
                          {' '}
                          {byId(data.data.playerId)
                            ? `${byId(data.data.playerId)?.name} ${data.data.score}`
                            : RoundDecimal(data.data.score)}
                        </text>
                      );
                    })}
                  </g>
                );
              },
            ]}
          />
        </BgtChartCard>
      </div>
    </>
  );
};
