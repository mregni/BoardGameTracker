import { useTranslation } from 'react-i18next';
import clsx from 'clsx';
import { Text } from '@radix-ui/themes';

import { BgtText } from '../BgtText/BgtText';
import { StringToRgb } from '../../utils/stringUtils';
import { getItemStateTranslationKey } from '../../utils/ItemStateUtils';
import { GameState } from '../../models';

interface Props {
  title: string;
  state?: GameState;
  image: string | null;
}

const getColorFromGameState = (state: GameState): 'amber' | 'orange' | 'red' | 'purple' | 'blue' | 'green' => {
  switch (state) {
    case GameState.Wanted:
      return 'amber';
    case GameState.Owned:
      return 'green';
    case GameState.PreviouslyOwned:
      return 'red';
    case GameState.NotOwned:
      return 'purple';
    case GameState.ForTrade:
      return 'blue';
    case GameState.OnLoan:
    default:
      return 'orange';
  }
};

export const BgtImageCard = (props: Props) => {
  const { title, image, state = null } = props;
  const { t } = useTranslation();

  return (
    <div className="flex flex-col justify-center cursor-pointer flex-nowrap relative gap-1">
      <div
        style={{ '--image-url': `url(${image})`, '--fallback-color': StringToRgb(title) }}
        className={clsx(
          'w-full relative overflow-hidden aspect-square z-10 rounded-xl flex justify-end flex-col gap-3 pb-4 px-3',
          `bg-cover bg-no-repeat bg-center`,
          image && 'bg-[image:var(--image-url)]',
          !image && `bg-[var(--fallback-color)]`
        )}
      ></div>
      <div className="flex flex-col items-start justify-start">
        {state !== null && (
          <BgtText
            size="1"
            className="line-clamp-1 uppercase w-full"
            weight="medium"
            color={getColorFromGameState(state)}
          >
            {t(getItemStateTranslationKey(state))}
          </BgtText>
        )}

        <BgtText size="4" className="line-clamp-1 uppercase w-full" weight="medium">
          {title}
        </BgtText>
      </div>
    </div>
  );
};
