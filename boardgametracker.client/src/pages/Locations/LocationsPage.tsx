import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';
import { PencilIcon, TrashIcon } from '@heroicons/react/24/outline';

import { Location } from '@/models';
import { usePage } from '@/hooks/usePage';
import { useLocations } from '@/hooks/useLocations';
import { BgtDataTable, DataTableProps } from '@/components/BgtTable/BgtDataTable';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtIconButton } from '@/components/BgtIconButton/BgtIconButton';
import { BgtCard } from '@/components/BgtCard/BgtCard';

export const LocationsPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();

  const { locations } = useLocations();

  const columns: DataTableProps<Location>['columns'] = useMemo(
    () => [
      {
        accessorKey: '0',
        cell: ({ row }) => <div>{row.original.id}</div>,
        header: t('common.id'),
      },
      {
        accessorKey: '1',
        cell: ({ row }) => <div>{row.original.name}</div>,
        header: t('common.name'),
      },
      {
        accessorKey: '2',
        cell: ({ row }) => <div className="flex justify-end">{row.original.playCount}</div>,
        header: <div className="flex justify-end">{t('common.count')}</div>,
      },
      {
        accessorKey: '3',
        cell: () => (
          <div className="flex flex-row justify-end gap-2">
            <BgtIconButton icon={<PencilIcon />} onClick={() => alert('edit')} />
            <BgtIconButton icon={<TrashIcon color="red" />} onClick={() => alert('delete')} />
          </div>
        ),
        header: '',
      },
    ],
    [t]
  );

  return (
    <BgtPage>
      <BgtPageHeader header={t(pageTitle)} actions={[]} />
      <BgtPageContent>
        <BgtCard className="p-4">
          <BgtDataTable
            columns={columns}
            data={locations}
            noDataMessage={'LOADING DATA'}
            widths={['w-[70px]', 'w-[100px]', '', 'w-[50px]']}
          />
        </BgtCard>
      </BgtPageContent>
    </BgtPage>
  );
};
