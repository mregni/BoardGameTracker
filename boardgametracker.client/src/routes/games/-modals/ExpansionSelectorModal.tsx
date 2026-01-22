import { useTranslation } from 'react-i18next';
import { useState } from 'react';

import { useExpansionSelectorModal } from '../-hooks/useExpansionSelectorModal';

import { BgtCheckboxList } from '@/components/BgtForm';
import {
  BgtDialog,
  BgtDialogClose,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogTitle,
} from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  gameId: number;
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  selectedExpansions: number[];
}

export const ExpansionSelectorModal = (props: Props) => {
  const { open, setOpen, gameId, selectedExpansions } = props;
  const { t } = useTranslation();
  const [selectedIds, setSelectedIds] = useState<number[]>(selectedExpansions);

  const { expansions, isLoading, isPending, saveExpansions } = useExpansionSelectorModal({ gameId });

  const saveModal = () => {
    saveExpansions({ gameId, expansionBggIds: selectedIds }).finally(() => {
      setOpen(false);
    });
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('game.expansions.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('game.expansions.description')}</BgtDialogDescription>
        <div className="my-4">
          {isLoading && <div>{t('common.loading-data')}</div>}
          <BgtCheckboxList
            items={expansions}
            selectedIds={selectedExpansions}
            onSelectionChange={(ids) => setSelectedIds(ids)}
            disabled={isLoading || isPending}
          />
        </div>
        <BgtDialogClose>
          <BgtButton variant="cancel" onClick={() => setOpen(false)} disabled={isLoading || isPending}>
            {t('common.cancel')}
          </BgtButton>
          <BgtButton type="button" variant="primary" disabled={isLoading} onClick={saveModal || isPending}>
            {t('game.expansions.update')}
          </BgtButton>
        </BgtDialogClose>
      </BgtDialogContent>
    </BgtDialog>
  );
};
