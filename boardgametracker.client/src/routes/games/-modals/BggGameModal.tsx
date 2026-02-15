import { useTranslation } from 'react-i18next';
import { useCallback } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';

import { useBggGameModal } from '../-hooks/useBggGameModal';

import { getItemStateTranslationKey } from '@/utils/ItemStateUtils';
import { handleFormSubmit } from '@/utils/formUtils';
import { toInputDate } from '@/utils/dateUtils';
import { BggSearchSchema, Game, GameState, ModalProps } from '@/models';
import { BgtFormField, BgtSwitch, BgtSelect, BgtInputField, BgtDatePicker } from '@/components/BgtForm';
import {
  BgtDialog,
  BgtDialogClose,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogTitle,
} from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';
import SquareOutIcon from '@/assets/icons/square-out.svg?react';

export const BggGameModal = (props: ModalProps) => {
  const { open, close } = props;
  const { t } = useTranslation();
  const navigate = useNavigate();

  const onSuccess = useCallback(
    (game: Game) => {
      navigate({ to: `/games/${game.id}` });
    },
    [navigate]
  );

  const { save, isPending, settings } = useBggGameModal({ onSuccess });

  const openBgg = useCallback(() => {
    window.open('https://boardgamegeek.com/browse/boardgame', '_blank');
  }, []);

  const handleClose = useCallback(() => {
    close();
  }, [close]);

  const form = useForm({
    defaultValues: {
      bggId: '',
      price: 0,
      date: toInputDate(undefined, true),
      state: GameState.Owned,
      hasScoring: true,
    },
    onSubmit: async ({ value }) => {
      const validatedData = BggSearchSchema.parse(value);
      await save(validatedData);
    },
  });

  return (
    <BgtDialog open={open} onClose={handleClose}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('game.new.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('game.new.bgg-description')}</BgtDialogDescription>
        <form
          onSubmit={handleFormSubmit(form)}
        >
          <div className="flex flex-col gap-4 mt-3 mb-6">
            <BgtButton onClick={openBgg} disabled={isPending} variant="primary" type="button">
              <SquareOutIcon className="size-5" />
              <>{t('game.bgg.external-page')}</>
            </BgtButton>
            <BgtFormField form={form} name="bggId" schema={BggSearchSchema}>
              {(field) => (
                <BgtInputField
                  field={field}
                  disabled={isPending}
                  label={t('game.bgg.label')}
                  type="text"
                  placeholder={t('game.bgg.placeholder')}
                />
              )}
            </BgtFormField>
            <BgtFormField form={form} name="price" schema={BggSearchSchema}>
              {(field) => (
                <BgtInputField
                  field={field}
                  disabled={isPending}
                  label={t('game.price.label')}
                  type="number"
                  placeholder={t('game.price.placeholder')}
                  prefixLabel={settings?.currency}
                />
              )}
            </BgtFormField>
            <BgtFormField form={form} name="date" schema={BggSearchSchema}>
              {(field) => (
                <BgtDatePicker
                  field={field}
                  disabled={isPending}
                  label={t('game.added-date.label')}
                  placeholder={t('game.added-date.placeholder')}
                />
              )}
            </BgtFormField>
            <BgtFormField form={form} name="state" schema={BggSearchSchema}>
              {(field) => (
                <BgtSelect
                  field={field}
                  disabled={isPending}
                  label={t('game.state.label')}
                  items={Object.values(GameState).map((value) => ({
                    label: t(getItemStateTranslationKey(value, false)),
                    value: value,
                  }))}
                />
              )}
            </BgtFormField>
            <BgtFormField form={form} name="hasScoring" schema={BggSearchSchema}>
              {(field) => <BgtSwitch field={field} label={t('game.scoring.label')} disabled={isPending} />}
            </BgtFormField>
          </div>
          <BgtDialogClose>
            <BgtButton variant="cancel" onClick={handleClose} disabled={isPending}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" variant="primary" disabled={isPending} className="flex-1">
              {t('game.new.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
