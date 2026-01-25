import { useState } from 'react';
import { t } from 'i18next';
import { createFileRoute } from '@tanstack/react-router';

import { useToasts } from '../-hooks/useToasts';
import { usePlayerById } from '../-hooks/usePlayerById';
import { useGameById } from '../-hooks/useGameById';

import NewLoanModal from './-modals/NewLoanModal';
import { useLoans } from './-hooks/useLoans';
import { useLoanModals } from './-hooks/useLoanModels';
import { useLoanActions } from './-hooks/useLoanActions';
import { LoanCard } from './-components/LoanCard';

import { getLoans } from '@/services/queries/loans';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtTextStatistic } from '@/components/BgtStatistic/BgtTextStatistic';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import Package from '@/assets/icons/package.svg?react';
import History from '@/assets/icons/history.svg?react';

export const Route = createFileRoute('/loans/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getLoans());
  },
});

function RouteComponent() {
  const [openModal, setOpenModal] = useState(false);
  const { errorToast, successToast } = useToasts();
  const { loans, settings, deleteLoan, returnLoan, isLoading } = useLoans({
    onDeleteError: () => errorToast('loan.delete.failed'),
    onDeleteSuccess: () => successToast('loan.delete.successfull'),
    onReturnError: (text?: string) => errorToast(text || 'loan.return.failed'),
    onReturnSuccess: () => successToast('loan.return.successfull'),
  });
  const { gameById } = useGameById();
  const { playerById } = usePlayerById();

  const modals = useLoanModals();

  const actions = useLoanActions({
    deleteLoan,
    returnLoan: returnLoan,
    onDeleteModalClose: modals.deleteModal.hide,
  });

  return (
    <BgtPage>
      <BgtPageHeader
        header={t('common.loans')}
        actions={[{ onClick: () => setOpenModal(true), variant: 'primary', content: 'loan.new.title' }]}
      />
      <BgtPageContent isLoading={isLoading} data={{ loans, settings }}>
        {({ loans, settings }) => (
          <>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 xl:gap-6">
              <BgtTextStatistic
                title={t('loan.statistics.active')}
                content={loans.filter((loan) => loan.returnedDate === null).length}
              />
              <BgtTextStatistic
                title={t('loan.statistics.returned')}
                content={loans.filter((loan) => loan.returnedDate !== null).length}
              />
              <BgtTextStatistic title={t('loan.statistics.total-loans')} content={loans.length} />
            </div>
            <BgtHeading size="5" className="flex items-center gap-2">
              <Package className="text-primary text-2xl" />
              {t('loan.statistics.active')} ({loans.filter((loan) => loan.returnedDate === null).length})
            </BgtHeading>
            {loans.filter((loan) => loan.returnedDate === null).length === 0 ? (
              <BgtText color="primary">{t('loan.no-active-loans')}</BgtText>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4 gap-6">
                {loans
                  .filter((loan) => loan.returnedDate === null)
                  .map((loan) => (
                    <LoanCard
                      key={loan.id}
                      loan={loan}
                      game={gameById(loan.gameId) ?? undefined}
                      player={playerById(loan.playerId) ?? undefined}
                      dateFormat={settings.dateFormat}
                      onReturn={actions.handleReturnLoan}
                      onDelete={actions.handleDelete}
                    />
                  ))}
              </div>
            )}
            <BgtHeading size="5" className="flex items-center gap-2">
              <History className="text-primary text-2xl" />
              {t('common.history')} ({loans.filter((loan) => loan.returnedDate !== null).length})
            </BgtHeading>
            {loans.filter((loan) => loan.returnedDate !== null).length === 0 ? (
              <BgtText color="primary">{t('loan.no-returned-loans')}</BgtText>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-4 gap-6">
                {loans
                  .filter((loan) => loan.returnedDate !== null)
                  .map((loan) => (
                    <LoanCard
                      key={loan.id}
                      loan={loan}
                      game={gameById(loan.gameId) ?? undefined}
                      player={playerById(loan.playerId) ?? undefined}
                      dateFormat={settings.dateFormat}
                      onDelete={actions.handleDelete}
                    />
                  ))}
              </div>
            )}
            <NewLoanModal open={openModal} setOpen={setOpenModal} />
          </>
        )}
      </BgtPageContent>
    </BgtPage>
  );
}
