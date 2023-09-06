import {Space} from 'antd';
import Table, {ColumnsType} from 'antd/es/table';
import React, {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {GcActionButtons} from '../../../components/GcTable/GcActionButtons';
import {usePagination} from '../../../hooks';
import {Location} from '../../../models';
import {LocationContext} from '../context/LocationState';
import {EditLocationDrawer} from './EditLocationDrawer';

export const LocationTable = () => {
  const { locations, loading, deleteLocation, updateLocation } = useContext(LocationContext);
  const { getPagination } = usePagination();
  const { t } = useTranslation();
  const [openLocationEdit, setOpenLocationEdit] = useState(false);
  const [locationToEdit, setLocationToEdit] = useState<Location | null>(null);

  const editLocation = (id: number): void => {
    setLocationToEdit(locations.filter(location => location.id === id)[0]);
    setOpenLocationEdit(true);
  }

  const columns: ColumnsType<Location> = [
    {
      title: 'Name',
      dataIndex: 'name',
      defaultSortOrder: 'descend',
      sorter: (a, b) => a.name.localeCompare(b.name)
    },
    {
      title: 'Plays',
      dataIndex: 'playCount',
      sorter: (a, b) => a.playCount - b.playCount
    },
    {
      title: 'Actions',
      key: 'actions',
      align: 'right',
      width: 70,
      render: (data: Location) => <GcActionButtons
        id={data.id}
        title={t('location.delete.title')}
        description={t('location.delete.description')}
        edit={editLocation}
        remove={() => deleteLocation(data.id)}
      />
    }
  ];

  return (
    <Space direction='vertical' style={{ display: 'flex' }}>
      <Table
        loading={loading}
        columns={columns}
        dataSource={locations}
        size="small"
        rowKey={(location: Location) => location.id}
        pagination={getPagination(locations.length)}
      />
      {
        locationToEdit && <EditLocationDrawer
          open={openLocationEdit}
          setOpen={setOpenLocationEdit}
          location={locationToEdit as Location}
          edit={updateLocation}
        />}
    </Space>
  )
}
