import { useTranslation } from 'react-i18next';
import { useCallback, Dispatch, SetStateAction } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { useForm } from '@tanstack/react-form';

import { useBggGameModal } from '../-hooks/useBggGameModal';

import { getItemStateTranslationKeyByString } from '@/utils/ItemStateUtils';
import { toInputDate } from '@/utils/dateUtils';
import { useToasts } from '@/routes/-hooks/useToasts';
import { BggSearchSchema, Game, GameState } from '@/models';
import { BgtFormField, BgtSwitch, BgtSelect, BgtInputField, BgtDatePicker } from '@/components/BgtForm';
import {
  BgtDialog,
  BgtDialogClose,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogTitle,
} from '@/components/BgtDialog/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';
import SquareOutIcon from '@/assets/icons/square-out.svg?react';

interface Props {
  open: boolean;
  setOpen: Dispatch<SetStateAction<boolean>>;
}

export const BggGameModal = (props: Props) => {
  const { open, setOpen } = props;
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { successToast } = useToasts();

  const onSuccess = useCallback(
    (game: Game) => {
      successToast('game.notifications.created');
      navigate({ to: `/games/${game.id}` });
    },
    [successToast, navigate]
  );

  const { save, isPending, settings } = useBggGameModal({ onSuccess });

  const openBgg = useCallback(() => {
    window.open('https://boardgamegeek.com/browse/boardgame', '_blank');
  }, []);

  const handleClose = useCallback(() => {
    setOpen(false);
  }, [setOpen]);

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
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('game.new.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('game.new.bgg-description')}</BgtDialogDescription>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
        >
          <div className="flex flex-col gap-4 mt-3 mb-6">
            <BgtButton onClick={openBgg} disabled={isPending} variant="primary" type="button">
              <SquareOutIcon className="size-5" />
              <>{t('game.bgg.external-page')}</>
            </BgtButton>
            <BgtFormField form={form} name="bggId" schema={BggSearchSchema.shape.bggId}>
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
            <BgtFormField form={form} name="price" schema={BggSearchSchema.shape.price}>
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
            <BgtFormField form={form} name="date" schema={BggSearchSchema.shape.date}>
              {(field) => (
                <BgtDatePicker
                  field={field}
                  disabled={isPending}
                  label={t('game.added-date.label')}
                  placeholder={t('game.added-date.placeholder')}
                />
              )}
            </BgtFormField>
            <BgtFormField form={form} name="state" schema={BggSearchSchema.shape.state}>
              {(field) => (
                <BgtSelect
                  field={field}
                  disabled={isPending}
                  label={t('game.state.label')}
                  items={Object.keys(GameState)
                    .filter((value) => !Number.isNaN(Number(value)))
                    .map((value) => ({ label: t(getItemStateTranslationKeyByString(value)), value: value }))}
                />
              )}
            </BgtFormField>
            <BgtFormField form={form} name="hasScoring" schema={BggSearchSchema.shape.hasScoring}>
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
