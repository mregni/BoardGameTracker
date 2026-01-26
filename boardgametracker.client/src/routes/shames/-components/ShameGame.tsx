import { useTranslation } from 'react-i18next';
import { format } from 'date-fns';
import { cx } from 'class-variance-authority';
import { Link } from '@tanstack/react-router';

import { StringToRgb } from '@/utils/stringUtils';
import { getDaysSincePurchase } from '@/utils/dateUtils';
import { Shame } from '@/models';
import { BgtText } from '@/components/BgtText/BgtText';
import Coins from '@/assets/icons/coins.svg?react';
import Calendar from '@/assets/icons/calendar.svg?react';

interface Props {
  shame: Shame;
  dateFormat: string;
  currency: string;
}

const ShameDataLines = ({ content, title }: { content: string; title: string }) => {
  return (
    <div className="flex items-center justify-between text-xs">
      <BgtText color="white" opacity={50} size="2" className="flex items-center gap-1">
        <Calendar className="size-4" />
        <span>{title}</span>
      </BgtText>
      <BgtText color="white" size="2" weight="bold">
        {content}
      </BgtText>
    </div>
  );
};

export const ShameGame = ({ shame, dateFormat, currency }: Props) => {
  const { t } = useTranslation();
  const link = `/games/${shame.id}`;

  const daysLastSession = getDaysSincePurchase(shame.lastSessionDate);

  return (
    <Link to={link} from="/shames">
      <div className="flex flex-col justify-center cursor-pointer flex-nowrap relative group gap-1 bg-primary/20 rounded-lg hover:border-primary/50 transition-all border border-white/10">
        <div className="aspect-square overflow-hidden border border-none transition-all duration-200 relative rounded-t-lg">
          <div
            style={
              {
                '--image-url': `url(${shame.image})`,
                '--fallback-color': StringToRgb(shame.title),
              } as React.CSSProperties
            }
            className={cx(
              'w-full overflow-hidden aspect-square z-10 flex flex-col justify-center relative',
              'bg-cover bg-no-repeat bg-center object-cover group-hover:scale-105 transition-transform duration-200',
              shame.image && 'bg-(image:--image-url)',
              !shame.image && 'bg-(--fallback-color)'
            )}
          >
            {!shame.image && (
              <span className="flex justify-center align-middle h-max font-bold text-3xl capitalize">
                {shame.title[0]}
              </span>
            )}
            <div className="absolute top-2 right-2 bg-red-500/90 backdrop-blur-sm px-2 py-1 rounded-lg">
              <p className="text-xs font-bold text-white">
                {daysLastSession > 0 ? t('common.day', { count: daysLastSession }) : t('common.never')}
              </p>
            </div>
          </div>
        </div>
        <div className="flex flex-col items-start justify-start pb-2 px-2">
          <BgtText size="4" className="line-clamp-1 uppercase w-full" weight="medium">
            {shame.title}
          </BgtText>
          <div className="flex flex-col w-full gap-1">
            <ShameDataLines
              content={
                shame.lastSessionDate !== null ? format(new Date(shame.lastSessionDate), dateFormat) : t('common.never')
              }
              title={t('shames.last-session')}
            />
            <div className="flex items-center justify-between text-xs">
              <BgtText color="white" opacity={50} size="2" className="flex items-center gap-1">
                <Coins className="size-4" />
                <span>{t('common.price')}</span>
              </BgtText>
              <BgtText color="cyan" weight="bold" size="2">
                {currency} {shame.price}
              </BgtText>
            </div>
          </div>
        </div>
      </div>
    </Link>
  );
};
