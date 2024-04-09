import i18next from 'i18next';
import {useTranslation} from 'react-i18next';

import {Badge, Button, Dialog, Text} from '@radix-ui/themes';

import {useGame} from '../../../hooks/useGame';
import {RoundDecimal} from '../../../utils/roundDecimal';

interface Props {
  id: string | undefined;
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

const sanitiseValues = (val1: number | null, val2: number | null): string | null => {
  if (val1 === val2) {
    return val1?.toString() ?? null
  }

  return `${val1} - ${val2}`
}

export const GameDetailsPopup = (props: Props) => {
  const { id, open, setOpen } = props;
  const { game } = useGame(id);
  const { t } = useTranslation();

  if (game === undefined || !open) return null;

  return (
    <Dialog.Root open={open}>
      <Dialog.Content>
        <Dialog.Title>
          {t('game.details.title', { title: game.title })}
        </Dialog.Title>
        <div className='flex flex-col gap-3 mt-3 mb-6'>
          <div className='flex flex-col'>
            <Text>{i18next.format(t('common.details'), 'capitalize')}</Text>
            <div className='flex flex-row gap-1 flex-wrap'>
              <Badge color="green">{t('common.play-time')}: {sanitiseValues(game.minPlayTime, game.maxPlayTime)} {t('common.minutes_abbreviation')}</Badge>
              <Badge color="green">{t('common.players')}: {sanitiseValues(game.minPlayers, game.maxPlayers)}</Badge>
              <Badge color="green">{t('common.rating')}: {game.rating?.toFixed(1)}/10</Badge>
              <Badge color="green">{t('common.weight')}: {RoundDecimal(game.weight)}/5</Badge>
              <Badge color="green">{t('common.age')}: {game.minAge}</Badge>
            </div>
          </div>
          <div className='flex flex-col'>
            <Text>{i18next.format(t('common.categories'), 'capitalize')}</Text>
            <div className='flex flex-row gap-1 flex-wrap'>
              {game.categories.map(x => <Badge color="green" key={x.id}>{x.name}</Badge>)}
            </div>
          </div>
          <div className='flex flex-col'>
            <Text>{i18next.format(t('common.mechanics'), 'capitalize')}</Text>
            <div className='flex flex-row gap-1 flex-wrap'>
              {game.mechanics.map(x => <Badge color="green" key={x.id}>{x.name}</Badge>)}
            </div>
          </div>
        </div>
        <div className='flex justify-end gap-3'>
          <Dialog.Close>
            <Button variant='surface' color='gray' onClick={() => setOpen(false)}>
              {t('common.close')}
            </Button>
          </Dialog.Close>
        </div>
      </Dialog.Content>
    </Dialog.Root >
  )
}