import { useTranslation } from 'react-i18next';
import clsx from 'clsx';
import { Badge, Heading } from '@radix-ui/themes';

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
    <div className="flex flex-col justify-center cursor-pointer flex-nowrap relative items-center">
      <div
        style={{ '--image-url': `url(${image})`, '--fallback-color': StringToRgb(title) }}
        className={clsx(
          'w-full relative overflow-hidden aspect-square z-10 rounded-xl flex justify-end flex-col gap-3 pb-4 px-3',
          'before:absolute before:inset-0 before:block before:bg-gradient-to-t before:from-black before:z-[-5] before:hover:from-gray-900',
          `bg-cover bg-no-repeat bg-center`,
          image && 'bg-[image:var(--image-url)]',
          !image && `bg-[var(--fallback-color)]`
        )}
      >
        {state !== null && (
          <div className="flex justify-center">
            <Badge variant="surface" radius="full" size="2" color={getColorFromGameState(state)}>
              {t(getItemStateTranslationKey(state))}
            </Badge>
          </div>
        )}

        <Heading align="center" as="h4" size="4" className="line-clamp-1" trim="start">
          {title}
        </Heading>
      </div>
    </div>
  );
};
