import clsx from 'clsx';
import {useTranslation} from 'react-i18next';

import {Text} from '@radix-ui/themes';

import {GameState} from '../../models';
import {getItemStateTranslationKey} from '../../utils/getItemStateTranslationKey';
import {BgtImageFallback} from './BgtImageFallback';

interface Props {
  title: string;
  image: string | null;
  state?: GameState;
}

const getColorFromGameState = (state: GameState): string => {
  switch (state) {
    case GameState.Wanted:
      return 'bg-red-700';
    case GameState.Owned:
      return 'bg-green-600';
    case GameState.PreviouslyOwned:
      return 'bg-violet-600';
    case GameState.NotOwned:
      return 'bg-orange-900';
    case GameState.ForTrade:
      return 'bg-blue-600';
    default:
      return ''
  }
}

export const BgtImageCard = (props: Props) => {
  const { title, image, state = null } = props;
  const { t } = useTranslation();

  return (
    <div className="flex flex-col justify-center transition-transform hover:scale-95 cursor-pointer gap-1 flex-nowrap">
      <div>
        {
          image && (
            <img
              src={image}
              alt={`poster for ${title}`}
              className={
                clsx("shadow-black drop-shadow-md w-full",
                  state !== null && "rounded-t-md",
                  state === null && "rounded-md"
                )
              }
            />
          )
        }
        <BgtImageFallback display={!image} title={title} roundBottom={state === null} fullWidth />
        {state !== null && <Text
          align="center"
          as='div'
          size="1"
          className={
            clsx("rounded-b-md", getColorFromGameState(state))
          }>
          {t(getItemStateTranslationKey(state))}
        </Text>}
      </div>

      <Text align="center" as='p' size="1" className='line-clamp-1'>{title}</Text>
    </div>
  )
}