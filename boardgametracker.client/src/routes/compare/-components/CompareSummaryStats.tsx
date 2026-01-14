import { useTranslation } from 'react-i18next';
import { memo, useMemo } from 'react';

import { useGameById } from '../../-hooks/useGameById';

import { CompareResult } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtCard } from '@/components/BgtCard/BgtCard';

interface CompareSummaryStatsProps {
  compare: CompareResult;
}

const CompareSummaryStatsComponent = ({ compare }: CompareSummaryStatsProps) => {
  const { t } = useTranslation();

  const { gameById } = useGameById();

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6 mb-12">
      <BgtCard>
        <div className="flex items-center flex-col">
          <BgtText color="cyan" size="8">
            {compare.totalSessionsTogether}
          </BgtText>
          <BgtText color="primary" opacity={70} className="uppercase tracking-wider">
            {t('compare.total-games-played')}
          </BgtText>
        </div>
      </BgtCard>
      <BgtCard>
        <div className="flex items-center flex-col">
          <BgtText color="cyan" size="8">
            {compare.minutesPlayed}
          </BgtText>
          <BgtText color="primary" opacity={70} className="uppercase tracking-wider">
            {t('compare.minutes-played')}
          </BgtText>
        </div>
      </BgtCard>
      <BgtCard>
        <div className="flex items-center flex-col">
          <BgtText color="cyan" size="8" className="line-clamp-1">
            {compare.preferredGame?.gameId ? gameById(compare.preferredGame.gameId)?.title : '-'}
          </BgtText>
          <BgtText color="primary" opacity={70} className="uppercase tracking-wider">
            {t('compare.most-played-game')}
          </BgtText>
        </div>
      </BgtCard>
    </div>
  );
};

CompareSummaryStatsComponent.displayName = 'CompareSummaryStats';

export const CompareSummaryStats = memo(CompareSummaryStatsComponent);
