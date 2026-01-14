import { useTranslation } from 'react-i18next';
import { memo } from 'react';

import { Expansion } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtCheckboxList } from '@/components/BgtForm/BgtCheckboxList';

interface SessionExpansionSelectorProps {
  expansionList: Expansion[];
  selectedIds: number[];
  selectedGameId: number;
  disabled: boolean;
  onSelectionChange: (ids: number[]) => void;
}

const SessionExpansionSelectorComponent = ({
  expansionList,
  selectedIds,
  selectedGameId,
  disabled,
  onSelectionChange,
}: SessionExpansionSelectorProps) => {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col justify-start">
      <div className="text-[15px] font-medium leading-[35px] uppercase pb-2">{t('game.expansions.title')}</div>
      {expansionList.length === 0 && (
        <BgtText color="primary">
          {selectedGameId !== undefined && selectedGameId !== 0 && t('game.expansions.none')}
          {(selectedGameId === undefined || selectedGameId === 0) && t('game.expansions.no-game')}
        </BgtText>
      )}

      <BgtCheckboxList
        items={expansionList.map((x) => ({ id: x.id, value: x.title }))}
        selectedIds={selectedIds}
        onSelectionChange={onSelectionChange}
        disabled={disabled}
      />
    </div>
  );
};

SessionExpansionSelectorComponent.displayName = 'SessionExpansionSelector';

export const SessionExpansionSelector = memo(SessionExpansionSelectorComponent);
