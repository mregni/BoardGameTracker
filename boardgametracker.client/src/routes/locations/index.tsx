import { useTranslation } from 'react-i18next';
import { useState, useMemo } from 'react';
import { createFileRoute } from '@tanstack/react-router';

import { BgtDeleteModal } from '../-modals/BgtDeleteModal';
import { useToasts } from '../-hooks/useToasts';

import { useLocationsData } from './-hooks/useLocationsData';

import { NewLocationModal } from '@/routes/locations/-modals/NewLocationModal';
import { EditLocationModal } from '@/routes/locations/-modals/EditLocationModal';
import { Location } from '@/models';
import { DataTableProps, BgtDataTable } from '@/components/BgtTable/BgtDataTable';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtIconButton } from '@/components/BgtIconButton/BgtIconButton';
import { BgtCard } from '@/components/BgtCard/BgtCard';
import TrashIcon from '@/assets/icons/trash.svg?react';
import PencilIcon from '@/assets/icons/pencil.svg?react';

export const Route = createFileRoute('/locations/')({
  component: RouteComponent,
});

interface ModalState {
  location: Location | null;
  open: boolean;
}

function RouteComponent() {
  const { t } = useTranslation();
  const { infoToast, errorToast } = useToasts();
  const [deleteModalState, setDeleteModalState] = useState<ModalState>({ location: null, open: false });
  const [editModalState, setEditModalState] = useState<ModalState>({ location: null, open: false });
  const [openNewModal, setOpenNewModal] = useState(false);

  const onDeleteSuccess = () => {
    setDeleteModalState({ open: false, location: null });
    infoToast(t('location.notifications.deleted'));
  };

  const onDeleteError = () => {
    errorToast(t('location.notifications.delete-failed'));
  };

  const { locations, deleteLocation } = useLocationsData({ onDeleteSuccess, onDeleteError });

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
        header={t('common.locations')}
        actions={[{ onClick: () => setOpenNewModal(true), variant: 'solid', content: 'location.new.button' }]}
      />
      <BgtPageContent>
        <BgtCard className="p-4">
          <BgtDataTable
            columns={columns}
            data={locations}
            noDataMessage={t('common.no-data')}
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
}
