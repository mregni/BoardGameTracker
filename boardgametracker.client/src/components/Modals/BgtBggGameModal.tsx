import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { Dispatch, SetStateAction } from 'react';
import { Dialog, Button } from '@radix-ui/themes';
import * as Form from '@radix-ui/react-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowTopRightOnSquareIcon } from '@heroicons/react/24/outline';

import { BgtSwitch } from '../BgtSwitch/BgtSwitch';
import { BgtIcon } from '../BgtIcon/BgtIcon';
import { BgtSelect } from '../BgtForm/BgtSelect';
import { BgtInputField } from '../BgtForm/BgtInputField';
import BgtButton from '../BgtButton/BgtButton';
import { getItemStateTranslationKeyByString } from '../../utils/ItemStateUtils';
import { BggSearch, BggSearchSchema, GameState } from '../../models';
import { useSettings } from '../../hooks/useSettings';
import { useGames } from '../../hooks/useGames';

interface Props {
  open: boolean;
  setOpen: Dispatch<SetStateAction<boolean>>;
}

export const BgtBggGameModal = (props: Props) => {
  const { open, setOpen } = props;
  const { t } = useTranslation();
  const { settings } = useSettings();
  const { save, saveIsPending } = useGames();

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
    <Dialog.Root open={open}>
      <Dialog.Content>
        <Dialog.Title>{t('game.new.title')}</Dialog.Title>
        <Dialog.Description>{t('game.new.bgg-description')}</Dialog.Description>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
          <div className="flex flex-col gap-4 mt-3 mb-6">
            <BgtButton onClick={() => openBgg()}>
              <BgtIcon icon={<ArrowTopRightOnSquareIcon />} size={18} />
              <>{t('game.bgg.external-page')}</>
            </BgtButton>
            <BgtInputField
              disabled={saveIsPending}
              label={t('game.bgg.label')}
              name="bggId"
              type="text"
              placeholder={t('game.bgg.placeholder')}
              control={control}
            />
            <BgtInputField
              disabled={saveIsPending}
              label={t('game.price.label')}
              name="price"
              type="number"
              placeholder={t('game.price.placeholder')}
              control={control}
              prefixLabel={settings?.currency}
            />
            <BgtInputField
              disabled={saveIsPending}
              label={t('game.added-date.label')}
              name="date"
              type="date"
              control={control}
              className="pr-2"
            />
            <BgtSelect
              disabled={saveIsPending}
              control={control}
              label={t('game.state.label')}
              name="state"
              items={Object.keys(GameState)
                .filter((value) => !isNaN(Number(value)))
                .map((value) => ({ label: t(getItemStateTranslationKeyByString(value)), value: value }))}
            />
            <BgtSwitch label={t('game.scoring.label')} disabled={saveIsPending} control={control} name="hasScoring" />
          </div>
          <div className="flex justify-end gap-3">
            <Dialog.Close>
              <>
                <Form.Submit>
                  <BgtButton type="submit" variant="soft" disabled={saveIsPending}>
                    {t('game.new.save')}
                  </BgtButton>
                </Form.Submit>
                <BgtButton variant="outline" onClick={() => setOpen(false)} disabled={saveIsPending}>
                  {t('common.cancel')}
                </BgtButton>
              </>
            </Dialog.Close>
          </div>
        </form>
      </Dialog.Content>
    </Dialog.Root>
  );
};
