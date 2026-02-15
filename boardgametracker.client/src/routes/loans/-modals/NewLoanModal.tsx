import { Bars } from 'react-loading-icons';
import { useTranslation } from 'react-i18next';
import { useState, useEffect, useMemo, useRef } from 'react';
import { useForm } from '@tanstack/react-form';

import { useNewLoanModal } from '../-hooks/useNewLoanModal';

import { handleFormSubmit } from '@/utils/formUtils';
import { CreatePlayerModal } from '@/routes/players/-modals/CreatePlayerModal';
import { CreateLoanSchema } from '@/models/Loan/CreateLoan';
import { ModalProps, Player } from '@/models';
import { BgtFormField, BgtSelect, BgtDatePicker } from '@/components/BgtForm';
import {
  BgtDialog,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogClose,
  BgtDialogTitle,
} from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

const NewLoanModal = (props: ModalProps) => {
  const { open, close } = props;
  const { t } = useTranslation();

  const [openCreatePlayerModal, setOpenCreatePlayerModal] = useState(false);
  const newlyCreatedPlayerIdRef = useRef<number | null>(null);

  const onSuccess = () => {
    form.reset();
    close();
  };

  const { games, players, isLoading, saveLoan } = useNewLoanModal({ onSuccess });

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
      dueDate: '',
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreateLoanSchema.parse(value);
      await saveLoan(validatedData);
    },
  });

  const handlePlayerCreated = (player: Player) => {
    newlyCreatedPlayerIdRef.current = player.id;
  };

  useEffect(() => {
    if (newlyCreatedPlayerIdRef.current !== null) {
      const playerExists = players.some((p) => p.id === newlyCreatedPlayerIdRef.current);
      if (playerExists) {
        form.setFieldValue('playerId', String(newlyCreatedPlayerIdRef.current));
        newlyCreatedPlayerIdRef.current = null;
      }
    }
  }, [players, form]);

  useEffect(() => {
    if (!open) {
      form.reset();
    }
  }, [open, form]);

  return (
    <BgtDialog open={open} onClose={close}>
      <BgtDialogContent>
        <form
          onSubmit={handleFormSubmit(form)}
          className="w-full"
        >
          <BgtDialogTitle>{t('loan.new.title')}</BgtDialogTitle>
          <BgtDialogDescription>{t('loan.new.description')}</BgtDialogDescription>
          <div className="flex flex-col gap-4 mt-3 mb-3">
            <div className="flex flex-col gap-3 w-full">
              <BgtFormField form={form} name="gameId" schema={CreateLoanSchema}>
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
              <BgtFormField form={form} name="playerId" schema={CreateLoanSchema}>
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
              <BgtFormField form={form} name="loanDate" schema={CreateLoanSchema}>
                {(field) => (
                  <BgtDatePicker
                    field={field}
                    label={t('loan.new.start.label')}
                    disabled={isLoading}
                    placeholder={t('loan.new.start.placeholder')}
                  />
                )}
              </BgtFormField>
              <BgtFormField form={form} name="dueDate" schema={CreateLoanSchema}>
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
            <BgtButton disabled={isLoading} variant="cancel" className="flex-1" onClick={close}>
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
        close={() => setOpenCreatePlayerModal(false)}
        onPlayerCreated={handlePlayerCreated}
      />
    </BgtDialog>
  );
};

export default NewLoanModal;
