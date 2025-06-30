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

  if (totalCount < countPerPage) {
    return null;
  }

  return (
    <div className="flex flex-row justify-between">
      <BgtButton onClick={() => setPage((prev) => prev - 1)} disabled={page === 0}>
        {t('common.previous-page')}
      </BgtButton>
      <div>
        {page + 1} / {Math.ceil(totalCount / countPerPage)}
      </div>
      <BgtButton onClick={() => setPage((prev) => prev + 1)} disabled={countPerPage * page > totalCount}>
        {t('common.next-page')}
      </BgtButton>
    </div>
  );
};
