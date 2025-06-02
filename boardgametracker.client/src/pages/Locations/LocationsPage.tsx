import { useTranslation } from 'react-i18next';
import { useMemo, useState } from 'react';

import { NewLocationModal } from './modals/NewLocationModal';
import { EditLocationModal } from './modals/EditLocationModal';

import { useToast } from '@/providers/BgtToastProvider';
import { Location } from '@/models';
import { usePage } from '@/hooks/usePage';
import { useLocations } from '@/hooks/useLocations';
import { BgtDeleteModal } from '@/components/Modals/BgtDeleteModal';
import { BgtDataTable, DataTableProps } from '@/components/BgtTable/BgtDataTable';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtIconButton } from '@/components/BgtIconButton/BgtIconButton';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import TrashIcon from '@/assets/icons/trash.svg?react';
import PencilIcon from '@/assets/icons/pencil.svg?react';

interface ModalState {
  location: Location | null;
  open: boolean;
}

export const LocationsPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();
  const { showInfoToast } = useToast();
  const [deleteModalState, setDeleteModalState] = useState<ModalState>({ location: null, open: false });
  const [editModalState, setEditModalState] = useState<ModalState>({ location: null, open: false });
  const [openNewModal, setOpenNewModal] = useState(false);

  const onDeleteSuccess = () => {
    setDeleteModalState({ open: false, location: null });
    showInfoToast(t('location.notifications.deleted'));
  };

  const onDeleteFailed = () => {
    showInfoToast(t('location.notifications.delete-failed'));
  };

  const { locations, deleteLocation } = useLocations({ onDeleteSuccess, onDeleteFailed });

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
        cell: ({ row }) => (
          <div className="flex flex-row justify-end gap-2">
            <BgtIconButton
              icon={<PencilIcon className="size-5" />}
              onClick={() => {
                setEditModalState({ open: true, location: row.original });
              }}
            />
            <BgtIconButton
              icon={<TrashIcon className="size-5" />}
              intent="danger"
              onClick={() => {
                row.original.id;
                setDeleteModalState({ open: true, location: row.original });
              }}
            />
          </div>
        ),
        header: '',
      },
    ],
    [t]
  );

  return (
    <BgtPage>
      <BgtPageHeader
        header={t(pageTitle)}
        actions={[{ onClick: () => setOpenNewModal(true), variant: 'solid', content: 'location.new.button' }]}
      />
      <BgtPageContent>
        <BgtCard className="p-4">
          <BgtDataTable
            columns={columns}
            data={locations}
            noDataMessage={'LOADING DATA'}
            widths={['w-[70px]', 'w-[100px]', '', 'w-[50px]']}
          />
        </BgtCard>
        {openNewModal && <NewLocationModal open={openNewModal} close={() => setOpenNewModal(false)} />}
        <BgtDeleteModal
          title={deleteModalState.location?.name ?? ''}
          open={deleteModalState.open}
          close={() => setDeleteModalState({ location: null, open: false })}
          onDelete={() => deleteModalState.location && deleteLocation(deleteModalState.location?.id)}
          description={
            t('location.delete.description', { name: deleteModalState.location?.name ?? '' }) +
            ' ' +
            ((deleteModalState.location?.playCount ?? 0) > 0
              ? t('location.delete.extra-description', { count: deleteModalState.location?.playCount ?? 0 })
              : '')
          }
        />
        {editModalState.location && (
          <EditLocationModal
            location={editModalState.location}
            open={editModalState.open}
            close={() => setEditModalState({ location: null, open: false })}
          />
        )}
      </BgtPageContent>
    </BgtPage>
  );
};
