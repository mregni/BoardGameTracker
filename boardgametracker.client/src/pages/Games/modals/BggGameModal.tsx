import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { Dispatch, SetStateAction } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';

import { useBggGameModal } from '../hooks/useBggGameModal';

import { getItemStateTranslationKeyByString } from '@/utils/ItemStateUtils';
import { useToast } from '@/providers/BgtToastProvider';
import { BggSearch, BggSearchSchema, Game, GameState } from '@/models';
import { useSettings } from '@/hooks/useSettings';
import { BgtSwitch } from '@/components/BgtSwitch/BgtSwitch';
import { BgtSelect } from '@/components/BgtForm/BgtSelect';
import { BgtInputField } from '@/components/BgtForm/BgtInputField';
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
  const { settings } = useSettings();
  const navigate = useNavigate();
  const { showInfoToast } = useToast();

  const onSuccess = (game: Game) => {
    showInfoToast('game.notifications.created');
    navigate(`/games/${game.id}`);
  };
  const { save, isPending } = useBggGameModal({ onSuccess });

  const openBgg = () => {
    window.open('https://boardgamegeek.com/browse/boardgame', '_blank');
  };

  const { handleSubmit, control } = useForm<BggSearch>({
    resolver: zodResolver(BggSearchSchema),
    defaultValues: {
      state: GameState.Owned,
      hasScoring: true,
    },
  });

  const onSubmit = async (data: BggSearch) => {
    await save(data);
  };

  return (
    <BgtDialog open={open}>
      <BgtDialogContent>
        <BgtDialogTitle>{t('game.new.title')}</BgtDialogTitle>
        <BgtDialogDescription>{t('game.new.bgg-description')}</BgtDialogDescription>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
          <div className="flex flex-col gap-4 mt-3 mb-6">
            <BgtButton onClick={() => openBgg()} disabled={isPending}>
              <SquareOutIcon className="size-5" />
              <>{t('game.bgg.external-page')}</>
            </BgtButton>
            <BgtInputField
              disabled={isPending}
              label={t('game.bgg.label')}
              name="bggId"
              type="text"
              placeholder={t('game.bgg.placeholder')}
              control={control}
            />
            <BgtInputField
              disabled={isPending}
              label={t('game.price.label')}
              name="price"
              type="number"
              placeholder={t('game.price.placeholder')}
              control={control}
              prefixLabel={settings.data?.currency}
            />
            <BgtInputField
              disabled={isPending}
              label={t('game.added-date.label')}
              name="date"
              type="date"
              control={control}
              className="pr-2"
            />
            <BgtSelect
              disabled={isPending}
              control={control}
              label={t('game.state.label')}
              name="state"
              items={Object.keys(GameState)
                .filter((value) => !isNaN(Number(value)))
                .map((value) => ({ label: t(getItemStateTranslationKeyByString(value)), value: value }))}
            />
            <BgtSwitch label={t('game.scoring.label')} disabled={isPending} control={control} name="hasScoring" />
          </div>
          <BgtDialogClose>
            <BgtButton variant="soft" color="cancel" onClick={() => setOpen(false)} disabled={isPending}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" variant="soft" disabled={isPending}>
              {t('game.new.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
