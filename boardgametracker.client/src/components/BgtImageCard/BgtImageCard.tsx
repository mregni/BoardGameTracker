import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import clsx from 'clsx';
import { EllipsisVerticalIcon } from '@heroicons/react/24/outline';

import { BgtText } from '../BgtText/BgtText';
import { BgtIcon } from '../BgtIcon/BgtIcon';
import BgtButton from '../BgtButton/BgtButton';
import { StringToRgb } from '../../utils/stringUtils';
import { getColorFromGameState, getItemStateTranslationKey } from '../../utils/ItemStateUtils';
import { GameState } from '../../models';

interface Props {
  title: string;
  state?: GameState;
  image: string | null;
  link: string;
}

export const BgtImageCard = (props: Props) => {
  const { title, image, state = null, link } = props;
  const { t } = useTranslation();

  return (
    <div className="flex flex-col justify-center cursor-pointer flex-nowrap relative gap-1 group">
      <Link to={link}>
        <div
          style={{ '--image-url': `url(${image})`, '--fallback-color': StringToRgb(title) }}
          className={clsx(
            'w-full relative overflow-hidden aspect-square z-10 rounded-xl flex justify-end flex-col gap-3 pb-4 px-3',
            `bg-cover bg-no-repeat bg-center`,
            image && 'bg-[image:var(--image-url)]',
            !image && `bg-[var(--fallback-color)]`
          )}
        ></div>
      </Link>
      <div className="flex flex-row justify-between items-end">
        <div className="flex flex-col items-start justify-start">
          <Link to={link}>
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
          </Link>
        </div>
        <BgtButton
          size="1"
          variant="inline"
          onClick={() => alert(title)}
          className="opacity-0 group-hover:opacity-100 transition-opacity duration-200"
        >
          <BgtIcon icon={<EllipsisVerticalIcon />} size={15} />
        </BgtButton>
      </div>
    </div>
  );
};
