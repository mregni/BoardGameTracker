import { useTranslation } from 'react-i18next';

import { BgtText } from '@/components/BgtText/BgtText';
import { BgtIconButton } from '@/components/BgtIconButton/BgtIconButton';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import Trash from '@/assets/icons/trash.svg?react';
import Plus from '@/assets/icons/plus-circle.svg?react';
import Package from '@/assets/icons/package.svg?react';

interface Expansion {
  id: number;
  title: string;
}

interface Props {
  expansions: Expansion[];
  onAddExpansion: () => void;
  onDeleteExpansion: (expansionId: number) => void;
}

export const ExpansionsCard = (props: Props) => {
  const { expansions, onAddExpansion, onDeleteExpansion } = props;
  const { t } = useTranslation();

  return (
    <BgtCard
      title={`${t('game.expansions.title')} (${expansions.length})`}
      icon={Package}
      actions={[
        {
          variant: 'primary',
          content: <Plus className="size-5" />,
          onClick: onAddExpansion,
        },
      ]}
    >
      {expansions.length === 0 ? (
        <div className="text-center py-8">
          <div className="text-white/50 text-sm">{t('game.expansions.none')}</div>
          <button
            onClick={onAddExpansion}
            className="mt-3 text-primary hover:text-primary/80 text-sm transition-colors"
          >
            {t('game.expansions.add')}
          </button>
        </div>
      ) : (
        <div className="space-y-2">
          {expansions.map((expansion) => (
            <div
              key={expansion.id}
              className="flex items-center gap-3 bg-primary/5 rounded-lg p-4 border border-primary/10 group"
            >
              <div className="shrink-0 w-10 h-10 bg-primary/20 rounded-lg flex items-center justify-center border border-primary/30">
                <Package className="text-primary" />
              </div>
              <div className="flex-1">
                <BgtText color="white">{expansion.title}</BgtText>
              </div>
              <div className="flex">
                <BgtIconButton icon={<Trash />} intent="danger" onClick={() => onDeleteExpansion(expansion.id)} />
              </div>
            </div>
          ))}
        </div>
      )}
    </BgtCard>
  );
};
