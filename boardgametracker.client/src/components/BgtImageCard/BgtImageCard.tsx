import { useTranslation } from 'react-i18next';
import { cx } from 'class-variance-authority';
import { Link } from '@tanstack/react-router';

import { BgtText } from '../BgtText/BgtText';
import { getColorFromGameState, getItemStateTranslationKey } from '../../utils/ItemStateUtils';

import { StringToRgb } from '@/utils/stringUtils';
import { GameState } from '@/models';

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
    <Link to={link} from="/">
      <div className="flex flex-col justify-center cursor-pointer flex-nowrap relative gap-1 group">
        <div className="aspect-square rounded-lg overflow-hidden mb-3 bg-[#8502fb]/10 border border-[#8502fb]/10 group-hover:border-[#8502fb]/30 transition-all duration-200 relative">
          <div
            style={{ '--image-url': `url(${image})`, '--fallback-color': StringToRgb(title) }}
            className={cx(
              'w-full overflow-hidden aspect-square z-10 rounded-xl flex flex-col justify-center relative',
              `bg-cover bg-no-repeat bg-center object-cover group-hover:scale-105 transition-transform duration-200`,
              image && 'bg-[image:var(--image-url)]',
              !image && `bg-[var(--fallback-color)]`
            )}
          >
            {!image && (
              <span className="flex justify-center align-middle h-max font-bold text-3xl capitalize">{title[0]}</span>
            )}
          </div>
        </div>
        <div className="flex flex-row justify-between items-end">
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
            <BgtText size="4" className="line-clamp-1 uppercase w-full group-hover:text-primary-light" weight="medium">
              {title}
            </BgtText>
          </div>
        </div>
      </div>
    </Link>
  );
};
