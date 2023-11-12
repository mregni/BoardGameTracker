import {Space} from 'antd';
import Table, {ColumnsType} from 'antd/es/table';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {SettingsContext} from '../../../context/settingsContext';
import {mapObjectToArray} from '../../../utils';

export const EnvironmentTable = () => {
  const { t } = useTranslation();
  const { environment } = useContext(SettingsContext);

  const columns: ColumnsType<{name: string, value: any}> = [
    {
      title: t('common.name'),
      dataIndex: 'name',
      render: (name: string) => name.toUpperCase()
    },
    {
      title: t('common.value'),
      dataIndex: 'value',
      render: (value: string) => value.toString()
    }
  ];

  console.log(mapObjectToArray(environment));

  return (
    <Space direction='vertical' style={{ display: 'flex' }}>
      <h3>{t('settings.environment-variables')}</h3>
      <Table
        pagination={false}
        columns={columns}
        dataSource={mapObjectToArray(environment)}
        size="small"
      />
    </Space>
  )
}
