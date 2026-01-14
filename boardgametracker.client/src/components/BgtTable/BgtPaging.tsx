import { useTranslation } from 'react-i18next';
import { SetStateAction } from 'react';

import BgtButton from '../BgtButton/BgtButton';

interface Props {
  page: number;
  setPage: (value: SetStateAction<number>) => void;
  totalCount: number;
  countPerPage: number;
}

export const BgtPaging = ({ page, setPage, totalCount, countPerPage }: Props) => {
  const { t } = useTranslation();

  if (totalCount <= countPerPage) return null;

  const totalPages = Math.ceil(totalCount / countPerPage);
  const isLastPage = page >= totalPages - 1;

  return (
    <div className="flex flex-row justify-between">
      <BgtButton variant="text" onClick={() => setPage((prev) => prev - 1)} disabled={page === 0}>
        {t('common.previous-page')}
      </BgtButton>
      <div>
        {page + 1} / {totalPages}
      </div>
      <BgtButton variant="text" onClick={() => setPage((prev) => prev + 1)} disabled={isLastPage}>
        {t('common.next-page')}
      </BgtButton>
    </div>
  );
};
