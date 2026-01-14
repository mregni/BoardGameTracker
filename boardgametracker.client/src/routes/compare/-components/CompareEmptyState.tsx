import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';

import { BgtText } from '@/components/BgtText/BgtText';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import BgtButton from '@/components/BgtButton/BgtButton';
import Users from '@/assets/icons/users.svg?react';
import TrendUp from '@/assets/icons/trend-up.svg?react';

interface CompareEmptyStateProps {
  playerCount: number;
}

export const CompareEmptyState = ({ playerCount }: CompareEmptyStateProps) => {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const handleAddPlayers = () => {
    navigate({ to: '/players' });
  };

  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <div className="text-center max-w-lg flex flex-col gap-3">
        <div className="mb-6 flex justify-center">
          <div className="relative">
            <div className="w-32 h-32 rounded-full bg-primary/10 border-2 border-primary/30 flex items-center justify-center">
              <Users className="text-primary/70 size-15" />
            </div>
            <div className="absolute -bottom-2 -left-2 w-10 h-10 rounded-full bg-cyan-400 flex items-center justify-center">
              <TrendUp className="text-white size-6" />
            </div>
          </div>
        </div>

        <BgtHeading className="mb-3">
          {playerCount === 0 ? t('compare.empty.no-players.title') : t('compare.empty.insufficient-players.title')}
        </BgtHeading>

        <BgtText color="white" opacity={60} className="mb-8 leading-relaxed">
          {playerCount === 0
            ? t('compare.empty.no-players.description')
            : t('compare.empty.insufficient-players.description')}
        </BgtText>

        <BgtButton onClick={handleAddPlayers} className="mt-6">
          {t('compare.empty.button')}
        </BgtButton>
      </div>
    </div>
  );
};
