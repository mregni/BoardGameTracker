import { Bars } from 'react-loading-icons';
import { useTranslation } from 'react-i18next';
import { useState, useEffect, useMemo } from 'react';
import { useForm } from '@tanstack/react-form';

import { useNewLoanModal } from '../-hooks/useNewLoanModal';

import { CreatePlayerModal } from '@/routes/players/-modals/CreatePlayerModal';
import { useToasts } from '@/routes/-hooks/useToasts';
import { CreateLoanSchema } from '@/models/Loan/CreateLoan';
import { Player } from '@/models';
import { BgtFormField, BgtSelect, BgtDatePicker } from '@/components/BgtForm';
import {
  BgtDialog,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogClose,
  BgtDialogTitle,
} from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

const NewLoanModal = (props: Props) => {
  const { open, setOpen } = props;
  const { t } = useTranslation();
  const { errorToast, successToast } = useToasts();

  const [openCreatePlayerModal, setOpenCreatePlayerModal] = useState(false);
  const [newlyCreatedPlayerId, setNewlyCreatedPlayerId] = useState<number | null>(null);

  const onSaveError = () => {
    errorToast('loan.notifications.create-failed');
  };

  const onSaveSuccess = () => {
    successToast('loan.notifications.created');
    form.reset();
    setOpen(false);
  };

  const { games, players, isLoading, saveLoan } = useNewLoanModal({ onSaveError, onSaveSuccess });

  const gamesSelectItems = useMemo(
    () =>
      games.map((x) => ({
        value: x.id,
        label: x.title,
        image: x.image,
      })),
    [games]
  );

  const playersSelectItems = useMemo(
    () =>
      players.map((x) => ({
        value: x.id,
        label: x.name,
        image: x.image,
      })),
    [players]
  );

  const form = useForm({
    defaultValues: {
      gameId: '',
      playerId: '',
      loanDate: '',
      returnDate: '',
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreateLoanSchema.parse(value);
      await saveLoan(validatedData);
    },
  });

  const handlePlayerCreated = (player: Player) => {
    setNewlyCreatedPlayerId(player.id);
  };

  useEffect(() => {
    if (newlyCreatedPlayerId !== null) {
      const playerExists = players.some((p) => p.id === newlyCreatedPlayerId);
      if (playerExists) {
        form.setFieldValue('playerId', String(newlyCreatedPlayerId));
        setTimeout(() => setNewlyCreatedPlayerId(null), 0);
      }
    }
  }, [players, newlyCreatedPlayerId, form]);

  useEffect(() => {
    if (!open) {
      form.reset();
    }
  }, [open, form]);

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
          className="w-full"
        >
          <BgtDialogTitle>{t('loan.new.title')}</BgtDialogTitle>
          <BgtDialogDescription>{t('loan.new.description')}</BgtDialogDescription>
          <div className="flex flex-col gap-4 mt-3 mb-3">
            <div className="flex flex-col gap-3 w-full">
              <BgtFormField form={form} name="gameId" schema={CreateLoanSchema.shape.gameId}>
                {(field) => (
                  <BgtSelect
                    field={field}
                    hasSearch
                    items={gamesSelectItems}
                    label={t('player-session.new.game.label')}
                    disabled={isLoading}
                    placeholder={t('player-session.new.game.placeholder')}
                  />
                )}
              </BgtFormField>
              <BgtFormField form={form} name="playerId" schema={CreateLoanSchema.shape.playerId}>
                {(field) => (
                  <BgtSelect
                    field={field}
                    hasSearch
                    items={playersSelectItems}
                    label={t('loan.new.player.label')}
                    disabled={isLoading}
                    placeholder={t('loan.new.player.placeholder')}
                  />
                )}
              </BgtFormField>
              <div>
                <BgtButton type="button" variant="text" size="1" onClick={() => setOpenCreatePlayerModal(true)}>
                  + {t('player-session.new.create-player')}
                </BgtButton>
              </div>
              <BgtFormField form={form} name="loanDate" schema={CreateLoanSchema.shape.loanDate}>
                {(field) => (
                  <BgtDatePicker
                    field={field}
                    label={t('loan.new.start.label')}
                    disabled={isLoading}
                    placeholder={t('loan.new.start.placeholder')}
                  />
                )}
              </BgtFormField>
              <BgtFormField form={form} name="dueDate" schema={CreateLoanSchema.shape.dueDate}>
                {(field) => (
                  <BgtDatePicker
                    field={field}
                    label={t('loan.new.end.label')}
                    disabled={isLoading}
                    placeholder={t('loan.new.end.placeholder')}
                  />
                )}
              </BgtFormField>
            </div>
          </div>
          <BgtDialogClose>
            <BgtButton disabled={isLoading} variant="cancel" className="flex-1" onClick={() => setOpen(false)}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" disabled={isLoading} className="flex-1" variant="primary">
              {isLoading && <Bars className="size-4" />}
              {t('loan.new.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>

      {/* Reuse CreatePlayerModal */}
      <CreatePlayerModal
        open={openCreatePlayerModal}
        setOpen={setOpenCreatePlayerModal}
        onPlayerCreated={handlePlayerCreated}
      />
    </BgtDialog>
  );
};

export default NewLoanModal;
