import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { createFileRoute } from '@tanstack/react-router';
import { t } from 'i18next';
import { useState } from 'react';
import NewLoanModal from './-modals/NewLoanModal';
import { useNewLoan } from './-hooks.tsx/useNewLoan';
import { getLoans } from '@/services/queries/loans';

export const Route = createFileRoute('/loans/')({
  component: RouteComponent,
  loader: ({ context: { queryClient } }) => {
    queryClient.prefetchQuery(getLoans());
  },
});

function RouteComponent() {
  const [openModal, setOpenModal] = useState(false);
  const { loans, isLoading } = useNewLoan();

  if (isLoading) return null;

  return (
    <BgtPage>
      <BgtPageHeader
        header={t('common.loans')}
        actions={[{ onClick: () => setOpenModal(true), variant: 'solid', content: 'loan.new.title' }]}
      />
      <BgtPageContent>
        <NewLoanModal open={openModal} setOpen={setOpenModal} />
        {loans.map((loan) => (
          <div>{loan.id}</div>
        ))}
      </BgtPageContent>
    </BgtPage>
  );
}
