import { t } from 'i18next';
import { differenceInDays, format } from 'date-fns';
import { isAfter } from 'date-fns';
import { cva } from 'class-variance-authority';

import { BgtPoster } from '../../-components/BgtPoster';

import type { Player } from '@/models/Player/Player';
import type { Loan } from '@/models/Loan/Loan';
import type { Game } from '@/models/Games/Game';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import { BgtAvatar } from '@/components/BgtAvatar/BgtAvatar';
import Trash from '@/assets/icons/trash.svg?react';
import Clock from '@/assets/icons/clock.svg?react';
import Check from '@/assets/icons/check.svg?react';
import Calendar from '@/assets/icons/calendar.svg?react';
import AlertTriangle from '@/assets/icons/alert-triangle.svg?react';

const loanCardVariants = cva('', {
  variants: {
    status: {
      active: 'bg-primary/10 border border-primary/30 hover:border-primary/50 hover:shadow-primary/10',
      overdue: 'bg-error/10 border-2 border-error/40 hover:border-error/60',
      inactive: 'opacity-80 hover:opacity-100 transition-opacity',
    },
  },
  defaultVariants: {
    status: 'active',
  },
});

const clockIconVariants = cva('size-5', {
  variants: {
    overdue: {
      true: 'text-error',
      false: 'text-primary/70',
    },
  },
  defaultVariants: {
    overdue: false,
  },
});

interface LoanCardProps {
  loan: Loan;
  game: Game | undefined;
  player: Player | undefined;
  dateFormat: string;
  onReturn?: (loanId: number, returnDate: Date) => void;
  onDelete: (loanId: number) => void;
}

export const LoanCard = ({ loan, game, player, dateFormat, onReturn, onDelete }: LoanCardProps) => {
  const isActive = loan.returnedDate === null;
  const isOverdue = loan.dueDate !== null && isAfter(new Date(), loan.dueDate);
  const daysOut = isActive
    ? differenceInDays(new Date(), new Date(loan.loanDate))
    : differenceInDays(new Date(loan.returnedDate!), new Date(loan.loanDate));

  const cardStatus = !isActive ? 'inactive' : isOverdue ? 'overdue' : 'active';

  return (
    <BgtCard key={loan.id} className={loanCardVariants({ status: cardStatus })}>
      {isOverdue && isActive && (
        <div className="flex items-center gap-2 mb-3 bg-error/20 border border-error/30 rounded-lg px-3 py-1.5">
          <AlertTriangle className="text-error text-xl" />
          <BgtText color="red" className="uppercase tracking-wider">
            {t('common.overdue')}
          </BgtText>
        </div>
      )}

      <div className="flex items-center gap-3 mb-4">
        <div className="w-12 h-12 bg-primary/20 rounded-lg flex items-center justify-center text-2xl">
          <BgtPoster title={game?.title || ''} image={game?.image ?? null} />
        </div>
        <div className="flex-1">
          <BgtHeading size="4" className="text-white">
            {game?.title}
          </BgtHeading>
        </div>
      </div>

      <div className="flex items-center gap-2 mb-4 pb-4 border-b border-primary/20">
        <BgtAvatar title={player?.name || ''} image={player?.image} />
        <BgtText color="white" opacity={80}>
          {player?.name}
        </BgtText>
      </div>

      <div className="space-y-2 mb-4">
        <div className="flex items-center gap-2 text-sm">
          <Calendar className="text-primary/70 size-5" />
          <BgtText color="white" opacity={60}>
            {t('common.loaned')}:
          </BgtText>
          <BgtText color="white" opacity={80}>
            {format(loan.loanDate, dateFormat)}
          </BgtText>
        </div>
        {loan.dueDate && (
          <div className="flex items-center gap-2 text-sm">
            <Calendar className="text-primary/70 size-5" />
            <BgtText color="white" opacity={60}>
              {t('common.due-date')}:
            </BgtText>
            <BgtText color="white" opacity={80}>
              {format(loan.dueDate, dateFormat)}
            </BgtText>
          </div>
        )}
        {daysOut > 0 && (
          <div className="flex items-center gap-2 text-sm">
            <Clock className={clockIconVariants({ overdue: isActive && isOverdue })} />
            <BgtText color="white" opacity={60}>
              {t('common.duration')}:
            </BgtText>
            <BgtText color={isActive && isOverdue ? 'red' : 'cyan'}>
              {isActive ? t('common.day', { count: daysOut }) : daysOut}
            </BgtText>
          </div>
        )}
      </div>

      <div className="mt-auto flex flex-row gap-2">
        {isActive && onReturn && (
          <button
            onClick={() => onReturn(loan.id, new Date())}
            className="w-full bg-lime-green/20 hover:bg-lime-green/30 text-lime-green border border-lime-green/30 py-1 rounded-lg flex items-center justify-center gap-2 transition-colors"
          >
            <Check className="size-5" />
            {t('loan.mark-as-returned')}
          </button>
        )}

        <button
          onClick={() => onDelete(loan.id)}
          className="w-full bg-error/20 hover:bg-error/30 text-error border border-error/30 py-1 rounded-lg flex items-center justify-center gap-2"
        >
          <Trash className="size-5" />
          {t('loan.delete.button')}
        </button>
      </div>
    </BgtCard>
  );
};
