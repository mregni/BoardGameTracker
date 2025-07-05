import { useTranslation } from 'react-i18next';
import { ComponentPropsWithoutRef } from 'react';
import { cx } from 'class-variance-authority';

import { CompareRow, Player } from '@/models';
import Throphy from '@/assets/throphy.svg?react';
import Cross from '@/assets/cross.svg?react';

interface RowProps extends ComponentPropsWithoutRef<'div'> {
  row: CompareRow<number>;
  position: 'left' | 'right';
  playerLeft: Player;
  playerRight: Player;
}

export const TotalDuration = ({ row, position, playerLeft, playerRight, className, ...props }: RowProps) => {
  const { t } = useTranslation();

  const isLeftPosition = position === 'left';
  const currentDuration = isLeftPosition ? row.left : row.right;
  const otherDuration = isLeftPosition ? row.right : row.left;
  const currentPlayer = isLeftPosition ? playerLeft : playerRight;

  const isWinner = currentDuration > otherDuration;
  const percentageLost = ((1 - currentDuration / otherDuration) * 100).toFixed(0);

  const Icon = isWinner ? Throphy : Cross;
  const classes = isWinner ? 'size-7' : 'size-5 mt-1';

  const shouldIconBeFirst = !isLeftPosition;

  return (
    <div className={cx('flex flex-row gap-2 items-start h-10', className)} {...props}>
      {shouldIconBeFirst && <Icon className={`${classes} inline`} />}
      <div className="flex flex-col">
        <span>{t('compare.total-duration.label', { player: currentPlayer.name, duration: currentDuration })}</span>
        {!isWinner && <span className="text-xs text-red-500 w-full text-right">{`-${percentageLost}%`}</span>}
      </div>
      {!shouldIconBeFirst && <Icon className={`${classes} inline`} />}
    </div>
  );
};
