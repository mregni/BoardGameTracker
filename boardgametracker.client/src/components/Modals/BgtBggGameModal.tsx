import {useForm} from 'react-hook-form';
import {useTranslation} from 'react-i18next';

import {ArrowTopRightOnSquareIcon} from '@heroicons/react/24/outline';
import {zodResolver} from '@hookform/resolvers/zod';
import * as Form from '@radix-ui/react-form';
import {Button, Dialog} from '@radix-ui/themes';

import {useGames} from '../../hooks/useGames';
import {useSettings} from '../../hooks/useSettings';
import {BggSearch, BggSearchSchema, GameState} from '../../models';
import {getItemStateTranslationKeyByString} from '../../utils/getItemStateTranslationKey';
import {BgtInputField} from '../BgtForm/BgtInputField';
import {BgtSelect} from '../BgtForm/BgtSelect';
import {BgtIcon} from '../BgtIcon/BgtIcon';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

export const BgtBggGameModal = (props: Props) => {
  const { open, setOpen } = props;
  const { t } = useTranslation();
  const { settings } = useSettings();
  const { save, saveIsPending } = useGames();

  const openBgg = () => {
    window.open('https://boardgamegeek.com/browse/boardgame', '_blank');
  }

  const { register, handleSubmit, formState: { errors }, control, getValues } = useForm<BggSearch>({
    resolver: zodResolver(BggSearchSchema),
    defaultValues: {
      state: GameState.Owned
    }
  });

  const onSubmit = async (data: BggSearch) => {
    await save(data);
  };

  return (
    <Dialog.Root open={open}>
      <Dialog.Content>
        <Dialog.Title>
          {t('game.new.title')}
        </Dialog.Title>
        <Dialog.Description>
          {t('game.new.bgg-description')}
        </Dialog.Description>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
          <div className='flex flex-col gap-4 mt-3 mb-6' >
            <Button onClick={openBgg}>
              <BgtIcon icon={<ArrowTopRightOnSquareIcon />} width={18} height={18} />
              {t('game.bgg.external-page')}
            </Button>
            <BgtInputField
              disabled={saveIsPending}
              label={t('game.bgg.label')}
              name='bggId'
              type='text'
              placeholder={t('game.bgg.placeholder')}
              register={register}
              errors={errors}
            />
            <BgtInputField
              disabled={saveIsPending}
              label={t('game.price.label')}
              name='price'
              type='number'
              placeholder={t('game.price.placeholder')}
              register={register}
              errors={errors}
              prefixLabel={settings?.currency}
            />
            <BgtInputField
              disabled={saveIsPending}
              label={t('game.added-date.label')}
              name='date'
              type='date'
              register={register}
              errors={errors}
              className="pr-2"
            />
            <BgtSelect
              disabled={saveIsPending}
              defaultValue={getValues('state').toString()}
              control={control}
              label={t('game.state.label')}
              name='state'
              items={Object
                .keys(GameState)
                .filter(value => !isNaN(Number(value)))
                .map((value) => ({ label: t(getItemStateTranslationKeyByString(value)), value: value }))}
            />
          </div>
          <div className='flex justify-end gap-3'>
            <Dialog.Close>
              <>
                <Form.Submit asChild>
                  <Button type='submit' variant='surface' color='orange' disabled={saveIsPending}>
                    {t('game.new.save')}
                  </Button>
                </Form.Submit>
                <Button variant='surface' color='gray' onClick={() => setOpen(false)} disabled={saveIsPending}>
                  {t('common.cancel')}
                </Button>
              </>
            </Dialog.Close>
          </div>
        </form>
      </Dialog.Content>
    </Dialog.Root >
  )
}